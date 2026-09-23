#!/usr/bin/env python3
"""
Aggregate test results from multiple output files into final_results.json.
Consolidates audio measurements, device tests, and microphone results.
"""
import argparse
import glob
import json
import os
from datetime import datetime, timezone

# File patterns
FILE_PATTERN_SERIAL = "serial*"
FILE_PATTERN_AUDIO = "hearingPass*"
FILE_PATTERN_RESULTS = "results.json"
FILE_PATTERN_KNOB_LEFT = "knob_left.json"
FILE_PATTERN_KNOB_RIGHT = "knob_right.json"
FILE_PATTERN_BLUETOOTH = "Prueba_*"
FILE_PATTERN_MICROPHONE = "MicroTest_*"
FILE_PATTERN_TIME_START = "tiempo1.txt"
FILE_PATTERN_TIME_END = "tiempo2.txt"

# Audio measurement thresholds
CHANNEL_BALANCE_THRESHOLD = 2  # dB difference acceptable
VOLUME_MIN = -30  # dB - minimum acceptable level
VOLUME_MAX = -10  # dB - maximum acceptable level
CLIPPING_THRESHOLD = 0  # dB - any level above 0 is clipping

# Test result constants
RESULT_PASS = "PASS"
RESULT_FAIL = "FAIL"
RESULT_TRUE = "True"

# Bluetooth test field names
BT_FIELD_CONNECTION = "Conexión Bluetooth"
BT_FIELD_PLAY_PAUSE = "Play / Pausa"
BT_FIELD_PREVIOUS = "Anterior"
BT_FIELD_NEXT = "Siguiente"
BT_FIELD_VOLUME_UP = "Subir Volumen"
BT_FIELD_VOLUME_DOWN = "Bajar Volumen"

# Microphone result field name
MIC_FIELD_RESULT = "Resultado"

# Models whose volume result is not applicable (reported as N/A, hidden from UI)
MODELS_WITHOUT_VOLUME = {"hd550", "hd560s", "hd569", "hd599", "hd600", "hd650", "hd660s", "hd400u"}

# RS195 knob test: the active EARS channel must clear the muted one by this much (provisional).
KNOB_SEPARATION_DB = 15.0

# All possible Bluetooth and level fields
BT_RESULT_FIELDS = ["bluetooth", "play_pausa", "anterior", "siguiente", "subir_volumen", "bajar_volumen"]
LEVEL_RESULT_FIELDS = ["left_dbfs", "left_peak", "right_dbfs", "right_peak", "balance", "volume", "clipping", "deteccion_senal", "balance_knob"]

def first_match(pattern):
    """Return first file matching glob pattern, or None."""
    matches = glob.glob(pattern)
    return matches[0] if matches else None

def read_text_file(path):
    """Read text file with fallback encodings."""
    encodings = ["utf-8", "cp1252", "latin-1"]
    for enc in encodings:
        try:
            with open(path, "r", encoding=enc) as f:
                return f.read().lstrip("\ufeff")
        except UnicodeDecodeError:
            continue
    # Last resort: ignore errors
    with open(path, "r", encoding="utf-8", errors="ignore") as f:
        return f.read().lstrip("\ufeff")

def normalize_model(name):
    """Strip non-alphanumerics and lowercase, for model matching."""
    return "".join(c for c in (name or "").lower() if c.isalnum())

def read_device_model(btfile):
    """Extract device model name from the Bluetooth report's Dispositivo line."""
    if not btfile:
        return ""
    for line in read_text_file(btfile).splitlines():
        if "Dispositivo" in line:
            return line.split(":", 1)[-1].strip()
    return ""

def read_ms_file(path):
    """Read millisecond timestamp from file."""
    if os.path.exists(path):
        try:
            with open(path, "r") as f:
                txt = f.read().strip()
                if txt.isdigit():
                    return int(txt)
        except Exception:
            pass
    return None

def parse_bluetooth_results(filepath, missing):
    """Extract Bluetooth test results from report file."""
    results = {}
    for line in read_text_file(filepath).splitlines():
        if BT_FIELD_CONNECTION in line:
            parts = line.split()
            results["bluetooth"] = parts[2] if len(parts) > 2 else missing
        elif BT_FIELD_PLAY_PAUSE in line:
            parts = line.split()
            results["play_pausa"] = parts[3] if len(parts) > 3 else missing
        elif BT_FIELD_PREVIOUS in line:
            parts = line.split()
            results["anterior"] = parts[1] if len(parts) > 1 else missing
        elif BT_FIELD_NEXT in line:
            parts = line.split()
            results["siguiente"] = parts[1] if len(parts) > 1 else missing
        elif BT_FIELD_VOLUME_UP in line:
            parts = line.split()
            results["subir_volumen"] = parts[2] if len(parts) > 2 else missing
        elif BT_FIELD_VOLUME_DOWN in line:
            parts = line.split()
            results["bajar_volumen"] = parts[2] if len(parts) > 2 else missing
    return results

