# Testing Patterns

**Analysis Date:** 2026-09-28

## Summary — read this before planning any test work

This repository has **no test framework**. Not "minimal" — none. There is no
xUnit, NUnit, MSTest, or MSTest.Sdk `PackageReference` in any `.csproj` or
`.sln`; no `Microsoft.NET.Test.Sdk`; no Coverlet. There is no `pytest`,
`unittest`, or `tox` configuration, no `requirements.txt`, no `pyproject.toml`,
no `setup.py`. There is **no CI at all** — no `.github/` directory, no
workflows, no pipeline YAML of any kind.

**The entirety of automated testing is one file:**
`scripts/test_signal_detection.py` (437 lines, 22 checks). It is a hand-rolled
runner over bare `assert` statements — *not* `unittest`, despite being
"unittest-style" in shape. It is invoked by hand from the command line and by
nothing else.

**None of the ~7,000 lines of C# has any automated test.** No WinForms code is
covered by anything. The PowerShell dialogs and `batch/*.bat` are untested too.

**And the one test file is not wired into the build.** Neither `batch/build-all.bat`
nor `batch/run.bat` invokes it. It can silently rot indefinitely.

Verification commands used to establish this:

```bash
# No .NET test framework anywhere
grep -rniE "xunit|nunit|mstest|coverlet|Microsoft.NET.Test.Sdk" --include='*.csproj' --include='*.sln' .
# (no output)

# No CI
ls .github                    # No such file or directory
find . -type d -name workflows

# The only test file in the tree
find . -name 'test_*' -o -name '*_test.*' -o -name '*.test.*' -o -name '*Tests.csproj' -o -name conftest.py
# → ./scripts/test_signal_detection.py

# No Python test framework
grep -rn "^import unittest|^import pytest|from unittest" --include='*.py' .
# (no output)
ls requirements*.txt pyproject.toml setup.py tox.ini pytest.ini
# (no matches found)
```

## Test Framework

**Runner:** none. There is no runner to invoke; `scripts/test_signal_detection.py`
is both the test file and its own runner.

**Its own `main()` is the runner** (`scripts/test_signal_detection.py:426-437`):

```python
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
```

Mechanics of the hand-rolled harness:

- **Discovery** — `sorted(globals().items())` filtered on the `test_` prefix.
  No decorator, no class, no registry. Any module-level function named
  `test_*` is collected, alphabetically, **and is run in one shared
  `TemporaryDirectory()`**.
- **Reporting** — one `PASS {name}` line per test, then a summary count.
- **Exit code** — an `AssertionError` propagates out of `main()` uncaught, so
  Python exits `1`. This is the entire pass/fail mechanism.
- **No isolation** — a test that chdirs or mutates a module global must restore
  it itself (see *Test Isolation* below). There is no `setUp`/`tearDown`.
- **No selection, no filtering, no `-k`, no `--verbose`, no parallelism, no
  reporting file, no JUnit XML.**

**Assertion library:** Python's bare `assert`. No `self.assertEqual`, no
`assert` helper class, no `hamcrest`/`pytest` style.

**Run commands:**

```bash
python3 scripts/test_signal_detection.py       # run everything; exit 0 or 1
```

That is the complete set. There is no watch mode, no coverage command, no
"run one test" flag. To run a single test you would have to edit the filter in
`main()`.

**Prerequisites:** Python 3.9+ on `PATH` and the stdlib only. No install step,
no virtualenv, no dependency resolution. (`README.md:342-344` also lists
`requests` as installed on demand by the runner, but `converter.py` uses
`urllib.request` from the stdlib — `requests` is not actually imported anywhere
in `scripts/`.)

**Import bootstrap** (`scripts/test_signal_detection.py:17-22`):

```python
sys.path.insert(0, str(Path(__file__).resolve().parent))

import converter
import db_chart
import getFinalResults
import station_calibration
```

The `sys.path.insert` is what lets the test import its siblings from any working
directory. The four `scripts/` modules themselves do not do this for their own
imports (`db_chart.py:11` `import common`, `converter.py:8`
`from common import load_config`), so this one line in the test is load-bearing
for the whole file.

