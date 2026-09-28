<!-- refreshed: 2026-09-28 -->
# Architecture

**Analysis Date:** 2026-09-28

## System Overview

An operator-driven production test bench for refurbished Sennheiser headphones. A single
.NET WinForms orchestrator (`SennheiserTestRunner.exe`) runs a fixed, ordered sequence of
test stages. All five test apps are **compiled into the orchestrator and hosted in the
same process as modal forms**; only external tools (PowerShell prompts, Python scripts,
`VolumeHelper.exe`, `RefurbishTool.exe`) are child processes. Stages do not return values
to each other — they communicate exclusively through flat artifact files dropped in the
runner working directory, which the same Python scripts later aggregate and upload.

```text
┌───────────────────────────────────────────────────────────────────────────┐
│  bin\SennheiserTestRunner.exe   [apps/SennheiserTestRunner/Program.cs]    │
│  static Program.Main — linear stage sequence, exit codes 1..6             │
│  Environment.CurrentDirectory := BaseDir (Program.cs:32)                  │
└──────┬──────────────────────┬──────────────────────┬──────────────────────┘
       │ ShowDialog()         │ ShowDialog()         │ RunProcess() / Process.Start
┌──────▼──────────────┐ ┌─────▼──────────────┐ ┌─────▼────────────────────────┐
│ IN-PROCESS WinForms │ │ IN-PROCESS         │ │ CHILD PROCESSES             │
│  AskForSerial2      │ │  HeadPhoneTest2    │ │  powershell *.ps1 prompts   │
│  BluetoothHeadphone-│ │  Form1 — TWICE:    │ │  python db_chart.py         │
│    Test (select+main│ │   1) station gate  │ │  python station_calibration │
│  AudioTest          │ │   2) per-unit level│ │  python getFinalResults.py  │
│  MicroTestCloud     │ │                    │ │  python converter.py        │
│                     │ │                    │ │  VolumeHelper.exe           │
│                     │ │                    │ │  RefurbishTool.exe (opt.)   │
└──────┬──────────────┘ └─────┬──────────────┘ └─────┬────────────────────────┘
       │                      │                      │
       ▼                      ▼                      ▼
┌───────────────────────────────────────────────────────────────────────────┐
│  ARTIFACT LAYER — the working directory IS the inter-stage bus             │
│  serial.txt · Prueba_*.txt · hearingPassResults.txt · MicroTest_*.txt     │
│  recorded*.wav · results.json · knob_left/right.json · audio_plays.json   │
│  calibracion.txt · station_calibration.json · tiempo1/2.txt               │
│  → final_results.json → final_results_converted.xml                        │
└──────────────────────────────────┬────────────────────────────────────────┘
                                   ▼
                     POST DataWipeResultV2 → cloud API
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| **SennheiserTestRunner** | Sole entry point; kills stale processes, cleans artifacts, runs the stage sequence, sets exit codes, owns `runner_log.txt` | `apps/SennheiserTestRunner/Program.cs` |
| **Station-calibration gate** | Decides whether the bench must be re-verified against a Golden Unit; hosts a `HeadPhoneTest2.Form1` with `STATION_CALIB=1`; locks the station with exit 6 on FAIL | `apps/SennheiserTestRunner/Program.cs:142-231` |
| **AskForSerial2** | Operator serial entry dialog; writes `serial.txt` and closes | `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/Form1.cs:33-45` |
| **BluetoothHeadphoneTest** (controls) | Device enumeration + selection, capability-driven test-panel sequence, AVRCP/hotkey capture, writes `Prueba_*.txt` | `apps/FunctionalButtonTest/` (whole project) |
| **AudioTest** | Operator listening check per physical connection type; writes `hearingPassResults.txt` | `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs` |
| **MicroTestCloud** | Microphone level/behaviour check; writes `MicroTest_*.txt` + `MicroTest_*.wav` | `apps/MicroTestCloud/MicroTestCloud/Form1.cs` |
| **HeadPhoneTest2 / LevelTest** | Sweep playback + E.A.R.S. recording, RS195 balance-knob takes, ambient calibration, 4-step station calibration; writes `recorded*.wav`, `results.json`, `knob_*.json`, `audio_plays.json`, `calibracion.txt` | `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs` |
| **VolumeHelper** | Out-of-process setter for the default playback endpoint volume (0-100) | `tools/VolumeHelper/Program.cs` |
| **db_chart.py** | WAV → per-channel RMS/peak/crest/dBFS + signal-presence + `channel_active`; writes JSON | `scripts/db_chart.py` |
| **station_calibration.py** | 4-check Golden-Unit verdict → `station_calibration.json` | `scripts/station_calibration.py` |
| **getFinalResults.py** | Aggregates every raw artifact into `final_results.json` | `scripts/getFinalResults.py` |
| **converter.py** | `final_results.json` → `DataWipeResultV2` XML + HTTP upload | `scripts/converter.py` |
| **common.py** | Shared config/baseline/channel-dbfs helpers for the Python side | `scripts/common.py` |
| **test_signal_detection.py** | Framework-free self-checks for signal detection, knob verdict and XML building | `scripts/test_signal_detection.py` |

## Pattern Overview

**Overall:** Sequential Orchestrator with File-Based Hand-Off (a "poor man's data bus").

**Key Characteristics:**
- **Single process, modal stages.** Every test app is a `ProjectReference` of
  `SennheiserTestRunner.csproj:14-19`. The orchestrator `new`s each app's `Form1` and
  calls `ShowDialog()` — no child process, no IPC. The one exception is external tooling
  (`RunProcess`, `Program.cs:476-498`).
- **Artifacts as the contract.** Each stage's *only* output channel to the next stage is a
  file in `BaseDir`. A stage "succeeded" iff its expected file exists.
- **Two audiences for artifacts.** The Python scripts consume the same files the C# apps
  write; there is no second, private channel.
- **Environment variables as a config bus.** `DEVICE_NAME`, `STATION_CALIB`,
  `QUICK_AUDIO`, `CALIBRATION` are set by the orchestrator and read by the forms.
- **Verdicts deferred to Python.** The C# side mostly collects numbers; PASS/FAIL rules
  live in `getFinalResults.py` / `station_calibration.py` where they are testable.
- **Operator-in-the-loop.** Nearly every stage is a wizard; the human is the sensor for
  "does it sound right".

## Layers

**Orchestration Layer:**
- Purpose: define the production sequence, gate it, and exit with a machine-readable code.
- Location: `apps/SennheiserTestRunner/Program.cs` (single 499-line file, one `static class Program`)
- Contains: stage methods `RunDailyStationCalibration`, `LaunchRefurbishTool`,
  `ShowBluetoothConnectPrompt`, `GetSerial`, `RunControlsTest`, `RunAudioTest`,
  `RunMicrophoneTest`, `SetVolume`, `RunLevelTest`, `RunResultsScripts`, `CleanupBluetooth`
- Depends on: all five app assemblies, `VolumeHelper.exe`, PowerShell, Python
- Used by: `bin/run.bat` only

**Stage Layer (UI + domain per app):**
- Purpose: one WinForms app per test, each owning its audio hardware interaction.
- Location: `apps/FunctionalButtonTest`, `apps/pruebasAudifonos/{AskForSerial2,AudioTest,LevelTest}`, `apps/MicroTestCloud`
- Contains: `Form1.cs` / `MainForm.cs` (view + logic fused), NAudio capture/playback, verdict display
- Depends on: NAudio, Win32 P/Invoke, sibling apps only via artifacts and env vars
- Used by: the orchestrator (in-process) — and each is independently runnable, which is why
  every app keeps its own `Program.cs`

**Analysis Layer (Python, out-of-process):**
- Purpose: DSP verdicts, aggregation, XML, upload. Stdlib only except optional `requests`.
- Location: `scripts/*.py`
- Contains: WAV decoding, dB math, verdict rules, `xml.etree` document building
- Depends on: `scripts/common.py`; optional `requests` (installed on demand by the runner,
  `Program.cs:408-428`)
- Used by: the orchestrator via `RunProcess("python", ...)`; also by `station_calibration.py`
  invoked from inside LevelTest

**Artifact Layer:**
- Purpose: the bus. No code lives here, everything depends on it.
- Location: `BaseDir` at runtime (normally `bin/`)
- Contains: see "Data Flow" below
- Used by: every stage, both C# and Python

**Presentation Hints Layer (PowerShell):**
- Purpose: modal operator instructions that need Windows Settings deep-links.
- Location: `show_bluetooth_connect.ps1`, `show_bluetooth_disconnect.ps1`, `show_bluetooth.ps1`
- Contains: WinForms built at runtime in PowerShell
- Used by: `ShowBluetoothConnectPrompt` / `ShowBluetoothDisconnectPrompt`

## Data Flow

### Primary Request Path (production run of one DUT)

Sequence is linear in `Program.Main`, `apps/SennheiserTestRunner/Program.cs:26-87`.
`Environment.CurrentDirectory` is set to `BaseDir` at line 32, so every relative path below
lands in the same folder.

| # | Stage | Launch mode | Reads | Writes | Gate |
|---|-------|-------------|-------|--------|------|
| 0 | `KillOldProcesses` (`:119`) | in-process | — | — | best-effort kill of 5 legacy worker names |
| 0 | `CleanOldFiles` (`:127`) | in-process | 11 glob patterns | — | deletes previous-run artifacts (note: `calibracion.txt` and `station_calibration.json` are deliberately **not** cleaned) |
| 1 | `RunDailyStationCalibration` (`:142`) | **in-process** `new HeadPhoneTest2.Form1()` + `ShowDialog()` (`:155`) | `station_calibration.json`, `scripts/config.json` | `calibracion.txt`, `results.json`, `station_calibration.json` | re-runs when missing / `FAIL` / older than `calibration_max_age_hours` (default 12 h). Failure ⇒ `Environment.Exit(6)` |
| 2 | `LaunchRefurbishTool` (`:99`) | child process, optional | — | — | silently continues if the exe is absent |
| 3 | `ShowBluetoothConnectPrompt` (`:233`) | child process (`powershell`) | — | — | skipped if the `.ps1` is absent |
| 4 | `tiempo1.txt` (`:50`) | file write | — | `tiempo1.txt` (Unix ms) | — |
| 5 | `GetSerial` (`:247`) | **in-process** `new AskForSerial2.Form1()` | `serial.txt` after dialog | `serial.txt` (by the dialog) | missing/empty ⇒ `Exit(1)` |
| 6 | `RunControlsTest` (`:256`) | **in-process** `DeviceSelectForm` → `MainForm` | `Prueba_*.txt` after dialog | `DEVICE_NAME` env var, `Prueba_<device>_<ts>.txt`, `Prueba_*.txt` | `WaitForFile("Prueba_*.txt", 5s)`; cancel or 5 failed attempts ⇒ `Exit(3)`. Returns the `Dispositivo:` line (`:445`) |
| 7 | `RunAudioTest` (`:305`) | **in-process** `new AudioTest.Form1()` | `hearingPass*.txt` | `hearingPassResults.txt` (`True`/`False`), `ear_microphone_capture*.wav`, `tests_conexiones.{json,txt}` | existence of `hearingPass*.txt`; 5 failed attempts ⇒ `Exit(2)` |
| 8 | `RunMicrophoneTest` (`:329`) | **in-process** `new MicroTestCloud.Form1()` | `MicroTest_*.txt` | `MicroTest_<ts>.txt`, `MicroTest_<ts>.wav` | `WaitForFile("MicroTest_*.txt", 5s)`; 5 failed attempts ⇒ `Exit(4)` |
| 9 | `SetVolume` (`:430`) | child process `VolumeHelper.exe` | — | system volume | 80 % for `MOMENTUM TW 4`, else 100 % (`:70`) |
| 10 | `RunLevelTest` (`:352`) | **in-process** `new HeadPhoneTest2.Form1()` (2nd instance) | `results.json` after dialog | `recorded.wav`, `results.json` (via `db_chart.py`), `audio_plays.json`, `knob_left.json`, `knob_right.json`, `recorded_knob_*.wav` | `results.json` exists; 5 failed attempts ⇒ `Exit(5)` |
| 11 | `tiempo2.txt` (`:76`) | file write | — | `tiempo2.txt` | — |
| 12 | `RunResultsScripts` (`:376`) | child processes | everything | `final_results.json`, `final_results_converted.xml`, HTTP POST | prefers `getFinalResults.exe`/`converter.exe`, falls back to `python scripts\*.py` |
| 13 | `CleanupBluetooth` (`:403`) + disconnect prompt | child process | — | — | PowerShell `Remove-PnpDevice` for Bluetooth class |
| 14 | `diferencia_minutos.txt` (`:84`) | file write | `tiempo1/2.txt` | `diferencia_minutos.txt` | — |

**State management:** there is no state store. Every stage is stateless between runs;
`TestSession` (`apps/FunctionalButtonTest/TestSession.cs`) lives only for the duration of
one controls dialog. The two cross-run state carriers are files the runner deliberately
leaves in place: `station_calibration.json` (gate freshness) and `calibracion.txt` (daily
ambient baseline). `Environment.SetEnvironmentVariable` carries the selected model from
the controls stage into AudioTest and LevelTest.

### Station Calibration Flow (pre-production gate)

```text
StationCalibrationCurrent()                       Program.cs:182
  reads station_calibration.json -> station_calibration == "PASS"
  and (UtcNow - time) <= calibration_max_age_hours()   Program.cs:212
        │
        ├─ current  -> log "PASS verificada y vigente" and return
        └─ stale/missing -> STATION_CALIB=1
              └─ new HeadPhoneTest2.Form1().ShowDialog()      Program.cs:155
                   Paso 1  30 s ambient capture, no audio
                           -> db_chart.py -> results.json
                           -> LevelTest writes calibracion.txt
                           -> check1Pass = max(L,R) <= ambient_max_dbfs
                   Paso 2  operator connects Golden Unit (connection_type from config.json)
                           -> play tone_1khz, record 40 s
                           -> db_chart.py -> results.json
                   ShowStationGoldenVerdict()                 Form1.cs:426
                     -> python station_calibration.py --results results.json
                                     --baseline calibracion.txt
                                     --config <config.json> --out station_calibration.json
                     -> PASS: "ESTACIÓN LIBERADA" + Close()
                     -> FAIL: red dialog, offer to repeat from Paso 1
              └─ re-check StationCalibrationCurrent()
                   false -> red lock dialog + Environment.Exit(6)
```

Notes:
- The station instance never writes `audio_plays.json` — `AddPlay` early-returns when
  `stationCalibrationMode` is set (`Form1.cs:250-257`), so the tone play is excluded from
  the per-unit `audio_test` tally.
- LevelTest calls `python` itself for `station_calibration.py`; the orchestrator does not.

### Audio Analysis Flow (per recording, inside LevelTest)

```text
playAudio(title)                      Form1.cs:677   (WaveOutEvent, audio\<title>.wav/.mp3)
  + startRecording(path)              Form1.cs:650   (WaveInEvent 44.1 kHz/16-bit/stereo)
        │ (PlaybackStopped -> stopActions, or Form1.Timer for timed takes)
        ▼
stopRecording()  ->  recorded*.wav
        ▼
RunPythonScript(wav, jsonOut)         Form1.cs:887
  python db_chart.py --input <wav> --json-out <json>
         [--baseline calibracion.txt]     (only when !calibrationMode and the file exists)
        ▼
db_chart.py: read_stereo_wav -> per-channel rms/peak/dbfs/crest
            evaluate_signal (floor | crest | SNR) -> signal_present / signal_reason
            channel_active -> "left" | "right" | "both" | "none"
        ▼
EvaluateResults(json)                 Form1.cs:941
  deserializes into MeasurementsResult (Form1.cs:1458)
  fills level_left/right, level_diff, peak, signal_present
        ▼
UI pass/fail icons for balance (|L-R| <= 2), volume (-30..-10), clipping (peak <= 0)
  + red "no se está detectando suficiente audio" banner when signal_present == false
```

### Aggregation & Upload Flow

```text
getFinalResults.py (cwd = BaseDir)
  serial*            -> serial
  hearingPass*       -> distorsion (True=PASS/else FAIL) [+ audio_fail=FAIL when FAIL]
  Prueba_*           -> model ("Dispositivo:" line) + 6 bluetooth control fields
  MicroTest_*        -> resultado_mic  (parses the "Resultado       : PASS" line)
  results.json       -> left/right dbfs+peak, balance, volume, clipping,
                        deteccion_senal (mirrors distorsion, NOT signal_present)
  station_calibration.json -> station_calibration, station_calibration_time
  knob_left/right.json     -> balance_knob, balance_knob_left, balance_knob_right (rs195 only)
  audio_plays.json         -> audio_test {runs, passed, result}
  tiempo1/2.txt            -> StartTime / EndTime (UTC)
        ▼  final_results.json
converter.py
  build_xml()  -> overall FAIL if ANY string field == "FAIL"
                  or audio_test.result == "FAIL"  (N/A and SKIPPED are neutral)
  pretty_with_ns() -> DataWipeResultV2 with ns0: prefix, one <subtest> per field
        ▼  final_results_converted.xml
  urllib.request POST to AZURE_API_ENDPOINT or config.json:endpoint
```

### RS195 Balance-Knob Flow (branch inside stage 10)

`isRS195` is derived from `DEVICE_NAME` in the LevelTest constructor (`Form1.cs:103-105`).
After the sweep results, "Siguiente" enters `StartKnobPhase()` (`Form1.cs:1051`):
delete stale `knob_*.json` / `recorded_knob_*.wav` → play `karmaPolice` at a random offset
→ 10 s take to `recorded_knob_left.wav` → operator flips the knob → 7 s take to
`recorded_knob_right.wav` → `db_chart.py` twice → `KnobVerdict()` (`Form1.cs:1220`) requires
exactly one active channel per take, opposite channels, and ≥ 15 dB separation. The
authoritative rule is recomputed in `getFinalResults.knob_verdict()` for the record.

## Key Abstractions

**`RunProcess(fileName, arguments, wait)`:**
- Purpose: the runner's only outbound process launcher (`Program.cs:476-498`).
- Examples: `Program.cs:112` (RefurbishTool), `:237`/`:243` (PowerShell), `:381`/`:387`/`:392`/`:398` (Python), `:441` (VolumeHelper)
- Pattern: `UseShellExecute = true`, `WorkingDirectory = BaseDir`, synchronous when `wait: true`, returns exit code or `-1` on exception (never throws).

**`ShowDialog()` stage invocation:**
- Purpose: run a whole app as a modal step.
- Examples: `Program.cs:155` (station gate), `:249` (serial), `:265`/`:286` (select + main), `:311` (audio), `:335` (mic), `:360` (level)
- Pattern: `using var form = new X.Form1(); form.ShowDialog();` — no `Application.Run` in the orchestrator; each `ShowDialog` spins its own message loop.

**`WaitForFile(pattern, maxSeconds)`:**
- Purpose: poll `BaseDir` for a glob until it appears (`Program.cs:463-474`).
- Pattern: 1 s `Thread.Sleep` granularity, 5 s budget, returns the first match or `null`. Retry detection for stages 6 and 8.

**`TestPanel` / `TestStepManager` (FunctionalButtonTest):**
- Purpose: capability-driven wizard. `DeviceProfile` (`Deviceprofile.cs`) declares which of the 6 checks apply; `TestStepManager.BuildSteps` (`TestStepManager.cs:62-89`) builds the panel list; each `TestPanel` reports via `TestCompleted(bool)`.
- Pattern: abstract `Panel` base + per-check subclasses (`HeadphonesOnPanel`, `BluetoothConnectionPanel`, `PlayPausePanel`, `TrackPanel`, `VolumePanel`) + `MiniPlayerWidget` + `SummaryPanel`.

**`DeviceProfileRegistry`:**
- Purpose: the model catalogue. `_btProfiles` (exact Windows name match), `_jackProfiles` (operator-chosen commercial names), `GenericAudioNames` (`Deviceprofileregistry.cs:18-101`).
- Pattern: static lookup with a permissive fallback (`GetProfile` returns an all-enabled profile for unknown models).

**`AppCommandRouter`:**
- Purpose: single funnel for media keys. Captures `WM_HOTKEY`, `WM_APPCOMMAND` and `WM_KEYDOWN` (`AppCommandRouter.cs:71-120`), maps them to `Keys`, drives the active `AudioPlayer` and raises `OnMediaKey` for panels.
- Pattern: static event + static `ActivePlayer`; `MainForm.WndProc` feeds every window message in.

**`DeviceProfile` + `TestSession` records:**
- Purpose: fixed 6-record result list (`TestSession.cs:55-90`) where non-applicable checks are `NotApplicable` and excluded from `AllPassed`, plus `BuildReportText()` which is the exact `Prueba_*.txt` format the aggregator parses.

**`MeasurementsResult` / `Measurement`:**
- Purpose: the C# mirror of `db_chart.py`'s JSON payload (`HeadPhoneTest2/Form1.cs:1448-1463`), bound by convention (lowercase property names) to the Python keys `measurements`, `dbfs`, `peak_dbfs`, `signal_present`, `signal_reason`.

## Entry Points

**`SennheiserTestRunner.exe` (primary):**
- Location: `apps/SennheiserTestRunner/Program.cs:27` — `static void Main()`, `[STAThread]`
- Triggers: `bin/run.bat` (`start /wait` on the exe) or a direct double-click
- Responsibilities: the whole production sequence, exit codes 1-6, `runner_log.txt`
- Note: this project has **no `.sln`** — it is only built through `batch/build-all.bat`, which publishes it self-contained single-file `win-x64`.

**`bin/run.bat` (launcher):**
- Location: `batch/run.bat:15`
- Triggers: operator double-click
- Responsibilities: locate `SennheiserTestRunner.exe` (in `bin\` or the parent folder), `start /wait`, propagate `%ERRORLEVEL%`.

**`batch/build-all.bat` (build):**
- Location: `batch/build-all.bat:32-33`
- Triggers: developer/operator after a .NET 9 SDK check
- Responsibilities: publish `VolumeHelper.exe`, publish `SennheiserTestRunner.exe` (both `win-x64`, self-contained, single-file, compression), then `xcopy`/`copy` the runtime files (`run.bat`, the two `.ps1`, `scripts\`, `miniDSP.jpg`, `PistaAudio\`, `audio\`, `karmaPolice.wav`) into `bin\`.

**Per-app `Program.cs` (standalone debug entry points):**
- `apps/FunctionalButtonTest/Program.cs:9` (select form → `Application.Run(MainForm)`)
- `apps/pruebasAudifonos/AudioTest/AudioTest/Program.cs`
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Program.cs`
- `apps/MicroTestCloud/MicroTestCloud/Program.cs`
- `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/Program.cs`
- Each app is double-clickable in isolation for bench debugging; the orchestrator never uses these.

**`scripts/test_signal_detection.py` (self-check entry):**
- Location: `scripts/test_signal_detection.py`
- Triggers: `python3 scripts/test_signal_detection.py`
- Responsibilities: plain-`assert` checks for signal detection, knob verdict and XML building. Exits non-zero on failure.

## Architectural Constraints

- **Threading:** single STA UI thread. The orchestrator has no background workers; every
  stage is a nested message loop. `DeviceSelectForm` uses
  `ThreadPool.QueueUserWorkItem` only to enumerate Bluetooth devices off-thread
  (`DeviceSelectForm.cs:251`); `VolumeMonitor` and `AudioEndpointVolumeCallback` deliver NAudio
  volume notifications from a COM callback thread and marshal with `InvokeRequired`
  (`VolumeMonitor.cs:27`).
- **No process isolation:** because all test apps are in the same process, a hard fault in
  one takes down the whole run. This is why the exit codes are checked in a `using` block
  and why `runner_log.txt` (`Program.cs:34`) is opened with `AutoFlush` — it is the only
  forensic trail after a crash.
- **Global state (module-level singletons):**
  - `BluetoothHeadphoneTest.AppCommandRouter.ActivePlayer` / `OnMediaKey` — `apps/FunctionalButtonTest/AppCommandRouter.cs:14-17`
  - `BluetoothHeadphoneTest.DeviceAssets.DeviceName` (static mutable string) — `apps/FunctionalButtonTest/Deviceassets.cs:13`
  - `Program._log` — `apps/SennheiserTestRunner/Program.cs:24`
  - `Program.BaseDir` / `RootDir` are computed from `Environment.ProcessPath`; `RootDir` is
    the *parent* of `BaseDir`, i.e. `bin\`'s parent, which is where `RefurbishToolArvato` is
    probed (`Program.cs:10-17`, `:103-104`).
- **Circular project references:** none today. The graph is strictly a star —
  `SennheiserTestRunner` references all six projects (`SennheiserTestRunner.csproj:14-19`)
  and none of the leaves reference each other. **Any new `using` between two leaf apps would
  create a cycle and break the build.** Cross-app data must stay in files.
- **Process-global CWD mutation:** `Environment.CurrentDirectory = BaseDir` at
  `Program.cs:32` is what makes every relative artifact path (`"serial.txt"`,
  `"results.json"`, `"hearingPassResults.txt"`, `"audio_plays.json"`) resolve to one folder.
  Removing it silently splits the pipeline across folders.
- **Environment-variable coupling:** `DEVICE_NAME` is written by the controls stage
  (`Program.cs:281`) and read by `AudioTest/Form1.cs:78` and `HeadPhoneTest2/Form1.cs:103`.
  It is never cleared, so a stale value leaks into a later standalone run.
  `STATION_CALIB` is the only variable with a matching `finally { SetEnvironmentVariable(..., null) }`
  (`Program.cs:152-161`).
- **Artifact double-write:** several writers save to both `AppDomain.CurrentDomain.BaseDirectory`
  and `Directory.GetCurrentDirectory()` (`MainForm.cs:118-121`, `SummaryPanel.cs:251-254`,
  `MicroTestCloud/Form1.cs:550-554`, `:1535-1541`). In the orchestrated run these are the
  same directory, so it is a harmless legacy of the standalone era; standalone debugging is
  where it matters.
- **Text parsing is positional, not a schema.** `getFinalResults.py` reads human-readable
  Spanish report lines by whitespace index (`parts[1]`, `parts[2]`, `parts[3]`) and decides
  the microphone result with `"PAS" in parts[2]`. Reformatting any `.txt` report breaks the
  aggregator silently.
- **Retry contract:** `MaxRetries = 5` is used by four stages, but `RetryDelayMs = 2000`
  (`Program.cs:22`) is **never referenced** — the retry loops re-open the dialog immediately.
  The README's "2 s wait between attempts" does not match the code.

## Anti-Patterns

### Success detected by file polling instead of a return value

**What happens:** after `ShowDialog()` returns, the runner ignores `DialogResult` and polls
the filesystem for up to 5 s (`Program.cs:288`, `:314`, `:338`, `:363`).
**Why it's wrong:** the form already ran to completion in-process; the artifact is being
used as a "did it work" flag rather than a data channel. `WaitForFile` adds a 5 s stall to
every successful stage and a 5 s stall per failed attempt.
**Do this instead:** the apps should expose a public result property (as
`BluetoothHeadphoneTest.MainForm` already does via `Session`), e.g. read
`form.Session.AllPassed` directly and keep the file write purely for the Python aggregator.
If the file check must stay, `AudioTest`/`MicroTestCloud` should expose `WriteReport` results
as return values (`MicroTestCloud/Form1.cs:1108` already has a `_reportGenerated` flag).

### Environment variables as a cross-app configuration bus

**What happens:** the runner passes the selected model to two other apps by mutating the
process environment (`Program.cs:281` → read at `AudioTest/Form1.cs:78` and
`HeadPhoneTest2/Form1.cs:103`).
**Why it's wrong:** the coupling is invisible — nothing in `AudioTest.csproj` or
`HeadPhoneTest2.csproj` mentions `DEVICE_NAME`, so a rename of the variable or a new
consumer is found only by grep. The value is never reset, so it survives into unrelated runs.
**Do this instead:** the orchestrator already holds `MainForm` alive in `RunControlsTest`
(`Program.cs:284-296`); pass the device object as a constructor argument. Until then, if you
must add a variable, mirror the `STATION_CALIB` pattern and null it in a `finally`
(`Program.cs:152-161`).

### Positional parsing of human-readable reports

**What happens:** `getFinalResults.py:122-144` splits report lines on whitespace and reads
`parts[2]`/`parts[3]`; the microphone verdict is `"PAS" in parts[2]` (`:342`).
**Why it's wrong:** the report writers (`TestSession.BuildReportText()`,
`MicroTestCloud.SaveReport()`) are free to change column padding — e.g. a double space after
`Resultado` — and the aggregator would then read the wrong token with no error. It also
means the `N/A` microphone path (`MicroTestCloud/Form1.cs:504`, `Resultado : N/A`) parses
as FAIL, and `No definido` (`:1121`) parses as FAIL too.
**Do this instead:** have the writers emit a machine-readable sibling file
(`MicroTest_<ts>.json`) next to the human report — `MicroTestCloud.WriteReport`
(`:544-557`) already centralises writing, and `tests_conexiones.json`
(`AudioTest/Form1.cs:311`) is the precedent for that pattern.

### `Application.Exit()` inside a hosted stage

**What happens:** AudioTest signals completion with `Application.Exit()`, not `this.Close()`
(`AudioTest/Form1.cs:138`, `:174`, `:191`, `:207`, `:247`) — also on cancel and on every
error path.
**Why it's wrong:** an in-process stage must not ask the *application* to exit; the runner
is the application. It works today only because the orchestrator never calls
`Application.Run`, so the process survives and `ShowDialog` simply returns with the form
already disposed — the file check is what actually advances the pipeline.
**Do this instead:** `Close()` in AudioTest (matching `AskForSerial2/Form1.cs:43` and
`HeadPhoneTest2/Form1.cs:494`) and let the orchestrator's gate decide. Keep the
`hearingPassResults.txt` "False" fallback at `Form1.cs:104` so a cancelled dialog still
produces a FAIL verdict instead of a retry.

### Split responsibility between the C# UI and the Python verdict layer

**What happens:** the RS195 knob verdict is implemented twice —
`HeadPhoneTest2/Form1.cs:1220-1260` (`KnobVerdict`) for the on-screen dialog, and
`getFinalResults.py:183-225` (`knob_verdict`) for the uploaded record. Same constants
(`KNOB_SEPARATION_DB = 15.0` in both), two implementations.
**Why it's wrong:** the two can drift silently; a change to one is not forced into the other
and nothing fails until a real RS195 unit is tested. The `NoVolumeModels` /
`MODELS_WITHOUT_VOLUME` model lists are duplicated the same way
(`HeadPhoneTest2/Form1.cs:82` vs `getFinalResults.py:50`).
**Do this instead:** keep the Python rule as the single source of truth (it is the testable
one — `test_signal_detection.py:159-172` covers it) and have the C# side read the verdict it
already wrote to `knob_*.json` instead of recomputing. This is a deliberate duplication
today; do not extend it to new checks.

### Legacy process-management code for a design that no longer exists

**What happens:** `KillOldProcesses` (`Program.cs:119-125`) kills processes named
`AskForSerial2`, `AudioTest`, `BluetoothHeadphoneTest`, `MicroTestCloud`, `LevelTest` — but
the runner never launches any of them as a child process.
**Why it's wrong:** it silently kills a developer's standalone debugging session of the same
app if they happen to have it open, and it implies a process-isolation model that does not
exist.
**Do this instead:** delete the loop and note in the stage methods that stages are in-process.
If a stale-process guard is genuinely needed later, target only the names the runner
actually starts (`python`, `VolumeHelper`).

## Error Handling

**Strategy:** fail-loud for the station gate, fail-by-retry for the production stages, and
best-effort everywhere else. There is no exception type, no result object, and no error
collector — a stage communicates failure by not writing its file.

**Patterns:**
- **Station gate is fail-stop.** Any FAIL ⇒ red `MessageBox` + `Environment.Exit(6)`
  (`Program.cs:169-179`); no fallback, no retry.
- **Production stages retry up to 5×** then `Environment.Exit(2|3|4|5)`
  (`Program.cs:326`, `:349`, `:373`, `:62`).
- **Missing serial is a hard stop** with exit 1 (`Program.cs:56`).
- **Silent degradation is deliberate and common.** `RunProcess` catches everything and
  returns `-1` (`Program.cs:493-497`); `EnsurePythonRequests` is wrapped in a bare
  `catch { }` (`:427`); `CleanOldFiles` and `StartKnobPhase`'s cleanup swallow IO errors
  (`:133`, `:1056`); `ParseDeviceName` returns `null` on any failure (`:459`). The pattern
  is "a test-bench nicety must never stop the line".
- **Writer-side self-healing.** Every writer makes sure its artifact exists even on the
  failure path: `AudioTest` writes `"False"` to `hearingPassResults.txt` on cancel, on
  missing audio devices, and on playback errors (`Form1.cs:104`, `:137`, `:173`, `:188`);
  `MainForm.WriteFallbackReportIfMissing` (`:79-127`) and
  `MicroTestCloud.SaveFallbackReport` (`:1108-1143`) write a minimal report on close if the
  normal one was never produced. This is why the orchestrator's file check is meaningful.
- **Python is exception-tolerant by default.** `getFinalResults.py` wraps every optional
  input in `try/except` and substitutes `N/A` or `SKIPPED` (`--some` flag,
  `getFinalResults.py:270-273`), so a missing input degrades the record instead of aborting
  the upload. `converter.py` catches upload failures and prints `Upload status: FAILED`
  without a non-zero exit.
- **One exception that does throw:** `RunPythonScript` raises when `db_chart.py` exits
  non-zero (`HeadPhoneTest2/Form1.cs:914-915`), which bubbles to the `MessageBox` in
  `stopActions` (`:879`) — the level-test result screen then never appears, so the stage
  writes no `results.json` and the orchestrator retries.

## Cross-Cutting Concerns

**Logging:** a single `Log(string, bool isError)` writing timestamped lines to
`bin\runner_log.txt` with `AutoFlush` (`Program.cs:34`, `:89-97`) and mirroring to
Console. Truncated (`append: false`) on every run. The apps themselves do not log; their
only trace is `runner_log.txt` plus the artifacts.

**Validation:** almost none in C#. `AudioTest` clamps device indices and bails out when
`WaveOut.DeviceCount == 0` (`Form1.cs:166-176`); `VolumeHelper` clamps the percentage
(`Program.cs:9`); the level verdict logic checks for a missing `results.json` and missing
L/R measurements (`HeadPhoneTest2/Form1.cs:943-957`). The real validation lives in the
Python verdict layer, and the trust boundary that matters is the artifact layer, which is
parsed defensively with encoding fallbacks (`read_text_file`, `getFinalResults.py:84-95`).

**Authentication:** none. `converter.py` posts to a pre-configured URL that carries its own
`code=` token in the query string; `USERNAME` defaults to `tester1`. `AZURE_API_ENDPOINT`
overrides `scripts/config.json:endpoint`. `requests` is installed on demand at runtime
(`Program.cs:408-428`) but `converter.py` itself uses `urllib.request` and stdlib only.

**Hardware integration (implicit but load-bearing):** NAudio (`WaveInEvent`, `WaveOutEvent`,
`MMDeviceEnumerator`, `SignalGenerator`) for audio, `BluetoothApis.dll` P/Invoke for
paired-device discovery (`BluetoothDetector.cs:87-90`), `RegisterHotKey` for AVRCP/media
keys (`AppCommandRouter.cs:25-26`), `System.Management` (referenced by
`MicroTestCloud.csproj`) for device inventory, and PowerShell `Get-PnpDevice` /
`Remove-PnpDevice` for Bluetooth cleanup (`Program.cs:405`).

**Configuration:** three sources with different precedence — environment variables
(`DEVICE_NAME`, `QUICK_AUDIO`, `CALIBRATION`, `STATION_CALIB`, `AZURE_API_ENDPOINT`,
`USERNAME`), `scripts/config.json` (per-machine golden values, API endpoint,
`calibration_max_age_hours`), and hard-coded defaults in the scripts
(`getFinalResults.py:28-56`, `station_calibration.py:23-27`, `db_chart.py:18-20`).

**Internationalisation:** none, and intentionally so — artifact keys, verdict values
(`PASS`/`FAIL`/`N/A`/`SKIPPED`), report field names (`Dispositivo`, `Resultado`) and all
operator prompts are hard-coded Spanish/English literals because they are parsed by the
aggregator. Comments and identifiers are mixed: Spanish in `FunctionalButtonTest` and
`LevelTest`, English elsewhere.

---

*Architecture analysis: 2026-09-28*