def analyze_audio_levels(measurements):
    """Analyze audio measurements for volume, balance, and clipping."""
    results = {}
    left_dbfs = None
    right_dbfs = None
    left_peak = None
    right_peak = None
    
    for m in measurements:
        if m["channel"] == "Left":
            left_dbfs = round(m["dbfs"], 2)
            left_peak = round(m["peak_dbfs"], 2)
            results["left_dbfs"] = left_dbfs
            results["left_peak"] = left_peak
        elif m["channel"] == "Right":
            right_dbfs = round(m["dbfs"], 2)
            right_peak = round(m["peak_dbfs"], 2)
            results["right_dbfs"] = right_dbfs
            results["right_peak"] = right_peak
    
    # Check channel balance
    if left_dbfs is not None and right_dbfs is not None:
        diff = abs(right_dbfs - left_dbfs)
        results["balance"] = RESULT_PASS if diff <= CHANNEL_BALANCE_THRESHOLD else RESULT_FAIL
        
        # Check volume levels
        left_ok = VOLUME_MIN <= left_dbfs <= VOLUME_MAX
        right_ok = VOLUME_MIN <= right_dbfs <= VOLUME_MAX
        results["volume"] = RESULT_PASS if (left_ok and right_ok) else RESULT_FAIL
    
    # Check for clipping
    if left_peak is not None and right_peak is not None:
        peak = max(left_peak, right_peak)
        results["clipping"] = RESULT_PASS if peak <= CLIPPING_THRESHOLD else RESULT_FAIL
    
    return results

def _channel_dbfs(data, channel):
    """Look up a channel's dBFS from a db_chart JSON payload."""
    for m in data.get("measurements", []):
        if str(m.get("channel", "")).lower() == channel:
            return m.get("dbfs")
    return None

def knob_verdict(left_take, right_take):
    """Verdict for the RS195 balance knob from the two db_chart JSON payloads.

    Mirror-agnostic: the E.A.R.S. jig sits flipped vs the headset, so we never
    assume which physical ear maps to which channel. Each take must leave exactly
    one channel sounding (knob hard to a side), the two takes must light opposite
    channels (knob actually swaps the driver), and the active channel must clear
    the muted one by KNOB_SEPARATION_DB.
    """
    reason = []
    verdicts = {}
    for name, take in (("left", left_take), ("right", right_take)):
        active = take.get("channel_active")
        if active not in ("left", "right"):
            verdicts[name] = RESULT_FAIL
            reason.append(f"toma {name}: canal activo '{active}' (se espera un solo canal)")
            continue
        muted = "right" if active == "left" else "left"
        active_dbfs = _channel_dbfs(take, active)
        muted_dbfs = _channel_dbfs(take, muted)
        sep = None if active_dbfs is None or muted_dbfs is None else active_dbfs - muted_dbfs
        if sep is not None and sep >= KNOB_SEPARATION_DB:
            verdicts[name] = RESULT_PASS
        else:
            verdicts[name] = RESULT_FAIL
            sep_txt = f"{sep:.1f}" if sep is not None else "n/d"
            reason.append(f"toma {name}: separación {sep_txt} dB < {KNOB_SEPARATION_DB} dB")

    active_left = left_take.get("channel_active")
    active_right = right_take.get("channel_active")
    differ = active_left in ("left", "right") and active_left != active_right
    if not differ:
        reason.append("ambas tomas activaron el mismo canal")

    ok = all(v == RESULT_PASS for v in verdicts.values()) and differ
    return {
        "left": verdicts.get("left"),
        "right": verdicts.get("right"),
        "left_channel_active": active_left,
        "right_channel_active": active_right,
        "balance_knob": RESULT_PASS if ok else RESULT_FAIL,
        "reason": "; ".join(reason),
    }