## Test File Organization

**Location:** flat, at the root of the module under test. There is no `tests/`
directory, no per-module test file. One test file covers four modules.

```
scripts/
├── common.py                  # helpers — exercised only transitively
├── converter.py               # covered
├── db_chart.py                # covered
├── getFinalResults.py         # covered
├── station_calibration.py     # covered
└── test_signal_detection.py   # the only test file
```

**Naming:** `test_signal_detection.py` — `test_` prefix, snake_case. It is
named after the *feature* (signal detection), not the modules, even though it
also covers aggregation, XML generation, and station calibration. A future
`test_aggregation.py` would be the natural split if the file ever needs to grow.

**Not covered by any test:**
- `scripts/common.py` — `load_config`, `load_baseline`, `channel_dbfs` are all
  exercised indirectly, but `load_config`'s explicit-path branch, the
  `isinstance(cfg, dict)` guard, and the script-dir/cwd search order
  (`common.py:7-31`) have no direct coverage.
- `scripts/converter.py` — `load_json`, `find_input_file`, `pretty_with_ns`,
  `save`, `upload`, `run`. Only `build_xml` is tested. The namespace-prefixing
  string surgery in `pretty_with_ns` (`:145-156`) and the whole upload path are
  untested.
- `scripts/getFinalResults.py` — `parse_bluetooth_results` (the field-by-field
  whitespace-split parsing of the Spanish report lines) has no test;
  `read_ms_file`, `read_device_model`, `normalize_model` are untested directly.
- All C# — everything, `apps/` and `tools/`.
- `batch/build-all.bat`, `batch/run.bat`, `show_bluetooth*.ps1`.

## Test Structure

**Suite organization:** flat module-level functions. No classes, no
`setUp`, no grouping.

```python
# scripts/test_signal_detection.py:88-94
def test_loud_sweep_detected(tmp):
    l, r = stereo_of(make_chirp)
    write_stereo_wav(tmp / "sweep.wav", l, r)
    left = measure(l)
    assert left.crest_db < db_chart.SIGNAL_MAX_CREST_DB, f"chirp crest too high: {left.crest_db:.1f}"
    ok, reason = db_chart.evaluate_signal([left, measure(r)], None)
    assert ok, f"loud sweep should be detected, got: {reason}"
```

**Signature convention:** every test takes exactly one positional argument — the
`tmp` path. Tests that need nothing from disk take `_` instead
(`test_baseline_snr(_)`, `:107`). The parameter is never optional and never
defaulted; the runner always passes it.

**Setup pattern — signal generation, not mocking.** The interesting fixtures are
synthetic WAV builders, because the thing under test is a DSP pipeline:

```python
# scripts/test_signal_detection.py:39-60
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
```

`make_chirp` models the real stimulus (low crest factor), `make_ambient` models
an empty room (high crest factor). `random.Random(seed)` is seeded so the noise
is deterministic — required, since the SNR assertions are near their thresholds.
`write_stereo_wav` (`:27-36`) emits real 16-bit stereo PCM via `struct`+`wave`.

**Small named helpers** wrap the awkward paths so tests read as assertions, not
plumbing:

```python
# scripts/test_signal_detection.py:72-85
def analyze_wav(path):
    """Run the db_chart measurement + channel_active path on a WAV file."""
    left, right, dur = db_chart.read_stereo_wav(path)
    results = [db_chart.measure_from_samples("Left", left, dur),
               db_chart.measure_from_samples("Right", right, dur)]
    active, reason = db_chart.channel_active(results, None)
    return results, active, reason


def payload(path):
    results, active, reason = analyze_wav(path)
    return db_chart.build_json(results, True, reason, active)


# :175-182
def xml_result(data):
    root = converter.build_xml(data)
    return root.find("./xDoc/record/Result").text


def subtest_names(data):
    root = converter.build_xml(data)
    return {st.find("TestName").text for st in root.findall("./xDoc/record/subtest")}
```

