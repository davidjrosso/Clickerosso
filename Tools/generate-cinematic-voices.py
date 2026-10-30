#!/usr/bin/env python3
"""Local Qwen3 voice design then consistent character synthesis. No paid API.

download: pinned public model weights; design: create four original synthetic voices;
render: bake all dialogue into staging; install: validate and copy staged WAVs to Unity.
The separate install phase lets a human audition performances before shipping.
"""
import argparse
import hashlib
import importlib.util
import json
import os
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODELS = {
    "design": ["Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign", "5ecdb67327fd37bb2e042aab12ff7391903235d3"],
    "base": ["Qwen/Qwen3-TTS-12Hz-0.6B-Base", "5d83992436eae1d760afd27aff78a71d676296fc"],
}
WEIGHT_HASHES = {
    "design": "391e8db219f292c515297cdceeb43e4eae67cdde35fa57e79a6a8a532fca0522",
    "base": "180b3b10eb1c9f1b4db7806d5475bae3071c0243c299d49926bab1da3b6946f6",
    "tokenizer": "836b7b357f5ea43e889936a3709af68dfe3751881acefe4ecf0dbd30ba571258",
}
spec = importlib.util.spec_from_file_location("pcm", Path(__file__).with_name("voice-pcm.py"))
pcm = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pcm)


