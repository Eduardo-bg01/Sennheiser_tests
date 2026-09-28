# Codebase Structure

**Analysis Date:** 2026-09-28

## Directory Layout

```
Sennheiser_tests/
├── batch/                      # Build + launch scripts (the only supported build path)
│   ├── build-all.bat           #   publishes runner + VolumeHelper, copies runtime files to bin\
│   └── run.bat                 #   thin launcher for bin\SennheiserTestRunner.exe
├── apps/                       # All WinForms test apps
│   ├── SennheiserTestRunner/   #   ★ ORCHESTRATOR (no .sln; entry point of the whole bench)
│   ├── FunctionalButtonTest/   #   Controls test → assembly BluetoothHeadphoneTest.exe
│   ├── MicroTestCloud/         #   Microphone test (+ nested MicroTestCloud/ project folder)
│   └── pruebasAudifonos/       #   "headphone tests" (legacy Spanish group name)
│       ├── AskForSerial2/      #     Serial entry (nested AskForSerial2/ project folder)
│       ├── AudioTest/          #     Listening check (nested AudioTest/ project folder)
│       └── LevelTest/          #     Level/sweep/knob/calibration → assembly LevelTest.exe
│                                  (project folder HeadPhoneTest2/ inside solution LevelTest)
├── tools/
│   └── VolumeHelper/           # Console app: set default playback volume 0-100
├── scripts/                    # Python analysis/aggregation/upload (stdlib only)
│   ├── common.py               #   shared config + baseline + channel-dbfs helpers
│   ├── db_chart.py             #   WAV → dBFS/peak/crest + signal presence
│   ├── station_calibration.py  #   4-check Golden-Unit verdict
│   ├── getFinalResults.py      #   all artifacts → final_results.json
│   ├── converter.py            #   final_results.json → DataWipeResultV2 XML → HTTP POST
│   ├── test_signal_detection.py#   framework-free self-checks (the only tests in the repo)
│   └── config.json             #   ⚠ per-machine site config — GITIGNORED, never commit
├── show_bluetooth_connect.ps1  # Operator prompt: connect the headset (WinForms in PowerShell)
├── show_bluetooth_disconnect.ps1
├── show_bluetooth.ps1          # Not invoked by the orchestrator; manual/diagnostic helper
├── miniDSP.jpg                 # Copied to bin\ by build-all.bat (coupler reference image)
├── README.md                   # Authoritative pipeline documentation (verify against code!)
├── .gitattributes              # *.bat and *.ps1 forced to CRLF
├── .gitignore                  # ignores bin/, obj/, artifacts, and scripts/config.json
└── .planning/
    └── codebase/               # GSD analysis documents
```

At runtime, `bin/` becomes a **flat working directory** that is the inter-stage bus:

```
bin/
├── SennheiserTestRunner.exe   # self-contained single-file publish
├── VolumeHelper.exe           # robocopy'd from a temp publish dir
├── run.bat, show_bluetooth_*.ps1, miniDSP.jpg
├── scripts/                   # xcopy of the repo's scripts/ (incl. local config.json)
├── audio/                     # xcopy of LevelTest/HeadPhoneTest2/audio + karmaPolice.wav
├── PistaAudio/                # xcopy of MicroTestCloud/.../PistaAudio
├── runner_log.txt             # orchestrator log (truncated each run)
└── <all per-run artifacts — see below>
```

## Directory Purposes

**`apps/SennheiserTestRunner/`:**
- Purpose: the orchestrator. Owns the stage sequence, the station gate, the volume
  choreography, the result-file cleanup, the exit codes and `runner_log.txt`.
- Contains: `Program.cs` (499 lines, one `static class Program`), `SennheiserTestRunner.csproj`
- Key files: `Program.cs` (everything), `SennheiserTestRunner.csproj:14-19` (the six `ProjectReference`s)

**`apps/FunctionalButtonTest/`:**
- Purpose: Bluetooth/USB control verification against the headset's real buttons, plus
  device selection. Emits `Prueba_*.txt`.