**Teardown pattern:** none. Cleanup is delegated to
`tempfile.TemporaryDirectory()` in `main()`.

**Assertion patterns in use:**

| Pattern | Example |
|---|---|
| Value with explanatory message | `assert ok, f"loud sweep should be detected, got: {reason}"` (`:94`) |
| Negation with literal message | `assert not ok, "ambient-only capture must be rejected"` (`:103`) |
| Substring on the reason | `assert "cresta" in reason or "piso" in reason` (`:104`) |
| Composed predicate | `assert not ok and "SNR" in reason, f"signal near baseline must fail SNR check, got: {reason}"` (`:112`) |
| Type/shape | `assert rec.find("PartNumber").text == "RS195"` (`:415`) |
| Substring in generated XML | `assert "audio_test=3/3 PASS" in rec.find("MiscInfo").text` (`:418`) |
| Set containment | `assert {"balance_knob_left", "balance_knob_right", "audio_test"} <= names` (`:420`) |
| All-of over a dict | `assert all(c["pass"] for c in v["checks"].values())` (`:231`) |

**Diagnostic output** — one test prints instead of asserting
(`scripts/test_signal_detection.py:101`):

```python
print(f"  [info] ambient: dbfs={ml.dbfs:.2f} peak={ml.peak_dbfs:.2f} crest={ml.crest_db:.2f}")
```

This is the accepted way to surface a value that helps when a threshold test
fails. Follow it rather than adding a logging framework.

## Test Isolation

There is no fixture framework, so isolation is manual and explicit. Two patterns,
both `try/finally`.

**chdir restore** — the four aggregation tests change the process working
directory because `getFinalResults.main()` reads every input from the cwd:

```python
# scripts/test_signal_detection.py:342-357
def test_aggregation_rs195(tmp):
    prev = os.getcwd()
    os.chdir(tmp)
    try:
        _write_aggregation_inputs(tmp, "RS195")
        getFinalResults.main()
        fr = json.loads((tmp / "final_results.json").read_text(encoding="utf-8"))
        assert fr["model"] == "RS195"
        assert fr["station_calibration"] == "PASS"
        assert fr["audio_test"] == {"runs": 3, "passed": 3, "result": "PASS"}
    finally:
        os.chdir(prev)
```

**Module-global monkey-patch with restore** — the only global-state test:

```python
# scripts/test_signal_detection.py:287-306
def test_get_final_results_stamps_station(tmp):
    original = getFinalResults.FILE_PATTERN_STATION_CALIB
    try:
        getFinalResults.FILE_PATTERN_STATION_CALIB = str(tmp / "station_calibration.json")
        ...
    finally:
        getFinalResults.FILE_PATTERN_STATION_CALIB = original
```

Note: because all 22 tests share one `TemporaryDirectory`, an unrestored global
would corrupt every test that runs after it. The `finally` is mandatory.

**Timestamps are not asserted as values, only as shape** —
`assert "time" in state and state["time"].endswith("Z")` (`:284`). The same
trick appears at `:300`, where the timestamp is a hardcoded literal because it
is read from a fixture file. **Follow this**: never assert on wall-clock time.

## Mocking

**Framework:** none. `unittest.mock` is never imported, `monkeypatch` does not
exist, no fake objects, no stub classes.

**The repo's approach is dependency injection through the filesystem, not
mocks.** `getFinalResults.py` and `converter.py` read glob patterns and literal
filenames from the cwd; the tests satisfy those by writing real files into
`tmp`. The setup helper writes 8 files that together emulate one completed
test-bench run:

```python
# scripts/test_signal_detection.py:316-339
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
        (tmp / "knob_left.json").write_text(json.dumps({"signal_present": True, "channel_active": "left", ...}))
        (tmp / "knob_right.json").write_text(json.dumps({"signal_present": True, "channel_active": "right", ...}))
        plays += [
            {"title": "karmaPolice", "recorded": "recorded_knob_left.wav", "duration_sec": 10.0},
            {"title": "karmaPolice", "recorded": "recorded_knob_right.wav", "duration_sec": 7.0},
        ]
    (tmp / "audio_plays.json").write_text(json.dumps({"plays": plays}), encoding="utf-8")
    verdict = "FAIL" if station_fail else "PASS"
    (tmp / "station_calibration.json").write_text(
        json.dumps({"station_calibration": verdict, "time": "2026-09-23T06:00:00Z"}), encoding="utf-8")
```

