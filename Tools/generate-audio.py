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
    ('OverloadEnd', .55, [220, 440]),
    ('Milestone', .65, [523.25, 659.25, 783.99]),
    ('OverloadHum', 2, [55, 110, 220]),
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
        if name == 'OverloadEnd':
            signal = math.sin(2 * math.pi * (660 * time - 320 * time * time))
        if name == 'OverloadHum':
            # Integer cycles across two seconds: continuous low-level loop.
            envelope = .45 + .1 * math.sin(2 * math.pi * 2 * time)
        frames.append(struct.pack('<h', int(32767 * .45 * envelope * signal)))
    with wave.open(str(root / (name + '.wav')), 'wb') as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(sample_rate)
        output.writeframes(b''.join(frames))
    print(name, len(frames), 'samples')

# New original cues: shared pentatonic electronic timbre, separate melodic shapes.
for name, notes, length in [
    ('ComboRise', [659.25, 783.99], .22),
    ('GeneratorUnlock', [329.63, 493.88, 659.25], .58),
    ('Ascension', [164.81, 246.94, 329.63, 493.88, 659.25], 1.15),
]:
    frames = []
    for index in range(round(sample_rate * length)):
        t = index / sample_rate
        slot = length / len(notes)
        note = min(len(notes) - 1, int(t / slot))
        age = t - note * slot
        envelope = min(1, age / .012) * max(0, 1 - age / slot) ** 2
        signal = (math.sin(2 * math.pi * notes[note] * age)
                  + .18 * math.sin(2 * math.pi * notes[note] * 2 * age)) / 1.18
        frames.append(struct.pack('<h', round(32767 * .38 * envelope * signal)))
    with wave.open(str(root / (name + '.wav')), 'wb') as output:
        output.setparams((1, 2, sample_rate, 0, 'NONE', 'not compressed'))
        output.writeframes(b''.join(frames))
    print(name, len(frames), 'samples')

# Quiet, beatless 16-second pad. Every oscillator and envelope completes an
# integer number of cycles, including at the loop boundary. No sampled music.
length = 16
frames = []
frequencies = [round(f * length) / length for f in [130.81, 196, 261.63, 329.63]]
for index in range(sample_rate * length):
    t = index / sample_rate
    signal = sum(math.sin(2 * math.pi * f * t) * (.65 + .35 * math.cos(2 * math.pi * (i + 1) * t / length))
                 for i, f in enumerate(frequencies)) / len(frequencies)
    frames.append(struct.pack('<h', round(32767 * .22 * signal)))
with wave.open(str(root / 'AmbientMusic.wav'), 'wb') as output:
    output.setparams((1, 2, sample_rate, 0, 'NONE', 'not compressed'))
    output.writeframes(b''.join(frames))
print('AmbientMusic', len(frames), 'samples')
