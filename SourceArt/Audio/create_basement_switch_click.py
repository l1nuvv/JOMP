"""Render the original mechanical click for the basement light switch."""

from math import exp, sin, tau
from pathlib import Path
from random import Random
import struct
import wave


if __name__ == "__main__":
    rate = 24_000
    noise = Random(17)
    samples = []
    for index in range(2_400):
        time = index / rate
        sample = 0.0
        for start, amplitude, frequency in ((0.002, 0.82, 1_180), (0.014, 0.43, 760)):
            age = time - start
            if age < 0:
                continue
            attack = min(1.0, age * 6_000)
            contact = 0.6 * noise.uniform(-1, 1) + 0.4 * sin(tau * frequency * age)
            sample += amplitude * attack * exp(-185 * age) * contact
        samples.append(sample)

    scale = 0.72 * 32767 / max(map(abs, samples))
    pcm = struct.pack(f"<{len(samples)}h", *(round(sample * scale) for sample in samples))
    project = Path(__file__).resolve().parents[2]
    output = project / "Assets/JustOneMorePeel/Resources/BasementSwitchClick.wav"
    with wave.open(str(output), "wb") as clip:
        clip.setparams((1, 2, rate, 0, "NONE", "not compressed"))
        clip.writeframes(pcm)