def load_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def save_json(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("phase", choices=["download", "design", "render", "install"])
    parser.add_argument("--work-dir", required=True, type=Path)
    parser.add_argument("--only", help="Render one voice ID for audition; install requires every catalog ID")
    parser.add_argument("--take", type=int, help="New deterministic take for --only; bypass its cache")
    parser.add_argument("--batch-size", type=int, choices=[1, 2, 3, 4], default=2)
    args = parser.parse_args()
    if args.take is not None and (args.phase != "render" or not args.only or args.take < 0):
        parser.error("--take requires render --only and a nonnegative number")
    work = args.work_dir.resolve()
    if work.is_relative_to(ROOT / "Assets"):
        parser.error("Working files must be outside Unity Assets")
    work.mkdir(parents=True, exist_ok=True)
    os.environ.setdefault("HF_HOME", str(work / "cache"))
    if args.phase == "download":
        from huggingface_hub import snapshot_download
        for name, (repo, revision) in MODELS.items():
            snapshot_download(repo, revision=revision, local_dir=str(work / name),
                              ignore_patterns=["*.md", ".gitattributes"], max_workers=2)
        return
    # No network is permitted during synthesis; all weights must already be local.
    os.environ["HF_HUB_OFFLINE"] = "1"
    os.environ["TRANSFORMERS_OFFLINE"] = "1"
    direction = load_json(ROOT / "Tools/voice-direction.json")
    beats = pcm.validate_beats(load_json(ROOT / "Assets/ReactorDeCaos/Resources/Narrative.json"))
    if args.only and args.only not in {beat['voice'] for beat in beats}:
        parser.error(f"Unknown voice ID: {args.only}")
    refs = work / "references"
    stage = work / "rendered"
    refs.mkdir(exist_ok=True)
    stage.mkdir(exist_ok=True)
    direction_hash = pcm.digest(ROOT / "Tools/voice-direction.json")
    if args.phase == "install":
        report = []
        for beat in beats:
            path = stage / (beat["voice"] + ".wav")
            row = load_json(path.with_suffix(".json"))
            if row.get("id") != beat["voice"]:
                raise ValueError(f"Mismatched voice ID: {beat['voice']}")
            if row.get("models") != MODELS:
                raise ValueError(f"Stale model versions: {beat['voice']}")
            if any(row[key] != beat[key] for key in ("text", "speaker")):
                raise ValueError(f"Stale dialogue: {beat['voice']}")
            if row["sha256"] != pcm.digest(path) or row["directionSha256"] != direction_hash:
                raise ValueError(f"Stale audio/direction: {beat['voice']}")
            if row["referenceSha256"] != pcm.digest(refs / (beat["portrait"] + ".wav")):
                raise ValueError(f"Stale reference: {beat['voice']}")
            report.append(row)
        output = ROOT / "Assets/ReactorDeCaos/Resources/Audio/Voice"
        for beat in beats:
            name = beat["voice"] + ".wav"
            (output / name).write_bytes((stage / name).read_bytes())
        save_json(ROOT / "Docs/Cinematics/voice.json", {
            "engine": "Qwen3-TTS VoiceDesign 1.7B + Base 0.6B (local CUDA)",
            "neuralVoice": True, "sampleRate": 22050, "models": MODELS,
            "accentDirection": direction["accent"], "clips": report})
        print(f"Installed {len(report)} validated clips; Unity GUIDs preserved.")
        return
    import numpy as np
    import soundfile as sf
    import torch
    from qwen_tts import Qwen3TTSModel
    from scipy.signal import resample_poly
    from math import gcd
    torch.set_num_threads(1)
    name = "design" if args.phase == "design" else "base"
    for relative, expected in [("model.safetensors", WEIGHT_HASHES[name]),
                               ("speech_tokenizer/model.safetensors", WEIGHT_HASHES["tokenizer"])]:
        with (work / name / relative).open("rb") as stream:
            if hashlib.file_digest(stream, "sha256").hexdigest() != expected:
                raise ValueError(f"Model weight checksum mismatch: {name}/{relative}")
    model = Qwen3TTSModel.from_pretrained(str(work / name), device_map="cuda:0",
                                          dtype=torch.bfloat16, attn_implementation="sdpa")
    if args.phase == "design":
        for character, profile in direction["characters"].items():
            torch.manual_seed(profile["seed"])
            wavs, rate = model.generate_voice_design(text=profile["referenceText"], language="Spanish",
                instruct=profile["direction"], max_new_tokens=1500)
            if not np.isfinite(wavs[0]).all() or not .1 < len(wavs[0]) / rate < 40:
                raise ValueError(f"Invalid reference audio: {character}")
            sf.write(str(refs / (character + ".wav")), wavs[0], rate, subtype="PCM_16")
            print(f"Designed {character}", flush=True)
        save_json(refs / "direction.json", direction)
        return
    if load_json(refs / "direction.json") != direction:
        raise ValueError("Reference voices do not match current direction; rerun design")
    prompts = {}
    pending = []
    for beat in beats:
        if args.only and args.only != beat["voice"]:
            continue
        character = beat["portrait"]
        profile = direction["characters"][character]
        path = stage / (beat["voice"] + ".wav")
        if args.take is None and path.exists() and path.with_suffix(".json").exists():
            previous = load_json(path.with_suffix(".json"))
            if (previous["text"] == beat["text"] and previous["speaker"] == beat["speaker"]
                    and previous.get("models") == MODELS
                    and previous["directionSha256"] == direction_hash
                    and previous["referenceSha256"] == pcm.digest(refs / (character + ".wav"))
                    and previous["sha256"] == pcm.digest(path)):
                print(f"Verified cached {beat['voice']}", flush=True)
                continue
        if character not in prompts:
            prompts[character] = model.create_voice_clone_prompt(
                ref_audio=str(refs / (character + ".wav")), ref_text=profile["referenceText"])
        pending.append(beat)
    # Similar text lengths reduce padding work without changing dialogue order in Unity.
    pending.sort(key=lambda beat: (len(beat["text"]), beat["voice"]))
    for start in range(0, len(pending), args.batch_size):
        batch = pending[start:start + args.batch_size]
        keys = [beat["voice"] for beat in batch]
        seed_key = "|".join(keys)
        if args.take is not None:
            seed_key += f"|take:{args.take}"
        seed = int(hashlib.sha256(seed_key.encode()).hexdigest()[:8], 16)
        torch.manual_seed(seed)
        started = time.monotonic()
        wavs, rate = model.generate_voice_clone(text=[beat["text"] for beat in batch], language="Spanish",
            voice_clone_prompt=[prompts[beat["portrait"]][0] for beat in batch], max_new_tokens=480)
        if len(wavs) != len(batch):
            raise ValueError("Incomplete generated batch")
        for beat, wav in zip(batch, wavs):
            character = beat["portrait"]
            path = stage / (beat["voice"] + ".wav")
            signal = np.asarray(wav)
            if not np.isfinite(signal).all() or not .1 < len(signal) / rate < 38:
                raise ValueError(f"Invalid speech duration/samples: {beat['voice']}")
            divisor = gcd(rate, 22050)
            signal = resample_poly(signal, 22050 // divisor, rate // divisor)
            sf.write(str(path), signal, 22050, subtype="PCM_16")
            save_json(path.with_suffix(".json"), {"id": beat["voice"], "speaker": beat["speaker"],
                "text": beat["text"], "character": character, "seed": seed, "take": args.take,
                "batch": keys, "models": MODELS,
                "directionSha256": direction_hash, "referenceSha256": pcm.digest(refs / (character + ".wav")),
                **pcm.finish_pcm(path)})
            print(f"Rendered {beat['voice']}", flush=True)
        print(f"Batch completed in {time.monotonic() - started:.1f}s", flush=True)


if __name__ == "__main__":
    main()
