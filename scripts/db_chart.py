from __future__ import annotations

import argparse
import json
import math
import struct
import wave
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Tuple

MIN_DB_FLOOR = -120.0  # Minimum dB value for unrepresentable audio
DEFAULT_CALIBRATION_SPL = 94.0  # Reference SPL for calibration

# PCM sample width scaling factors
PCM_SCALE_8BIT = 128.0
PCM_SCALE_16BIT = 32768.0
PCM_SCALE_24BIT = 8388608.0
PCM_SCALE_32BIT = 2147483648.0

# Chart rendering constants
CHART_FIGSIZE = (8, 4.5)
CHART_DPI = 150
ASCII_CHART_WIDTH = 50

# Stereo constants
STEREO_CHANNELS = 2
CHANNEL_LEFT = 0
CHANNEL_RIGHT = 1


@dataclass
class Measurement:
    label: str
    rms: float
    dbfs: float
    dbspl: float | None
    peak_dbfs: float
    duration_sec: float


def pcm_to_floats(raw: bytes, sample_width: int) -> List[float]:
    """Convert raw PCM bytes to normalized floating-point samples."""
    if sample_width == 1:
        vals = [(b - 128) / PCM_SCALE_8BIT for b in raw]
        return vals

    if sample_width == 2:
        count = len(raw) // 2
        vals = struct.unpack("<" + "h" * count, raw)
        return [v / PCM_SCALE_16BIT for v in vals]

    if sample_width == 3:
        vals = []
        for i in range(0, len(raw), 3):
            b0, b1, b2 = raw[i], raw[i + 1], raw[i + 2]
            x = b0 | (b1 << 8) | (b2 << 16)
            if x & 0x800000:
                x -= 0x1000000
            vals.append(x / PCM_SCALE_24BIT)
        return vals

    if sample_width == 4:
        count = len(raw) // 4
        vals = struct.unpack("<" + "i" * count, raw)
        return [v / PCM_SCALE_32BIT for v in vals]

    raise ValueError(f"Unsupported sample width: {sample_width} bytes")


def calc_rms(samples: List[float]) -> float:
    """Calculate RMS (root mean square) amplitude of samples."""
    if not samples:
        return 0.0
    return math.sqrt(sum(s * s for s in samples) / len(samples))


def db_from_amplitude(amplitude: float, floor_db: float = MIN_DB_FLOOR) -> float:
    """Convert linear amplitude to decibels, with floor threshold."""
    if amplitude <= 0.0:
        return floor_db
    return max(20.0 * math.log10(amplitude), floor_db)


def split_channels(samples: List[float], channels: int) -> Tuple[List[float], List[float]]:
    """Separate interleaved stereo samples into left and right channels."""
    if channels < STEREO_CHANNELS:
        raise ValueError("Input WAV must be stereo (2 channels)")

    left = samples[CHANNEL_LEFT::channels]
    right = samples[CHANNEL_RIGHT::channels]

    return left, right


def read_stereo_wav(wav_path: Path) -> Tuple[List[float], List[float], float]:
    """Read stereo WAV file and return left/right channels with duration."""
    with wave.open(str(wav_path), "rb") as wf:
        channels = wf.getnchannels()
        sample_width = wf.getsampwidth()
        frame_rate = wf.getframerate()
        nframes = wf.getnframes()
        raw = wf.readframes(nframes)

    samples = pcm_to_floats(raw, sample_width)

    left, right = split_channels(samples, channels)

    duration = 0.0 if frame_rate == 0 else nframes / frame_rate

    return left, right, duration


def measure_from_samples(
    label: str,
    samples: List[float],
    duration: float,
    calibration_offset_db: float | None
) -> Measurement:
    """Analyze audio samples and return loudness measurements."""

    rms = calc_rms(samples)
    peak = max((abs(s) for s in samples), default=0.0)

    dbfs = db_from_amplitude(rms)
    peak_dbfs = db_from_amplitude(peak)

    dbspl = None if calibration_offset_db is None else dbfs + calibration_offset_db

    return Measurement(
        label=label,
        rms=rms,
        dbfs=dbfs,
        dbspl=dbspl,
        peak_dbfs=peak_dbfs,
        duration_sec=duration,
    )