def main():
    """Generate final_results.json from test output files."""
    parser = argparse.ArgumentParser()
    parser.add_argument("--some", action="store_true",
                        help="build-some variant: fill missing tests with N/A")
    args = parser.parse_args()
    missing = "N/A" if args.some else "SKIPPED"

    final_results = {}
    
    # Read serial number
    serialfile = first_match(FILE_PATTERN_SERIAL)
    if serialfile:
        final_results["serial"] = read_text_file(serialfile).strip()
    else:
        final_results["serial"] = missing if args.some else ""
    
    # Read audio distortion test
    audiofile = first_match(FILE_PATTERN_AUDIO)
    if audiofile:
        audio_result = read_text_file(audiofile).strip()
        final_results["distorsion"] = RESULT_PASS if audio_result == RESULT_TRUE else RESULT_FAIL
    else:
        final_results["distorsion"] = missing

    # Dedicated failing subtest so an operator-rejected unit is visible in the XML
    # even if other fields end up N/A. converter.py flips the overall result on it.
    if final_results["distorsion"] == RESULT_FAIL:
        final_results["audio_fail"] = RESULT_FAIL
    
    # Read Bluetooth control test (also provides the device model)
    btfile = first_match(FILE_PATTERN_BLUETOOTH)

    # Read audio level measurements
    if os.path.exists(FILE_PATTERN_RESULTS):
        results_text = read_text_file(FILE_PATTERN_RESULTS)
        results = json.loads(results_text)
        audio_analysis = analyze_audio_levels(results.get("measurements", []))
        # deteccion_senal mirrors the operator's AudioTest verdict (visible choice),
        # not the auto signal detector (which stays as informational warning only).
        # This avoids false negatives / invisible FAILs for HD/IE families.
        if final_results.get("distorsion") in (RESULT_PASS, RESULT_FAIL):
            audio_analysis["deteccion_senal"] = final_results["distorsion"]
        else:
            audio_analysis["deteccion_senal"] = missing
        model = normalize_model(read_device_model(btfile))
        if any(m in model for m in MODELS_WITHOUT_VOLUME) or model.startswith("ie"):
            audio_analysis["volume"] = "N/A"
        final_results.update(audio_analysis)
    elif args.some:
        for field in LEVEL_RESULT_FIELDS:
            final_results[field] = missing
    else:
        final_results["balance"] = missing
        final_results["volume"] = missing
        final_results["clipping"] = missing
        final_results["balance_knob"] = missing
    
    if btfile:
        bt_results = parse_bluetooth_results(btfile, missing)
        final_results.update(bt_results)
    elif args.some:
        for field in BT_RESULT_FIELDS:
            final_results[field] = missing
    
    # Read microphone test
    micfile = first_match(FILE_PATTERN_MICROPHONE)
    if micfile:
        for line in read_text_file(micfile).splitlines():
            if MIC_FIELD_RESULT in line:
                parts = line.split()
                final_results["resultado_mic"] = RESULT_PASS if "PAS" in parts[2] else RESULT_FAIL
    else:
        final_results["resultado_mic"] = missing

    # Read knob balance test (RS195). LevelTest writes the two db_chart outputs
    # (knob_left.json / knob_right.json); the verdict is computed here so the
    # rule lives in one testable place.
    knob_left = first_match(FILE_PATTERN_KNOB_LEFT)
    knob_right = first_match(FILE_PATTERN_KNOB_RIGHT)
    if knob_left and knob_right:
        try:
            left_data = json.loads(read_text_file(knob_left))
            right_data = json.loads(read_text_file(knob_right))
            knob = knob_verdict(left_data, right_data)
            final_results["balance_knob"] = knob["balance_knob"]
        except Exception:
            final_results["balance_knob"] = missing
    elif args.some and "balance_knob" not in final_results:
        final_results["balance_knob"] = missing
    
    # Add timestamps if available
    try:
        t1 = read_ms_file(FILE_PATTERN_TIME_START)
        t2 = read_ms_file(FILE_PATTERN_TIME_END)
        if t1 is not None:
            final_results['StartTime'] = datetime.fromtimestamp(t1 / 1000.0, tz=timezone.utc).strftime('%Y-%m-%d %H:%M:%S')
        if t2 is not None:
            final_results['EndTime'] = datetime.fromtimestamp(t2 / 1000.0, tz=timezone.utc).strftime('%Y-%m-%d %H:%M:%S')
    except Exception:
        # Best-effort: if timestamp parsing fails, continue without timestamps
        pass
    
    # Write results
    with open("final_results.json", "w") as f:
        json.dump(final_results, f, indent=4)
    
    print("final_results.json generated successfully")

if __name__ == "__main__":
    main()
