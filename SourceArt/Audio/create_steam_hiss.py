"""Render the original looping wallpaper-steamer sound used by the game."""

import math
from pathlib import Path
import random
import struct
import wave


def render(output: Path) -> None:
    rate = 24_000
    length = 57_600
    rng = random.Random(480)
    noise = [rng.uniform(-1, 1) for _ in range(length)]
    treble = mid = low = 0.0
    samples = []

    # Warm the filters with the previous loop so the seam has the same filter state.
    for i, value in enumerate(noise + noise):
        treble += (value - treble) * 0.62
        mid += (value - mid) * 0.17
        low += (value - low) * 0.038
        if i >= length:
            phase = math.tau * (i - length) / length
            pressure = 0.89 + 0.06 * math.sin(phase) + 0.035 * math.sin(3 * phase + 0.8)
            samples.append(pressure * (0.82 * (treble - mid) + 0.65 * (mid - low)))

    scale = 0.65 * 32767 / max(map(abs, samples))
    pcm = struct.pack(f"<{length}h", *(round(sample * scale) for sample in samples))
    with wave.open(str(output), "wb") as clip:
        clip.setparams((1, 2, rate, 0, "NONE", "not compressed"))
        clip.writeframes(pcm)


if __name__ == "__main__":
    project = Path(__file__).resolve().parents[2]
    render(project / "Assets/JustOneMorePeel/Resources/SteamHiss.wav")
