#!/usr/bin/env python3
"""Self-checks for signal-presence detection and verdict logic.

Run: python3 scripts/test_signal_detection.py
No frameworks, plain asserts. Exits non-zero on failure.
"""
import json
import math
import os
import random
import struct
import sys
import tempfile
import wave
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import converter
import db_chart
import getFinalResults
import station_calibration

SAMPLE_RATE = 44100


def write_stereo_wav(path, left, right):
    frames = b"".join(
        struct.pack("<hh", int(max(-1.0, min(1.0, l)) * 32767), int(max(-1.0, min(1.0, r)) * 32767))
        for l, r in zip(left, right)
    )
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        wf.writeframes(frames)


def make_chirp(seconds=5.0, amplitude=0.5):
    """Sweep 200 Hz -> 2000 Hz; low crest factor like the real stimulus."""
    n = int(SAMPLE_RATE * seconds)
    out, phase = [], 0.0
    for i in range(n):
        t = i / SAMPLE_RATE
        freq = 200 + (2000 - 200) * (i / n)
        phase += 2 * math.pi * freq / SAMPLE_RATE
        out.append(amplitude * math.sin(phase))
    return out


def make_ambient(seconds=5.0, seed=7):
    """Low noise floor plus sparse transients: high crest factor, like an empty room."""
    rng = random.Random(seed)
    n = int(SAMPLE_RATE * seconds)
    out = [rng.gauss(0, 0.0008) for _ in range(n)]
    for k in range(0, n, SAMPLE_RATE // 3):  # a click every ~0.33 s
        for j in range(60):
            if k + j < n:
                out[k + j] += 0.05 * math.exp(-j / 12.0) * rng.choice((-1, 1))
    return out


def measure(samples):
    return db_chart.measure_from_samples("Left", samples, len(samples) / SAMPLE_RATE, None)


def stereo_of(generator):
    s = generator()
    return s, s  # identical channels keep it simple


def analyze_wav(path):
    """Run the db_chart measurement + channel_active path on a WAV file."""
    left, right, dur = db_chart.read_stereo_wav(path)
    results = [
        db_chart.measure_from_samples("Left", left, dur, None),
        db_chart.measure_from_samples("Right", right, dur, None),
    ]
    active, reason = db_chart.channel_active(results, None)
    return results, active, reason


def payload(path):
    results, active, reason = analyze_wav(path)
    return db_chart.build_json(results, True, reason, active)


def test_loud_sweep_detected(tmp):
    l, r = stereo_of(make_chirp)
    write_stereo_wav(tmp / "sweep.wav", l, r)
    left = measure(l)
    assert left.crest_db < db_chart.SIGNAL_MAX_CREST_DB, f"chirp crest too high: {left.crest_db:.1f}"
    ok, reason = db_chart.evaluate_signal([left, measure(r)], None)
    assert ok, f"loud sweep should be detected, got: {reason}"


def test_ambient_rejected(tmp):
    l, r = stereo_of(make_ambient)
    write_stereo_wav(tmp / "ambient.wav", l, r)
    ml, mr = measure(l), measure(r)
    print(f"  [info] ambient: dbfs={ml.dbfs:.2f} peak={ml.peak_dbfs:.2f} crest={ml.crest_db:.2f}")
    ok, reason = db_chart.evaluate_signal([ml, mr], None)
    assert not ok, "ambient-only capture must be rejected"
    assert "cresta" in reason or "piso" in reason


def test_baseline_snr(_):
    # Quiet-but-real stimulus (~-29 dBFS) sitting only 3 dB above the room -> reject.
    stim = measure(make_chirp(amplitude=0.05))
    baseline = {"Left": stim.dbfs - 3, "Right": stim.dbfs - 3}
    ok, reason = db_chart.evaluate_signal([stim, stim], baseline)
    assert not ok and "SNR" in reason, f"signal near baseline must fail SNR check, got: {reason}"

    # Same baseline, clear stimulus well above the room -> accept.
    ok, reason = db_chart.evaluate_signal([measure(make_chirp()), measure(make_chirp())], baseline)
    assert ok, f"clear signal should pass with baseline, got: {reason}"


def test_load_baseline_paths(tmp):
    assert db_chart.load_baseline(None) is None
    assert db_chart.load_baseline(tmp / "missing.txt") is None
    bad = tmp / "bad.txt"
    bad.write_text("not json", encoding="utf-8")
    assert db_chart.load_baseline(bad) is None
    good = tmp / "calibracion.txt"
    good.write_text(json.dumps({"date": "2026-08-20", "left_dbfs": -52.1, "right_dbfs": -51.8}), encoding="utf-8")
    assert db_chart.load_baseline(good) == {"Left": -52.1, "Right": -51.8}


def test_json_payload_shape(tmp):
    l, r = stereo_of(make_chirp)
    write_stereo_wav(tmp / "sweep.wav", l, r)
    results = [measure(l), measure(r)]
    payload = db_chart.build_json(results, True, "", "both")
    assert payload["signal_present"] is True
    assert payload["measurements"][0]["crest_db"] == results[0].crest_db
    assert "signal_reason" in payload
    assert payload["channel_active"] == "both"


def test_knob_channel_active(tmp):
    write_stereo_wav(tmp / "both.wav", make_chirp(), make_chirp())
    results, active, reason = analyze_wav(tmp / "both.wav")
    assert active == "both", reason

    write_stereo_wav(tmp / "left.wav", make_chirp(), make_ambient())
    results, active, reason = analyze_wav(tmp / "left.wav")
    assert active == "left", reason

    write_stereo_wav(tmp / "right.wav", make_ambient(), make_chirp())
    results, active, reason = analyze_wav(tmp / "right.wav")
    assert active == "right", reason

    write_stereo_wav(tmp / "none.wav", make_ambient(), make_ambient())
    results, active, reason = analyze_wav(tmp / "none.wav")
    assert active == "none", reason


def test_knob_verdict(tmp):
    write_stereo_wav(tmp / "left.wav", make_chirp(), make_ambient())
    write_stereo_wav(tmp / "right.wav", make_ambient(), make_chirp())
    write_stereo_wav(tmp / "both.wav", make_chirp(), make_chirp())
    left_take = payload(tmp / "left.wav")
    right_take = payload(tmp / "right.wav")
    both = payload(tmp / "both.wav")

    assert getFinalResults.knob_verdict(left_take, right_take)["balance_knob"] == "PASS"
    assert getFinalResults.knob_verdict(right_take, left_take)["balance_knob"] == "PASS"
    assert getFinalResults.knob_verdict(left_take, left_take)["balance_knob"] == "FAIL"  # same channel twice
    assert getFinalResults.knob_verdict(both, both)["balance_knob"] == "FAIL"  # no single-sided take
    assert getFinalResults.knob_verdict(left_take, both)["balance_knob"] == "FAIL"  # one take both channels
    assert getFinalResults.knob_verdict(both, right_take)["balance_knob"] == "FAIL"


def xml_result(data):
    root = converter.build_xml(data)
    return root.find("./xDoc/record/Result").text


def subtest_names(data):
    root = converter.build_xml(data)
    return {st.find("TestName").text for st in root.findall("./xDoc/record/subtest")}


def test_converter_overall(_):
    assert xml_result({"distorsion": "PASS", "balance": "PASS"}) == "PASS"
    assert xml_result({"distorsion": "FAIL"}) == "FAIL"
    assert xml_result({"deteccion_senal": "FAIL"}) == "FAIL"
    assert xml_result({"audio_fail": "FAIL"}) == "FAIL"
    # N/A stays neutral by design (documented rule).
    assert xml_result({"distorsion": "PASS", "volume": "N/A", "bluetooth": "N/A"}) == "PASS"


def test_converter_new_subtests(_):
    names = subtest_names({"distorsion": "PASS", "deteccion_senal": "FAIL", "audio_fail": "FAIL"})
    assert {"deteccion_senal", "audio_fail"} <= names


def test_analyze_audio_levels(_):
    good = [
        {"channel": "Left", "dbfs": -20.0, "peak_dbfs": -3.0},
        {"channel": "Right", "dbfs": -21.0, "peak_dbfs": -4.0},
    ]
    res = getFinalResults.analyze_audio_levels(good)
    assert res["balance"] == "PASS" and res["volume"] == "PASS" and res["clipping"] == "PASS"

    bad = [
        {"channel": "Left", "dbfs": -20.0, "peak_dbfs": -3.0},
        {"channel": "Right", "dbfs": -30.0, "peak_dbfs": 0.5},
    ]
    res = getFinalResults.analyze_audio_levels(bad)
    assert res["balance"] == "FAIL" and res["clipping"] == "FAIL"


def _golden(results, signal=True):
    return {"signal_present": signal, "measurements": results}


def _quiet_room():
    return {"left_dbfs": -52.1, "right_dbfs": -51.8}


STATIC_GOLDEN = {"golden_left_dbfs": -33.0, "golden_right_dbfs": -34.0}


def test_station_verdict_pass(_):
    v = station_calibration.station_verdict(
        _golden([{"channel": "Left", "dbfs": -33.2}, {"channel": "Right", "dbfs": -34.3}]),
        _quiet_room(), STATIC_GOLDEN)
    assert v["station_calibration"] == "PASS"
    assert all(c["pass"] for c in v["checks"].values())


def test_station_verdict_off_tolerance(_):
    v = station_calibration.station_verdict(
        _golden([{"channel": "Left", "dbfs": -28.0}, {"channel": "Right", "dbfs": -34.1}]),
        _quiet_room(), STATIC_GOLDEN)
    assert v["station_calibration"] == "FAIL"
    assert not v["checks"]["check2_left"]["pass"]
    assert "check2" in v["reason"]


def test_station_verdict_unbalanced(_):
    v = station_calibration.station_verdict(
        _golden([{"channel": "Left", "dbfs": -33.0}, {"channel": "Right", "dbfs": -30.0}]),
        _quiet_room(), STATIC_GOLDEN)
    assert v["station_calibration"] == "FAIL"
    assert not v["checks"]["check3_right"]["pass"]
    assert not v["checks"]["check4_balance"]["pass"]


def test_station_verdict_gates(_):
    good = _golden([{"channel": "Left", "dbfs": -33.2}, {"channel": "Right", "dbfs": -34.3}])
    # Goldens not configured -> locked no matter how good the take is.
    v = station_calibration.station_verdict(good, _quiet_room(), {})
    assert v["station_calibration"] == "FAIL"
    assert "golden_left_dbfs" in v["reason"]
    # Noisy room -> locked even when the golden matches.
    v = station_calibration.station_verdict(good, {"left_dbfs": -25.0, "right_dbfs": -25.0}, STATIC_GOLDEN)
    assert v["station_calibration"] == "FAIL"
    assert not v["checks"]["check1_ambient"]["pass"]
    # No signal on the golden take -> locked.
    v = station_calibration.station_verdict(
        _golden([{"channel": "Left", "dbfs": -33.2}, {"channel": "Right", "dbfs": -34.3}], signal=False),
        _quiet_room(), STATIC_GOLDEN)
    assert v["station_calibration"] == "FAIL"
    for name in ("check2_left", "check3_right", "check4_balance"):
        assert not v["checks"][name]["pass"]


def test_station_state_write(tmp):
    out = tmp / "station_calibration.json"
    (tmp / "config.json").write_text(json.dumps(STATIC_GOLDEN), encoding="utf-8")
    (tmp / "calibracion.txt").write_text(json.dumps(_quiet_room()), encoding="utf-8")
    (tmp / "results.json").write_text(
        json.dumps(_golden([{"channel": "Left", "dbfs": -33.2}, {"channel": "Right", "dbfs": -34.3}])),
        encoding="utf-8")
    station_calibration.run(["--results", str(tmp / "results.json"),
                             "--baseline", str(tmp / "calibracion.txt"),
                             "--config", str(tmp / "config.json"),
                             "--out", str(out)])
    state = json.loads(out.read_text(encoding="utf-8"))
    assert state["station_calibration"] == "PASS"
    assert "time" in state and state["time"].endswith("Z")


def test_get_final_results_stamps_station(tmp):
    original = getFinalResults.FILE_PATTERN_STATION_CALIB
    try:
        getFinalResults.FILE_PATTERN_STATION_CALIB = str(tmp / "station_calibration.json")
        (tmp / "station_calibration.json").write_text(json.dumps({"station_calibration": "FAIL"}), encoding="utf-8")
        final = {"distorsion": "PASS"}
        getFinalResults._stamp_station_calibration(final, "SKIPPED")
        assert final["station_calibration"] == "FAIL"

        (tmp / "station_calibration.json").write_text(
            json.dumps({"station_calibration": "PASS", "time": "2026-09-23T06:00:00Z"}), encoding="utf-8")
        getFinalResults._stamp_station_calibration(final, "SKIPPED")
        assert final["station_calibration"] == "PASS"
        assert final["station_calibration_time"] == "2026-09-23T06:00:00Z"

        (tmp / "station_calibration.json").write_text("not json", encoding="utf-8")
        getFinalResults._stamp_station_calibration(final, "SKIPPED")
        assert final["station_calibration"] == "SKIPPED"
    finally:
        getFinalResults.FILE_PATTERN_STATION_CALIB = original


def test_converter_station_calib_flips(_):
    assert xml_result({"station_calibration": "FAIL"}) == "FAIL"
    assert xml_result({"station_calibration": "PASS"}) == "PASS"
    names = subtest_names({"station_calibration": "FAIL"})
    assert "station_calibration" in names


def _write_aggregation_inputs(tmp, model, knob_plays=True, sweep_ok=True, station_fail=False):
    (tmp / "serial.txt").write_text("SN00315588", encoding="utf-8")
    (tmp / "hearingPass.txt").write_text("True", encoding="utf-8")
    (tmp / "Prueba_001.txt").write_text(f"Dispositivo: {model}\nConexión Bluetooth: PASS\n", encoding="utf-8")
    left = -24.31 if sweep_ok else -24.31
    right = -25.02 if sweep_ok else -30.00
    (tmp / "results.json").write_text(json.dumps({"measurements": [
        {"channel": "Left", "dbfs": left, "peak_dbfs": -6.12},
        {"channel": "Right", "dbfs": right, "peak_dbfs": -6.88},
    ]}), encoding="utf-8")
    plays = [{"title": "audioSweep", "recorded": "recorded.wav", "duration_sec": 40.0}]
    if knob_plays:
        (tmp / "knob_left.json").write_text(json.dumps({"signal_present": True, "channel_active": "left",
            "measurements": [{"channel": "Left", "dbfs": -20.0}, {"channel": "Right", "dbfs": -45.0}]}), encoding="utf-8")
        (tmp / "knob_right.json").write_text(json.dumps({"signal_present": True, "channel_active": "right",
            "measurements": [{"channel": "Left", "dbfs": -46.0}, {"channel": "Right", "dbfs": -21.0}]}), encoding="utf-8")
        plays += [
            {"title": "karmaPolice", "recorded": "recorded_knob_left.wav", "duration_sec": 10.0},
            {"title": "karmaPolice", "recorded": "recorded_knob_right.wav", "duration_sec": 7.0},
        ]
    (tmp / "audio_plays.json").write_text(json.dumps({"plays": plays}), encoding="utf-8")
    verdict = "FAIL" if station_fail else "PASS"
    (tmp / "station_calibration.json").write_text(
        json.dumps({"station_calibration": verdict, "time": "2026-09-23T06:00:00Z"}), encoding="utf-8")


def test_aggregation_rs195(tmp):
    prev = os.getcwd()
    os.chdir(tmp)
    try:
        _write_aggregation_inputs(tmp, "RS195")
        getFinalResults.main()
        fr = json.loads((tmp / "final_results.json").read_text(encoding="utf-8"))
        assert fr["model"] == "RS195"
        assert fr["station_calibration"] == "PASS"
        assert fr["station_calibration_time"] == "2026-09-23T06:00:00Z"
        assert fr["balance_knob"] == "PASS"
        assert fr["balance_knob_left"] == "PASS"
        assert fr["balance_knob_right"] == "PASS"
        assert fr["audio_test"] == {"runs": 3, "passed": 3, "result": "PASS"}
    finally:
        os.chdir(prev)


def test_aggregation_non_knob(tmp):
    prev = os.getcwd()
    os.chdir(tmp)
    try:
        _write_aggregation_inputs(tmp, "MOMENTUM TW 4", knob_plays=False)
        getFinalResults.main()
        fr = json.loads((tmp / "final_results.json").read_text(encoding="utf-8"))
        assert fr["model"] == "MOMENTUM TW 4"
        assert fr["balance_knob"] == "SKIPPED"
        assert fr["balance_knob_left"] == "SKIPPED"
        assert fr["balance_knob_right"] == "SKIPPED"
        assert fr["audio_test"] == {"runs": 1, "passed": 1, "result": "PASS"}
    finally:
        os.chdir(prev)


def test_aggregation_audio_test_fail(tmp):
    prev = os.getcwd()
    os.chdir(tmp)
    try:
        _write_aggregation_inputs(tmp, "RS195", sweep_ok=False)
        getFinalResults.main()
        fr = json.loads((tmp / "final_results.json").read_text(encoding="utf-8"))
        assert fr["audio_test"] == {"runs": 3, "passed": 2, "result": "FAIL"}
        assert fr["balance"] == "FAIL"
        assert fr["balance_knob"] == "PASS"  # knob takes still fine
    finally:
        os.chdir(prev)


def test_aggregation_audio_test_missing(tmp):
    prev = os.getcwd()
    os.chdir(tmp)
    try:
        _write_aggregation_inputs(tmp, "RS195")
        (tmp / "audio_plays.json").unlink()
        getFinalResults.main()
        fr = json.loads((tmp / "final_results.json").read_text(encoding="utf-8"))
        assert fr["audio_test"] == "SKIPPED"  # neutral, like the other missing tests
    finally:
        os.chdir(prev)


def test_converter_schema(_):
    data = {
        "serial": "SN1",
        "model": "RS195",
        "station_calibration": "PASS",
        "station_calibration_time": "2026-09-23T06:00:00Z",
        "balance_knob_left": "PASS",
        "balance_knob_right": "PASS",
        "audio_test": {"runs": 3, "passed": 3, "result": "PASS"},
    }
    root = converter.build_xml(data)
    rec = root.find("./xDoc/record")
    assert rec.find("PartNumber").text == "RS195"
    assert "model=RS195" in rec.find("MiscInfo").text
    assert "station_calibration=PASS@2026-09-23T06:00:00Z" in rec.find("MiscInfo").text
    assert "audio_test=3/3 PASS" in rec.find("MiscInfo").text
    names = subtest_names(data)
    assert {"balance_knob_left", "balance_knob_right", "audio_test"} <= names
    assert xml_result(data) == "PASS"
    fail_data = dict(data, audio_test={"runs": 3, "passed": 2, "result": "FAIL"})
    assert xml_result(fail_data) == "FAIL"


def main():
    tests = [v for k, v in sorted(globals().items()) if k.startswith("test_")]
    with tempfile.TemporaryDirectory() as td:
        tmp = Path(td)
        for t in tests:
            t(tmp)
            print(f"PASS {t.__name__}")
    print(f"\nAll {len(tests)} checks passed.")


if __name__ == "__main__":
    main()
