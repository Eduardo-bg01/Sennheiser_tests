#!/usr/bin/env python3
"""
Station calibration (TV Listeners) verdict.

Consumes a LevelTest golden-unit recording (results.json from db_chart), the
daily ambient baseline (calibracion.txt), and the per-machine config.json to
produce the 4-check verdict written to station_calibration.json:

  check1_ambient : nothing playing must sit at/below ambient_max_dbfs
  check2_left    : golden unit L channel within golden_left_dbfs +/- tolerance
  check3_right   : golden unit R channel within golden_right_dbfs +/- tolerance
  check4_balance : |L - R| <= balance_max_db

The verdict is authoritative: SennheiserTestRunner gates the pipeline on it and
getFinalResults stamps it into every DUT record.
"""
import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from common import load_baseline, load_config

DEFAULTS = {
    "golden_tolerance_db": 3.0,
    "balance_max_db": 2.0,
    "ambient_max_dbfs": -30.0,
}


def channel_dbfs(results, channel):
    for m in results.get("measurements", []):
        if str(m.get("channel", "")).lower() == channel:
            v = m.get("dbfs")
            return v if isinstance(v, (int, float)) else None
    return None


def station_verdict(results, baseline, cfg):
    """Compute the 4-check verdict. results = db_chart payload, baseline = ambient."""
    thresholds = {k: cfg.get(k, v) for k, v in DEFAULTS.items()} if cfg else dict(DEFAULTS)
    checks = {}
    reason = []

    base = baseline or {}
    amb_l = base.get("left_dbfs")
    amb_r = base.get("right_dbfs")
    if amb_l is None or amb_r is None:
        checks["check1_ambient"] = {"pass": False, "detail": "sin calibración ambiental (calibracion.txt)"}
        reason.append("check1: no hay línea base ambiental")
    else:
        ambient_max = max(amb_l, amb_r)
        ok = ambient_max <= thresholds["ambient_max_dbfs"]
        checks["check1_ambient"] = {
            "pass": ok,
            "detail": f"ambiente max {ambient_max:.2f} dBFS <= {thresholds['ambient_max_dbfs']:.0f} dBFS"
        }
        if not ok:
            reason.append(f"check1: ruido ambiente {ambient_max:.2f} dBFS sobre el límite")
        if amb_l is not None:
            checks["check1_ambient"]["ambient_left_dbfs"] = round(amb_l, 2)
        if amb_r is not None:
            checks["check1_ambient"]["ambient_right_dbfs"] = round(amb_r, 2)

    signal_present = bool(results.get("signal_present", False))
    left = channel_dbfs(results, "left")
    right = channel_dbfs(results, "right")

    golden_l = (cfg or {}).get("golden_left_dbfs")
    golden_r = (cfg or {}).get("golden_right_dbfs")
    tol = thresholds["golden_tolerance_db"]
    bal = thresholds["balance_max_db"]

    golden_ok = isinstance(golden_l, (int, float)) and isinstance(golden_r, (int, float))
    if not golden_ok:
        reason.append("golden_left_dbfs/golden_right_dbfs sin configurar en config.json")

    check2_ok = check3_ok = check4_ok = False
    if not signal_present:
        reason.append("no se detectó señal del audífono (signal_present=false)")
    if left is None or right is None:
        reason.append("resultados sin mediciones L/R")
    if golden_ok and signal_present and left is not None and right is not None:
        delta_l = abs(left - golden_l)
        delta_r = abs(right - golden_r)
        diff = abs(left - right)
        check2_ok = delta_l <= tol
        check3_ok = delta_r <= tol
        check4_ok = diff <= bal
        checks["check2_left"] = {
            "pass": check2_ok,
            "detail": f"|{left:.2f} - {golden_l:.2f}| = {delta_l:.2f} <= {tol:.1f} dB",
        }
        checks["check3_right"] = {
            "pass": check3_ok,
            "detail": f"|{right:.2f} - {golden_r:.2f}| = {delta_r:.2f} <= {tol:.1f} dB",
        }
        checks["check4_balance"] = {
            "pass": check4_ok,
            "detail": f"|L-R| = {diff:.2f} <= {bal:.1f} dB",
        }
        if not check2_ok:
            reason.append(f"check2: canal L {left:.2f} fuera de tolerancia (±{tol:.1f} dB de {golden_l:.2f})")
        if not check3_ok:
            reason.append(f"check3: canal R {right:.2f} fuera de tolerancia (±{tol:.1f} dB de {golden_r:.2f})")
        if not check4_ok:
            reason.append(f"check4: balance |L-R| = {diff:.2f} dB sobre {bal:.1f} dB")
    else:
        for name in ("check2_left", "check3_right", "check4_balance"):
            checks[name] = {"pass": False, "detail": reason[-1] if reason else "datos insuficientes"}

    overall = "PASS" if all(c["pass"] for c in checks.values()) else "FAIL"
    now = datetime.now(timezone.utc)
    return {
        "date": now.strftime("%Y-%m-%d"),
        "time": now.strftime("%Y-%m-%dT%H:%M:%SZ"),
        "station_calibration": overall,
        "checks": checks,
        "signal_present": signal_present,
        "left_dbfs": None if left is None else round(left, 2),
        "right_dbfs": None if right is None else round(right, 2),
        "golden_left_dbfs": golden_l,
        "golden_right_dbfs": golden_r,
        "golden_tolerance_db": tol,
        "balance_max_db": bal,
        "ambient_max_dbfs": thresholds["ambient_max_dbfs"],
        "reason": "; ".join(reason),
    }


def run(argv=None):
    parser = argparse.ArgumentParser(description="Station calibration verdict -> station_calibration.json")
    parser.add_argument("--results", required=True, help="results.json from db_chart (golden-unit take)")
    parser.add_argument("--baseline", default=None, help="calibracion.txt ambient baseline")
    parser.add_argument("--config", default=None, help="per-machine config.json")
    parser.add_argument("--out", default="station_calibration.json")
    args = parser.parse_args(argv)

    results = json.loads(Path(args.results).read_text(encoding="utf-8"))
    cfg = load_config(args.config)
    baseline = load_baseline(args.baseline)
    verdict = station_verdict(results, baseline, cfg)
    Path(args.out).write_text(json.dumps(verdict, indent=2), encoding="utf-8")
    print(verdict["station_calibration"])


if __name__ == "__main__":
    run()