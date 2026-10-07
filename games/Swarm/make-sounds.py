#!/usr/bin/env python3
"""Makes the game's sound, so it carries none it did not make.

A short hit, a falling tone under a little noise, 16-bit mono at 22,050 a second, written to
assets/sounds/hit.wav beside this script. The noise is seeded, so the file comes out the same.
"""
import math
import os
import random
import struct
import wave

random.seed(7)
rate, seconds = 22050, 0.09
frames = []
for i in range(int(rate * seconds)):
    t = i / rate
    fade = (1 - t / seconds) ** 2
    tone = math.sin(2 * math.pi * (520 - 2600 * t) * t)
    value = (0.55 * tone + 0.25 * (random.random() * 2 - 1)) * fade
    frames.append(struct.pack('<h', int(max(-1, min(1, value)) * 26000)))

path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'assets', 'sounds', 'hit.wav')
with wave.open(path, 'wb') as out:
    out.setnchannels(1)
    out.setsampwidth(2)
    out.setframerate(rate)
    out.writeframes(b''.join(frames))