- Contains: forms, capability-driven panels, AVRCP router, generated-tone audio player, embedded GIF/JPG assets
- Key files: `DeviceSelectForm.cs`, `MainForm.cs`, `TestStepManager.cs`, `TestSession.cs`, `TestPanels.cs`, `AppCommandRouter.cs`, `Deviceprofileregistry.cs`, `Deviceassets.cs`, `BluetoothDetector.cs`, `SummaryPanel.cs`, `AudioPlayer.cs`, `VolumeMonitor.cs`, `Colors.cs`
- Note: the folder is `FunctionalButtonTest` but the assembly/namespace is `BluetoothHeadphoneTest` — rename neither casually.

**`apps/MicroTestCloud/`:**
- Purpose: microphone diagnostics (level sampling, playback of the operator's voice, or a
  `PistaAudio` track), operator PASS/FAIL verdict, `MicroTest_*.txt` + `.wav` report.
- Contains: one project folder, a fully hand-built `Form1` (1552 lines), a custom progress
  bar, a custom borderless title bar, and a `FormResultado` modal.
- Key files: `MicroTestCloud/Form1.cs` (all of it), `MicroTestCloud/Program.cs`, `MicroTestCloud/MicroTestCloud.csproj`, `MicroTestCloud/PistaAudio/*.mp3`
- Note: despite the name there is no cloud code — the cloud hand-off is `converter.py`'s job.

**`apps/pruebasAudifonos/`:**
- Purpose: the three dialog-style test apps, grouped under a legacy Spanish name that no
  longer describes the content. This is the historical home of the bench.
- Contains: `AskForSerial2/`, `AudioTest/`, `LevelTest/` (each a `.sln` + a nested project folder)
- Key files: `LevelTest/HeadPhoneTest2/Form1.cs` (1463 lines — the largest piece of test
  logic), `AudioTest/AudioTest/Form1.cs` (770 lines), `AskForSerial2/AskForSerial2/Form1.cs` (125 lines)
- `apps/pruebasAudifonos/README.md` exists but is **empty**.

**`apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/`:**
- Purpose: the only app with three operating modes, switched purely by environment variable:
  - default → play `audioSweep`, record via E.A.R.S., analyse, show balance/volume/clipping
  - `CALIBRATION=1` → 30 s ambient capture, no playback → `calibracion.txt`
  - `STATION_CALIB=1` → the 4-step Golden-Unit wizard → `station_calibration.json`
  Plus the RS195 balance-knob branch (auto-enabled when `DEVICE_NAME` normalises to `rs195`).
- Contains: `Form1.cs`, `Form1.Designer.cs` (870 lines), `Properties/Resources.*` (check/x icons), `audio/`
- Key files: `Form1.cs:142-513` (mode detection + station config), `:650-738` (record/play),
  `:789-968` (analysis + verdict rendering), `:1035-1260` (knob flow)

**`tools/VolumeHelper/`:**
- Purpose: a 32-line console exe that sets the default render endpoint volume, so the
  orchestrator and `AudioTest` can control volume without a UI. Exit 0/1/2.
- Contains: `Program.cs`, `VolumeHelper.csproj`
- Key files: `Program.cs` (top-level statements, no `Main` method)

**`scripts/`:**
- Purpose: all out-of-process analysis, aggregation and upload.
- Contains: 5 modules + 1 self-check + local config
- Key files: `db_chart.py` (DSP), `station_calibration.py` (gate verdict),
  `getFinalResults.py` (the aggregator — the most important file in the repo),
  `converter.py` (XML + HTTP), `common.py` (shared helpers)

**`batch/`:**
- Purpose: the only supported build path. There is no root `.sln` and no `.sln` for the
  orchestrator, so `dotnet build` on a solution will never produce the shipped artifact.
