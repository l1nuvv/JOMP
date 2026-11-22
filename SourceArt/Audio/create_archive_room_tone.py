"""Render a seamless room tone for the archive."""

from math import sin, tau
from pathlib import Path
from random import Random
import struct
import wave


if __name__ == "__main__":
    rate = 24_000
    length = 12 * rate
    rng = Random(17)
    noise = [rng.uniform(-1, 1) for _ in range(length)]
    air = low = 0.0
    samples = []

    # Warm the filters with the previous period to keep the loop continuous.
    for i in range(length * 2):
        value = noise[i % length]
        air += (value - air) * 0.13
        low += (value - low) * 0.023
        if i >= length:
            seconds = (i - length) / rate
            phase = tau * (i - length) / length
            drift = 0.89 + 0.07 * sin(phase) + 0.04 * sin(2 * phase + 0.5)
            hum = 0.065 * sin(tau * 57 * seconds) + 0.026 * sin(tau * 112 * seconds + 0.9)
            samples.append(drift * (0.70 * low + 0.19 * (air - low) + hum))

    scale = 0.42 * 32767 / max(map(abs, samples))
    pcm = struct.pack(f"<{length}h", *(round(sample * scale) for sample in samples))
    project = Path(__file__).resolve().parents[2]
    output = project / "Assets/JustOneMorePeel/Resources/ArchiveRoomTone.wav"
    with wave.open(str(output), "wb") as clip:
        clip.setparams((1, 2, rate, 0, "NONE", "not compressed"))
        clip.writeframes(pcm)
