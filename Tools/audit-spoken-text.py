#!/usr/bin/env python3
"""Optional local ASR screening. Differences flag takes for listening, not automatic rejection."""
import argparse
import difflib
import json
import re
import unicodedata
from pathlib import Path


def words(text):
    return re.findall(r"[a-z0-9]+", "".join(c for c in unicodedata.normalize("NFD", text.lower())
                                          if unicodedata.category(c) != "Mn"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model-dir", required=True)
    parser.add_argument("--audio-dir", required=True, type=Path)
    parser.add_argument("--report", required=True, type=Path)
    args = parser.parse_args()
    from faster_whisper import WhisperModel
    model = WhisperModel(args.model_dir, device="cpu", compute_type="int8", cpu_threads=4,
                         local_files_only=True)
    root = Path(__file__).resolve().parents[1]
    catalog = json.loads((root / "Assets/ReactorDeCaos/Resources/Narrative.json").read_text(encoding="utf-8"))
    rows = []
    for scene in catalog["scenes"]:
        for beat in scene["beats"]:
            path = args.audio_dir / (beat["voice"] + ".wav")
            if not path.exists():
                raise FileNotFoundError(f"Missing dialogue audio: {path}")
            segments, _ = model.transcribe(str(path), language="es", beam_size=5,
                                            condition_on_previous_text=False, vad_filter=False)
            actual = " ".join(segment.text.strip() for segment in segments)
            ratio = difflib.SequenceMatcher(None, words(beat["text"]), words(actual), autojunk=False).ratio()
            rows.append({"id": beat["voice"], "expected": beat["text"], "transcribed": actual,
                         "wordSequenceSimilarity": round(ratio, 3), "review": ratio < .85})
            print(beat["voice"], round(ratio, 3), actual, flush=True)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps({"method": "Whisper base local CPU; heuristic text screening, not an accent or acting evaluation",
        "clips": rows}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
