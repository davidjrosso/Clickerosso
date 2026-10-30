#!/usr/bin/env python3
"""Validate dialogue IDs and condition PCM for the existing Unity voice mix."""
import array
import hashlib
import re
import sys
import wave

PROFILES = {"nara", "mara", "core", "memory"}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate_beats(catalog):
    beats = [beat for scene in catalog["scenes"] for beat in scene["beats"]]
    seen = set()
    for beat in beats:
        key = beat["voice"]
        if not re.fullmatch(r"[a-z0-9_]+", key) or key in seen:
            raise ValueError(f"Invalid or duplicate voice ID: {key}")
        if beat["portrait"] not in PROFILES or not beat["text"].strip():
            raise ValueError(f"Missing voice profile or text: {key}")
        seen.add(key)
    if not beats:
        raise ValueError("Empty narrative catalog")
    return beats


def finish_pcm(path):
    with wave.open(str(path), "rb") as wav:
        if (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()) != (1, 2, 22050):
            raise ValueError(f"Unexpected PCM format: {path}")
        samples = array.array("h", wav.readframes(wav.getnframes()))
    if sys.byteorder != "little":
        samples.byteswap()
    peak = max((abs(x) for x in samples), default=0)
    if not peak or len(samples) < 2205:
        raise ValueError(f"Empty or silent speech: {path}")
    # Match existing mix headroom and remove boundary clicks, without raising quiet audio.
    scale = min(1, 16000 / peak)
    fade = min(265, len(samples) // 2)
    samples = array.array("h", (int(x * scale * min(1, i / fade, (len(samples)-1-i) / fade))
                               for i, x in enumerate(samples)))
    result = {"seconds": round(len(samples) / 22050, 3),
              "peak": round(max(abs(x) for x in samples) / 32768, 4)}
    if sys.byteorder != "little":
        samples.byteswap()
    with wave.open(str(path), "wb") as wav:
        wav.setparams((1, 2, 22050, 0, "NONE", "not compressed"))
        wav.writeframes(samples.tobytes())
    result["sha256"] = digest(path)
    return result


