"""Offline validation of failure paths and PCM conditioning; no model required."""
import array
import importlib.util
import tempfile
import unittest
import wave
import json
import sys
from unittest.mock import patch
from pathlib import Path

spec = importlib.util.spec_from_file_location("neural", Path(__file__).with_name("voice-pcm.py"))
neural = importlib.util.module_from_spec(spec)
spec.loader.exec_module(neural)
cinema_spec = importlib.util.spec_from_file_location("cinema", Path(__file__).with_name("generate-cinematic-voices.py"))
cinema = importlib.util.module_from_spec(cinema_spec)
cinema_spec.loader.exec_module(cinema)


class VoiceTests(unittest.TestCase):
    def catalog(self, key="intro_0", portrait="nara"):
        return {"scenes": [{"beats": [{"voice": key, "portrait": portrait, "text": "Hola."}]}]}

    def test_reject_path_traversal_and_unknown_profiles(self):
        for catalog in [self.catalog("../escape"), self.catalog(portrait="unknown"), {"scenes": []}]:
            with self.assertRaises(ValueError):
                neural.validate_beats(catalog)

    def test_reject_duplicate_ids(self):
        catalog = self.catalog()
        catalog["scenes"] *= 2
        with self.assertRaises(ValueError):
            neural.validate_beats(catalog)

    def test_silence_rejected_and_peak_bounded_with_fades(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "speech.wav"
            for value in [0, 32000]:
                with wave.open(str(path), "wb") as wav:
                    wav.setparams((1, 2, 22050, 0, "NONE", "not compressed"))
                    samples = array.array("h", [value] * 5000)
                    if neural.sys.byteorder != "little":
                        samples.byteswap()
                    wav.writeframes(samples.tobytes())
                if value == 0:
                    with self.assertRaises(ValueError):
                        neural.finish_pcm(path)
                else:
                    report = neural.finish_pcm(path)
                    self.assertLess(report["peak"], .5)
                    with wave.open(str(path), "rb") as wav:
                        pcm = wav.readframes(wav.getnframes())
                    self.assertEqual(pcm[:2], b"\0\0")
                    self.assertEqual(pcm[-2:], b"\0\0")

    def test_install_checks_entire_catalog_before_replacing_any_clip(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            resources = root / "Assets/ReactorDeCaos/Resources"
            output = resources / "Audio/Voice"
            output.mkdir(parents=True)
            (root / "Tools").mkdir()
            (root / "Docs/Cinematics").mkdir(parents=True)
            work = root / "staging"
            (work / "references").mkdir(parents=True)
            (work / "rendered").mkdir()
            direction = root / "Tools/voice-direction.json"
            direction.write_text('{"accent":"Latin American Spanish"}', encoding="utf-8")
            reference = work / "references/nara.wav"
            reference.write_bytes(b"synthetic reference")
            beats = [dict(voice=key, text="Hola.", speaker="NARA", portrait="nara") for key in ("first", "second")]
            (resources / "Narrative.json").write_text(json.dumps({"scenes": [{"beats": beats}]}), encoding="utf-8")
            for beat in beats:
                (output / (beat["voice"] + ".wav")).write_bytes(b"original")
                path = work / "rendered" / (beat["voice"] + ".wav")
                path.write_bytes(b"generated")
                row = dict(id=beat["voice"], text=beat["text"], speaker=beat["speaker"],
                           models=cinema.MODELS,
                           sha256=neural.digest(path), directionSha256=neural.digest(direction),
                           referenceSha256=neural.digest(reference))
                path.with_suffix(".json").write_text(json.dumps(row), encoding="utf-8")
            second = work / "rendered/second.json"
            valid = second.read_text(encoding="utf-8")
            with patch.object(cinema, "ROOT", root), patch.object(sys, "argv", ["generator", "install", "--work-dir", str(work)]):
                for field in ("text", "sha256", "directionSha256", "referenceSha256", "models"):
                    row = json.loads(valid)
                    row[field] = "stale"
                    second.write_text(json.dumps(row), encoding="utf-8")
                    with self.assertRaises(ValueError):
                        cinema.main()
                    self.assertEqual((output / "first.wav").read_bytes(), b"original")
                second.write_text(valid, encoding="utf-8")
                cinema.main()
                self.assertEqual((output / "first.wav").read_bytes(), b"generated")


if __name__ == "__main__":
    unittest.main()