The boolean keyword args (`knob_plays`, `sweep_ok`, `station_fail`) are the
substitution mechanism: instead of mocking, the test varies the inputs and
asserts the aggregation outcome. `test_aggregation_non_knob` (`:360`) and
`test_aggregation_audio_test_fail` (`:376`) are the same fixture with one flag
flipped each.

**What is mocked:** nothing. **What is faked:** the filesystem, the ambient room
(amplitude-0.05 chirp + 3 dB baseline, `test_baseline_snr` at `:107-116`), and
the station (`STATIC_GOLDEN`).

## Fixtures and Data

**The one fixture is the shared `tmp` directory** created in `main()` and passed
to every test. There is no `conftest.py`, no fixture registry, no per-test setup
beyond the helper functions.

**Golden data is module-level and hand-written:**

```python
# scripts/test_signal_detection.py:215-223
def _golden(results, signal=True):
    return {"signal_present": signal, "measurements": results}


def _quiet_room():
    return {"left_dbfs": -52.1, "right_dbfs": -51.8}


STATIC_GOLDEN = {"golden_left_dbfs": -33.0, "golden_right_dbfs": -34.0}
```

Note the private-underscore convention for helpers used only by tests
(`_golden`, `_quiet_room`, `_write_aggregation_inputs`) versus public names for
the signal generators (`make_chirp`, `make_ambient`, `write_stereo_wav`).

**Literal dicts inline for one-off payloads** rather than factories — this is
the dominant style for `getFinalResults` and `converter` inputs:

```python
# scripts/test_signal_detection.py:199-212
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
```

**Config and baseline are written as real files in `tmp`** when a test needs
them on disk (`test_load_baseline_paths` at `:119-127`,
`test_station_state_write` at `:271-284`) — including the negative cases
(`bad.write_text("not json")` at `:123`, `.write_text("not json")` at `:302`).

**Where to put new fixture data:** module-level constants above the first
`test_` function in `scripts/test_signal_detection.py`, or inline in the test
if used once. Do not create a `fixtures/` directory — the file is small and the
pattern is not established.

## Coverage

**Requirements:** none enforced. No coverage tool, no threshold, no report, no
CI gate. Nothing fails if coverage drops to zero.

**View coverage:** not possible without adding tooling. `python3 -m trace
--count scripts/test_signal_detection.py` or `coverage run` would work, but
neither is configured or referenced anywhere in the repo.

**Effective coverage, described honestly:**

| Area | Coverage | Notes |
|---|---|---|
| `scripts/db_chart.py` | good | DSP math, `evaluate_signal`, `channel_active`, `build_json`, `load_baseline` — the original reason the file exists |
| `scripts/getFinalResults.py` | partial | `knob_verdict`, `analyze_audio_levels`, `_stamp_station_calibration`, `main()` happy + neutral paths. `parse_bluetooth_results`, `read_ms_file`, `read_device_model`, `normalize_model` untested |
| `scripts/station_calibration.py` | good | all 4 checks + both gates + `run()` |
| `scripts/converter.py` | partial | `build_xml` well covered (overall verdict, subtest list, `MiscInfo` shape). `pretty_with_ns`, `upload`, `save`, `find_input_file` untested |
| `scripts/common.py` | incidental | only via the modules that import it |
| **all C# (`apps/`, `tools/`)** | **none** | 0 of ~7,000 lines |
| `batch/*.bat` | none | |
| `show_bluetooth*.ps1` | none | |

**The single most expensive untested area is the C# orchestrator**, because it
owns the exit-code contract (`SennheiserTestRunner/Program.cs:56,63,179,326,349,373`),
the retry loops, the station-calibration gate, and the file-existence protocol
that `getFinalResults.py` depends on. A change to those file names breaks
aggregation with no test failing.