def print_table(results: List[Measurement]) -> None:
    """Print results as formatted ASCII table."""
    use_spl = any(r.dbspl is not None for r in results)
    print("\nChannel Loudness Results")
    print("-" * 78)
    if use_spl:
        print(f"{'Channel':<12} {'RMS':>10} {'dBFS':>10} {'dBSPL':>10} {'Peak dBFS':>12} {'Sec':>8}")
    else:
        print(f"{'Channel':<12} {'RMS':>10} {'dBFS':>10} {'Peak dBFS':>12} {'Sec':>8}")

    for r in results:
        if use_spl:
            spl = f"{r.dbspl:0.2f}" if r.dbspl is not None else "N/A"
            print(f"{r.label:<12} {r.rms:>10.6f} {r.dbfs:>10.2f} {spl:>10} {r.peak_dbfs:>12.2f} {r.duration_sec:>8.2f}")
        else:
            print(f"{r.label:<12} {r.rms:>10.6f} {r.dbfs:>10.2f} {r.peak_dbfs:>12.2f} {r.duration_sec:>8.2f}")


def print_ascii_chart(results: List[Measurement]) -> None:
    use_spl = all(r.dbspl is not None for r in results)
    metric_name = "dBSPL" if use_spl else "dBFS"
    values = [r.dbspl if use_spl else r.dbfs for r in results]
    max_val = max(values)
    min_val = min(values)

    print(f"\nASCII Chart ({metric_name})")
    print("-" * 78)

    span = max(max_val - min_val, 1e-9)

    for r, v in zip(results, values):
        bar_len = int(((v - min_val) / span) * ASCII_CHART_WIDTH)
        bar = "#" * bar_len
        print(f"{r.label:<12} | {bar:<50} {v:>7.2f} {metric_name}")


def maybe_save_png(results: List[Measurement], output_png: Path) -> None:
    try:
        import matplotlib.pyplot as plt
    except Exception:
        print(f"\n[Info] matplotlib not installed. Skipped PNG export: {output_png}")
        return

    labels = [r.label for r in results]
    use_spl = all(r.dbspl is not None for r in results)
    y = [r.dbspl if use_spl else r.dbfs for r in results]
    ylabel = "dBSPL" if use_spl else "dBFS"

    plt.figure(figsize=CHART_FIGSIZE)
    bars = plt.bar(labels, y)
    plt.title("Headphone Channel Comparison")
    plt.ylabel(ylabel)
    plt.grid(axis="y", alpha=0.25)

    for b, v in zip(bars, y):
        plt.text(b.get_x() + b.get_width() / 2, b.get_height(), f"{v:.2f}",
                 ha="center", va="bottom")

    plt.tight_layout()
    plt.savefig(output_png, dpi=CHART_DPI)
    plt.close()
    print(f"\nSaved chart image: {output_png}")


def build_json(results: List[Measurement]) -> Dict:
    return {
        "measurements": [
            {
                "channel": r.label,
                "rms": r.rms,
                "dbfs": r.dbfs,
                "dbspl": r.dbspl,
                "peak_dbfs": r.peak_dbfs,
                "duration_sec": r.duration_sec,
            }
            for r in results
        ]
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Analyze stereo WAV recording and compare channel loudness."
    )

    parser.add_argument(
        "--input",
        required=True,
        type=Path,
        help="Path to stereo WAV capture"
    )

    parser.add_argument("--calibration-dbfs", type=float, default=None,
                        help="Measured dBFS for your calibration tone")
    parser.add_argument("--calibration-spl", type=float, default=94.0,
                        help="Known SPL of calibration tone (default: 94 dB SPL)")

    parser.add_argument("--json", action="store_true",
                        help="Print machine-readable JSON to stdout (for C# integration)")
    parser.add_argument("--json-out", type=Path, default=None,
                        help="Optional path to write JSON results")
    parser.add_argument("--png-out", type=Path, default=None,
                        help="Optional output PNG path (requires matplotlib)")

    args = parser.parse_args()

    calibration_offset_db = None
    if args.calibration_dbfs is not None:
        calibration_offset_db = args.calibration_spl - args.calibration_dbfs

    left_samples, right_samples, duration = read_stereo_wav(args.input)

    left_measure = measure_from_samples(
        "Left",
        left_samples,
        duration,
        calibration_offset_db
    )

    right_measure = measure_from_samples(
        "Right",
        right_samples,
        duration,
        calibration_offset_db
    )

    combined_samples = [l + r for l, r in zip(left_samples, right_samples)]

    both_measure = measure_from_samples(
        "Both",
        combined_samples,
        duration,
        calibration_offset_db
    )

    results = [
        left_measure,
        right_measure,
        both_measure
    ]

    print_table(results)
    print_ascii_chart(results)

    if args.png_out:
        maybe_save_png(results, args.png_out)

    payload = build_json(results)
    if args.json_out:
        args.json_out.write_text(json.dumps(payload, indent=2), encoding="utf-8")
        print(f"\nSaved JSON: {args.json_out}")

    if args.json:
        print("\nJSON_RESULT=" + json.dumps(payload, separators=(",", ":")))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
