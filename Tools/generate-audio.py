#!/usr/bin/env python3
"""Reproducible original UI sounds, synthesized locally without external samples."""
import math
from pathlib import Path
import struct
import wave

root = Path(__file__).resolve().parents[1] / 'Assets/ReactorDeCaos/Resources/Audio'
root.mkdir(parents=True, exist_ok=True)
sample_rate = 44100
for name, duration, frequencies in [
    ('Tap', .085, [650, 1050]),
    ('Purchase', .28, [440, 554.37, 659.25]),
    ('Overload', .65, [220, 330, 440, 660]),
]:
    frames = []
    for index in range(int(sample_rate * duration)):
        time = index / sample_rate
        progress = time / duration
        attack = min(1, time / .006)
        envelope = attack * (1 - progress) ** 2
        signal = sum(math.sin(2 * math.pi * f * time) for f in frequencies) / len(frequencies)
        if name == 'Tap':
            signal = math.sin(2 * math.pi * (900 * time - 1200 * time * time))
        frames.append(struct.pack('<h', int(32767 * .45 * envelope * signal)))
    with wave.open(str(root / (name + '.wav')), 'wb') as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(sample_rate)
        output.writeframes(b''.join(frames))
    print(name, len(frames), 'samples')