## Test Types

**Unit tests:** the 22 functions are unit tests of the pure decision functions —
`db_chart.evaluate_signal`, `db_chart.channel_active`, `db_chart.build_json`,
`getFinalResults.knob_verdict`, `getFinalResults.analyze_audio_levels`,
`getFinalResults._stamp_station_calibration`,
`station_calibration.station_verdict`, `converter.build_xml`. They exercise the
exact functions named in `README.md:281-288` as the testable seams.

**Integration tests:** four of them, and they are the closest thing to E2E —
`test_aggregation_rs195` (`:342`), `test_aggregation_non_knob` (`:360`),
`test_aggregation_audio_test_fail` (`:376`), `test_aggregation_audio_test_missing`
(`:390`). Each writes a complete fake run into `tmp`, calls
`getFinalResults.main()`, and asserts the produced `final_results.json`. Plus
`test_station_state_write` (`:271`), which drives `station_calibration.run()`
through argparse end to end.

**End-to-end tests:** none. No C# code, no audio hardware, no WinForms, no
E.A.R.S. coupler is exercised. The `bin\run.bat` full pipeline is manual-only.

## Common Patterns

### Async Testing

Not applicable — no async or threaded code under test. (The C# side does use
`System.Windows.Forms.Timer` and NAudio callbacks throughout
`TestPanels.cs:191-199`, `BluetoothDetector.cs`, `MicroTestCloud/Form1.cs`, but
none of it is tested.)

### Error Testing

Errors are asserted as *neutral-value* behavior, never as raised exceptions.

```python
# scripts/test_signal_detection.py:119-127
def test_load_baseline_paths(tmp):
    assert db_chart.load_baseline(None) is None
    assert db_chart.load_baseline(tmp / "missing.txt") is None
    bad = tmp / "bad.txt"
    bad.write_text("not json", encoding="utf-8")
    assert db_chart.load_baseline(bad) is None
    good = tmp / "calibracion.txt"
    good.write_text(json.dumps({"date": "2026-08-20", "left_dbfs": -52.1, "right_dbfs": -51.8}), encoding="utf-8")
    assert db_chart.load_baseline(good) == {"Left": -52.1, "Right": -51.8}
```

Four failure modes probed in one test: `None` input, missing file, corrupt JSON,
valid JSON. The corrupt-JSON-to-`SKIPPED` path is asserted for the aggregator too
(`:302-304`) and the missing-file-to-`SKIPPED` path (`:390-399`):

```python
(tmp / "audio_plays.json").unlink()
getFinalResults.main()
...
assert fr["audio_test"] == "SKIPPED"  # neutral, like the other missing tests
```

`ValueError` raises are **never** tested —
`db_chart.pcm_to_floats` raises on an unsupported sample width
(`scripts/db_chart.py:65`) and `split_channels` on mono input
(`scripts/db_chart.py:85`). Neither path has a test.

**Domain rule worth restating:** a missing or unreadable input must produce
`SKIPPED` / `N/A`, never `FAIL`. This is the single most important invariant in
the aggregation layer and it is enforced only by these asserts.

### Boundary Testing

Boundaries are tested at the *verdict* level, not by probing a function with
`x ± 1`:

```python
# scripts/test_signal_detection.py:107-116
def test_baseline_snr(_):
    # Quiet-but-real stimulus (~-29 dBFS) sitting only 3 dB above the room -> reject.
    stim = measure(make_chirp(amplitude=0.05))
    baseline = {"Left": stim.dbfs - 3, "Right": stim.dbfs - 3}
    ok, reason = db_chart.evaluate_signal([stim, stim], baseline)
    assert not ok and "SNR" in reason, f"signal near baseline must fail SNR check, got: {reason}"

    # Same baseline, clear stimulus well above the room -> accept.
    ok, reason = db_chart.evaluate_signal([measure(make_chirp()), measure(make_chirp())], baseline)
    assert ok, f"clear signal should pass with baseline, got: {reason}"
```