- Contains: `build-all.bat`, `run.bat`
- Key files: `build-all.bat:9` (`PUBLISH_FLAGS`), `:23` (VolumeHelper), `:33` (runner),
  `:40-67` (runtime-file copy), `run.bat:6-15` (exe discovery + `start /wait`)

## Key File Locations

**Entry Points:**
- `apps/SennheiserTestRunner/Program.cs:27` — `Main()`, the production pipeline
- `batch/run.bat:15` — operator launcher (`start /wait` on the exe)
- `batch/build-all.bat:33` — `dotnet publish` of the orchestrator
- `apps/FunctionalButtonTest/Program.cs:9` — standalone controls-test entry
- `apps/pruebasAudifonos/AudioTest/AudioTest/Program.cs` — standalone listening-check entry
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Program.cs` — standalone level-test entry
- `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/Program.cs` — standalone serial entry
- `apps/MicroTestCloud/MicroTestCloud/Program.cs` — standalone microphone-test entry
- `scripts/test_signal_detection.py` — the self-check entry (`python3 scripts/test_signal_detection.py`)

**Configuration:**
- `scripts/config.json` — per-machine: `endpoint`, `golden_left_dbfs`, `golden_right_dbfs`,
  `golden_tolerance_db`, `balance_max_db`, `ambient_max_dbfs`, `connection_type`,
  `calibration_max_age_hours`. **Gitignored.** The checked-out copy in this repo contains
  only an empty `endpoint`. Read it in C# via `CalibrationMaxAgeHours()`
  (`Program.cs:212-231`) and `LoadStationConfig()` (`HeadPhoneTest2/Form1.cs:206-248`); read
  it in Python via `common.load_config()`.
- `apps/SennheiserTestRunner/SennheiserTestRunner.csproj:14-19` — the ProjectReference list;
  adding a stage app means adding a line here
- `batch/build-all.bat:9` — publish flags (`win-x64`, self-contained, single-file, compression)
- `.gitattributes` — CRLF for `*.bat` / `*.ps1`
- `.gitignore` — `bin/`, `obj/`, all run artifacts, `scripts/config.json`

**Core Logic:**
- `apps/SennheiserTestRunner/Program.cs:142-180` — station-calibration gate (the hard lock)
- `apps/SennheiserTestRunner/Program.cs:256-303` — controls stage + `DEVICE_NAME` plumbing
- `apps/SennheiserTestRunner/Program.cs:352-374` — level stage + `EnsurePythonRequests`
- `apps/SennheiserTestRunner/Program.cs:463-498` — `WaitForFile` + `RunProcess` (the two
  primitives every other stage is built from)
- `apps/FunctionalButtonTest/TestStepManager.cs:62-89` — capability-driven step construction
- `apps/FunctionalButtonTest/TestSession.cs:55-90` — the 6 fixed records; `:124-163` — the
  `Prueba_*.txt` format the aggregator parses
- `apps/FunctionalButtonTest/AppCommandRouter.cs:71-120` — media-key capture (3 channels)
- `apps/FunctionalButtonTest/Deviceprofileregistry.cs:18-101` — the model catalogue (add
  models here, not anywhere else)
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:887-939` — `db_chart.py` launcher
  and script-path resolution
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:426-513` — station verdict +
  `station_calibration.py` launcher
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:1035-1260` — RS195 knob flow
- `scripts/db_chart.py:172-231` — `channel_signal_ok` / `evaluate_signal` / `channel_active`
- `scripts/getFinalResults.py:146-225` — level thresholds + `knob_verdict`
- `scripts/getFinalResults.py:267-398` — `main()`, the whole aggregation contract
- `scripts/converter.py:63-143` — `build_xml()` and the overall PASS/FAIL rule

**Testing:**
- `scripts/test_signal_detection.py` — the **only** test file in the repo. Plain `assert`,
  no framework, no test directory. Runs on demand; nothing in `build-all.bat` invokes it.
- `scripts/test_signal_detection.py:27-88` — WAV/fixture generators (`write_stereo_wav`,
  `make_chirp`, `make_ambient`)