The convention is a **named failing case plus a matching passing case from the
same fixture**, with the reason string asserted so a wrong-reason failure is
distinguishable from a right-reason-wrong-threshold failure. `test_station_verdict_gates`
(`:252-268`) is the model: three independent "locked" scenarios (no goldens
configured, noisy room, no signal on the golden take) in one function.

### Combinatorial Testing

The knob verdict is the one place that tests a matrix, because its correctness
depends on rejecting five near-miss combinations:

```python
# scripts/test_signal_detection.py:167-172
assert getFinalResults.knob_verdict(left_take, right_take)["balance_knob"] == "PASS"
assert getFinalResults.knob_verdict(right_take, left_take)["balance_knob"] == "PASS"
assert getFinalResults.knob_verdict(left_take, left_take)["balance_knob"] == "FAIL"  # same channel twice
assert getFinalResults.knob_verdict(both, both)["balance_knob"] == "FAIL"  # no single-sided take
assert getFinalResults.knob_verdict(left_take, both)["balance_knob"] == "FAIL"  # one take both channels
assert getFinalResults.knob_verdict(both, right_take)["balance_knob"] == "FAIL"
```

Trailing comments name the intent of each negative case. **Do that** — a bare
`assert ... == "FAIL"` tells the reader nothing about which invariant it guards.

## Where to Add New Tests

**Test a Python verdict function:** add a `test_*` function to
`scripts/test_signal_detection.py`. It will be discovered automatically. Take
`tmp` if it needs the filesystem, `_` if it does not. Use `try/finally` for any
`chdir` or module-global mutation.

**Test a Python module that does not yet have a test file:** create
`scripts/test_<feature>.py` following the exact shape of
`test_signal_detection.py` — the same `main()` collector, the same
`sys.path.insert` bootstrap, the same `tmp` argument, the same bare `assert`
with messages. Do not introduce `pytest` for one file; it would add a
dependency to a repo that is stdlib-only by design
(`README.md:287` — "Stdlib only").

**Test C# code:** there is no pattern to follow and no harness to run it. If
this becomes a goal it is a greenfield decision, not an extension of an existing
convention — it would mean adding test projects that
`batch/build-all.bat` does not build and that no pipeline runs. Treat it as a
milestone-sized change with its own build integration, not a task to bolt onto
a feature.

## Known Gaps

- **The test file is not in the build.** `batch/build-all.bat` runs exactly two
  `dotnet publish` invocations, one `robocopy`, and a series of `copy`/`xcopy`
  lines (`:22-67`). It never calls `python`. `batch/run.bat` (`:14-15`) only
  does `start /wait "" "%APP_DIR%SennheiserTestRunner.exe"`. A green build says
  nothing about test status. `README.md:288` documents the command but nothing
  enforces it.
- **No gate anywhere.** There is no pre-commit hook, no CI, no reviewer
  checklist. A change that breaks `converter.build_xml` ships.
- **`test_ambient_rejected` prints instead of asserting** on its measurements
  (`:101`) — the only test whose diagnostics are its assertions.
- **`parse_bluetooth_results` is untested** despite being the parser most
  exposed to input drift: it splits Spanish report lines on whitespace and
  indexes into the parts by hardcoded position
  (`scripts/getFinalResults.py:126-143`). A change in the report format from
  `FunctionalButtonTest` breaks Bluetooth verdicts with no failure.
- **The `FILE_PATTERN_*` file-name contract is untested.** `getFinalResults.py:15-25`
  and the writer side in `apps/FunctionalButtonTest` / `AudioTest/Form1.cs` must
  agree; nothing verifies it. The aggregation tests only prove the pattern works
  for files the test itself invented.
- **Thresholds are provisional by their own admission**
  (`scripts/db_chart.py:16-17`, "until calibrated against known-good units in
  the field"). `test_loud_sweep_detected` and `test_ambient_rejected` pin the
  *shape* of the behaviour, not the field validity of the numbers.

---

*Testing analysis: 2026-09-28*