- `scripts/test_signal_detection.py:88-173` — signal-detection, baseline, `channel_active`,
  `knob_verdict` assertions
- No C# test project exists anywhere in the tree.

**Documentation:**
- `README.md` — the intended pipeline, result schema, env-var table and site-config table.
  Mostly accurate; two known drifts: it claims a 2 s wait between retries (the
  `RetryDelayMs` constant at `Program.cs:22` is never used) and describes the orchestrator
  as running the apps "in-process" (correct) while the runner still carries child-process
  bookkeeping for workers it never launches.
- `apps/pruebasAudifonos/LevelTest/README.md` — RS195 knob-test flow.
- `apps/FunctionalButtonTest/README.md`, `apps/MicroTestCloud/MicroTestCloud/README.md` — per-app notes.
- `apps/*/.github/copilot-instructions.md` (3 files) — boilerplate Azure MCP rules, not
  project-specific guidance.

**Assets (media):**
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/audio/audioSweep.mp3` — the per-unit sweep
- `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/audio/tone_1khz.wav` — station-calibration tone
- `apps/pruebasAudifonos/AudioTest/AudioTest/karmaPolice.wav` — listening test + RS195 knob takes
- `apps/MicroTestCloud/MicroTestCloud/PistaAudio/*.mp3` — mic-test track + spoken prompt
- `apps/FunctionalButtonTest/assets/<model>/*.gif` — animated button-press hints, **embedded**
  as resources (`BluetoothHeadphoneTest.csproj:16-19`)

## Naming Conventions

**Files:**
- C#: `PascalCase.cs` (`TestStepManager.cs`), and for WinForms always the pair
  `Form1.cs` / `Form1.Designer.cs` / `Form1.resx` / `Properties/Resources.*`
- Python: `snake_case.py` for modules, `snake_case()` for functions
- Batch/PowerShell: `kebab-or-lowercase` — `build-all.bat`, `run.bat`, `show_bluetooth_connect.ps1`
- Artifacts: Spanish, mixed case, underscore-separated
  (`Prueba_<device>_<yyyyMMdd_HHmmss>.txt`, `MicroTest_<yyyyMMdd_HHmmss>.txt`, `.wav`,
  `hearingPassResults.txt`, `calibracion.txt`, `tiempo1.txt`, `diferencia_minutos.txt`)
- Machine-readable JSON is always `lowercase_snake_case` and **not** timestamped:
  `results.json`, `final_results.json`, `station_calibration.json`, `knob_left.json`,
  `knob_right.json`, `audio_plays.json`, `tests_conexiones.json`
- Two exceptions that are load-bearing: `calibracion.txt` has a `.txt` extension but holds
  JSON, and `tiempo1/2.txt` hold bare Unix-millisecond integers

**Directories:**
- `apps/` = one folder per app; `tools/` = console utilities; `scripts/` = Python;
  `batch/` = build/launch; `.planning/` = GSD state
- Three apps have a **doubly-nested project folder** because each `.sln` sits beside its
  project: `MicroTestCloud/MicroTestCloud/`, `AskForSerial2/AskForSerial2/AskForSerial2/`,
  `AudioTest/AudioTest/`, `LevelTest/HeadPhoneTest2/`. Commands must be run against the
  inner `.csproj`, not the `.sln` folder.
- The orchestrator and `VolumeHelper` are single-level (no `.sln`)

**Identifiers (inconsistent, mirror the neighbouring file):**
- C#: `PascalCase` types and methods, `_camelCase` for `Form1` fields, `camelCase` for
  `TestStepManager` fields, `[camelCase]` for JSON-bound properties
  (`HeadPhoneTest2/Form1.cs:1448-1463`), `SCREAMING_SNAKE` for module constants
  (`KNOB_SEPARATION_DB`, `SIGNAL_MIN_DBFS`)
- File-name/style mismatch to be aware of: `DeviceSelectForm.cs` (PascalCase) and
  `TestPanels.cs` sit in a project whose other files are `Deviceprofileregistry.cs`,
  `Deviceassets.cs`, `TestStepManager.cs`, `AppCommandRouter.cs` (lowercase first letter)
- Python: functions `snake_case`, module constants `UPPER_SNAKE`, and — inconsistent —
  `test_signal_detection.py` uses `snake_case` for its test functions while the other
  scripts use `camelCase`
- Comments and operator strings: Spanish in `FunctionalButtonTest` and `LevelTest`, English
  in `MicroTestCloud`, `VolumeHelper` and all of `scripts/`

## Where to Add New Code

**A new pipeline stage (a whole new test app):**
1. Create `apps/<NewApp>/<NewApp>/` (or single-level, matching the orchestrator's shape).
   Give it a `Program.cs` with a `[STAThread] Main` so it stays standalone-debuggable.
2. Write exactly one artifact file, `NewResult_*.txt` or `.json`, from
   `AppDomain.CurrentDomain.BaseDirectory` **and** `Directory.GetCurrentDirectory()`
   (see `SummaryPanel.cs:249-255` for the double-write precedent).
3. Write a "never leave the artifact missing" fallback on every exit path
   (`AudioTest/Form1.cs:104` and `MicroTestCloud/Form1.cs:1108` are the models) so the
   orchestrator's file check is meaningful.
4. Add a `ProjectReference` in `apps/SennheiserTestRunner/SennheiserTestRunner.csproj:13-20`.
5. Add a `RunXxxTest()` method next to `RunAudioTest` in `Program.cs:305`, with
   `for (int attempt = 1; attempt <= MaxRetries; attempt++)`, a `WaitForFile`/`File.Exists`
   gate, and a new exit code.
6. Register the new exit code in the README's exit-code table.
7. Add the artifact to `CleanOldFiles` (`Program.cs:129`).
8. If the stage needs config, read it in the stage and set an env var around the dialog
   following the `STATION_CALIB` `try/finally` pattern (`Program.cs:152-161`).
9. Copy any runtime assets in `batch/build-all.bat:40-67`.

**A new field in `final_results.json` / a new subtest in the XML:**
1. Compute the value in `scripts/getFinalResults.py:main()` (`:267-398`).
2. Add the name to `SUBTESTS` in `scripts/converter.py:34-40` — **append only**; existing
   `TestIDNumber` values are index-based and the backend depends on them.
3. If it is a machine-readable artifact, write it from the owning C# stage and add a
   `FILE_PATTERN_*` constant in `getFinalResults.py:15-25`.
4. Document the field in the README's `final_results.json` table.

**A new device model:**
- Bluetooth / USB-C / USB-A with a name Windows reports: add a `DeviceProfile` to
  `_btProfiles` in `apps/FunctionalButtonTest/Deviceprofileregistry.cs:18-65` (the comment
  block at `:52-64` is the recipe).
- Wired 3.5 mm / generic endpoint: add the commercial name to `JackModelNames` in
  `Deviceprofileregistry.cs:73-77`. Capabilities are derived automatically.
- If the model has no balance knob, add it to `MODELS_WITHOUT_VOLUME`
  (`scripts/getFinalResults.py:50`) **and** `NoVolumeModels`
  (`HeadPhoneTest2/Form1.cs:82`) — the lists are duplicated, keep them in sync.
- If the model has a balance knob, add it to `KNOB_MODELS` (`getFinalResults.py:56`).
- If it has a button-animation GIF, drop `assets/<name>/*.gif` under
  `apps/FunctionalButtonTest/assets/` (the glob in `BluetoothHeadphoneTest.csproj:17`
  embeds it automatically) and the code normalises spaces to underscores when resolving
  (`Deviceassets.cs:118-119`).

**A new audio stimulus:**
- LevelTest/calibration: drop the file into
  `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/audio/` and reference it by title
  (no extension) in `playAudio(...)`; resolution tries `.wav` then `.mp3` across
  `./audio`, `BaseDirectory/audio`, `Cwd/audio` (`HeadPhoneTest2/Form1.cs:740-777`).
  The attachment of the `PlaybackStopped` analysis hook is opt-in per title at
  `HeadPhoneTest2/Form1.cs:685-686` — add new titles there.
- Add the copy step in `batch/build-all.bat:59-66` if it lives outside `LevelTest/audio`.

**A new analysis/verdict:**
- Put it in `scripts/` as a new module, or extend `scripts/common.py` if it is a shared IO
  helper. `scripts/` must stay importable by plain `python scripts\x.py` with cwd =
  `bin\`, so import siblings directly (`from common import ...`, as `db_chart.py:11` does).
- If a C# form must call it, copy the launcher pattern at
  `HeadPhoneTest2/Form1.cs:887-919` (resolve with `ResolvePythonScriptPath`, run with
  `RedirectStandardError`, throw on non-zero exit).
- Add `assert`-based coverage to `scripts/test_signal_detection.py` — that file is the
  project's only test harness and is the reason the verdict rules live in Python.

**Shared helpers:**
- Cross-app C# utilities: **do not** create a shared library — it would need a
  `ProjectReference` from two leaves and create a cycle. Duplicate the helper
  (as `SharedTheme.cs`/`Colors.cs` already are, per app) or move it to `tools/` as a
  referenced exe like `VolumeHelper`.
- Per-app UI constants: `apps/FunctionalButtonTest/Colors.cs` for that app;
  `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/SharedTheme.cs` for that app. The
  palette is duplicated per app on purpose — the apps cannot reference each other.

## Special Directories

**`bin/` (created by `batch/build-all.bat`, does not exist in the repo):**
- Purpose: deployment root **and** runtime working directory — the inter-stage artifact bus
- Generated: Yes, entirely. `dotnet publish` output + `robocopy`/`xcopy`/`copy` of runtime files
- Committed: No (`.gitignore` line 2). It accumulates per-run artifacts that are never pruned
  except by the runner's own `CleanOldFiles` on the next run.

**`obj/`:**
- Purpose: NuGet restore/build intermediates
- Generated: Yes
- Committed: No (`.gitignore` line 3)

**`apps/FunctionalButtonTest/assets/`:**
- Purpose: per-model button-press GIFs and device photos, resolved by convention
- Generated: No — hand-authored, committed
- Committed: Yes, as **embedded resources** (`BluetoothHeadphoneTest.csproj:16-19`), so they
  ship inside the single-file exe and are found by manifest-name matching, not by path
  (`Deviceassets.cs:75-114`)

**`apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/audio/` and `apps/MicroTestCloud/MicroTestCloud/PistaAudio/`:**
- Purpose: runtime audio resolved by path (not embedded)
- Generated: No
- Committed: Yes, and `xcopy`d into `bin\audio` / `bin\PistaAudio` by `build-all.bat:52-62`

**`scripts/config.json`:**
- Purpose: per-machine site config (API endpoint, golden reference levels, thresholds)
- Generated: No — hand-edited per station
- Committed: **No** (`.gitignore` final block). A local copy exists in this working tree
  containing only an empty `endpoint`; the golden values must be set on each bench.

**`apps/*/Properties/`, `apps/*/Form1.resx`, `apps/*/Form1.Designer.cs`:**
- Purpose: WinForms designer artifacts
- Generated: Yes (by the Visual Studio designer)
- Committed: Yes, necessarily. `Form1.Designer.cs` should never be hand-edited; runtime
  theming in these apps is applied in code after `InitializeComponent()`
  (`ApplyCohesiveTheme()` in `AskForSerial2/Form1.cs:47`, `AudioTest/Form1.cs`,
  `HeadPhoneTest2/Form1.cs:1305`), not in the designer.

**`.planning/`:**
- Purpose: GSD planning state and codebase maps
- Generated: Yes
- Committed: Yes, by GSD convention (not covered by `.gitignore`)

---

*Structure analysis: 2026-09-28*
