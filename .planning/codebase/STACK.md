# Technology Stack

**Analysis Date:** 2026-09-28

## Languages

**Primary:**
- C# 12 / .NET 8 & 9 - 7 projects (`apps/*` WinForms apps + `tools/VolumeHelper` console). All app code is `.cs`; the two largest are `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs` and `apps/FunctionalButtonTest/MainForm.cs`.

**Secondary:**
- Python 3.9+ (stdlib only) - 6 scripts in `scripts/`, invoked as subprocesses by the C# runner/apps, never imported into C#.
- PowerShell (Windows PowerShell 5.1) - 3 UI-prompt scripts at the repo root: `show_bluetooth_connect.ps1`, `show_bluetooth_disconnect.ps1`, `show_bluetooth.ps1`. Built with `Add-Type -AssemblyName System.Windows.Forms` / `System.Drawing` to show WinForms dialogs from PowerShell.
- Batch / cmd - `batch/build-all.bat`, `batch/run.bat`.
- MSBuild XML - `*.csproj`, `*.sln` (SDK-style projects).
- ResX - `*.resx` WinForms resource files (two of them carry base64 image payloads; see Repository Size).

## Runtime

**Environment:**
- Windows 10/11 only. Enforced by `net8.0-windows` / `net9.0-windows` + `UseWindowsForms`, Win32 P/Invoke (`BluetoothApis.dll`), `Get-PnpDevice` PowerShell cmdlets, and the `ms-settings:` URI protocol. Nothing builds or runs on macOS/Linux.
- .NET 9 SDK for building (checked at `batch/build-all.bat:11-15`); target frameworks span `net8.0-windows` and `net9.0-windows` so the SDK must be able to resolve both.
- Python 3.9+ on `PATH`, invoked as the literal command name `"python"` (`apps/SennheiserTestRunner/Program.cs:387,399`; `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:408,899`). A `python3`-only PATH will break the pipeline.
- PowerShell must be invocable as `powershell` (`apps/SennheiserTestRunner/Program.cs:237,244,405`).

**Package Manager:**
- NuGet via the `dotnet` CLI (`dotnet publish` implicitly restores). No `NuGet.config`, no `Directory.Build.props`, no `packages.lock.json`.
- Lockfile: **missing**. Package versions are pinned only by the `Version=` attribute in each `.csproj`.
- pip: used exactly once, on demand — `python -m pip install --user requests` (`apps/SennheiserTestRunner/Program.cs:424`), guarded by a `python -c "import requests"` probe. No `requirements.txt` / `pyproject.toml`.

## Frameworks

**Core:**
- .NET SDK-style MSBuild - `<Project Sdk="Microsoft.NET.Sdk">` in every `.csproj`; `net8.0-windows` / `net9.0-windows`.
- Windows Forms (`UseWindowsForms`) - all 6 GUI apps and the orchestrator (`apps/SennheiserTestRunner/SennheiserTestRunner.csproj:6`). `OutputType=WinExe` everywhere except `tools/VolumeHelper/VolumeHelper.csproj` (`Exe`, console).
- Windows Bluetooth stack (Win32 P/Invoke) - `BluetoothApis.dll` (`BluetoothFindFirstRadio`, `BluetoothFindNextRadio`, `BluetoothFindRadioClose`, `BluetoothFindFirstDevice`, `BluetoothFindNextDevice`, `BluetoothFindDeviceClose`) plus `kernel32!CloseHandle`, declared in `apps/FunctionalButtonTest/BluetoothDetector.cs:87-110`.

**Testing:**
- No C# test project exists. The only automated check is `scripts/test_signal_detection.py` — a plain-`assert`, no-framework self-check (`python3 scripts/test_signal_detection.py`), covering signal-presence detection and verdict logic. See `README.md:288`.
- No NUnit/xUnit/MSTest reference in any `.csproj`.

**Build/Dev:**
- `dotnet publish` driven by hand-rolled batch scripts. `batch/build-all.bat:9` sets the publish profile:
  `-r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false`
- `apps/FunctionalButtonTest/Properties/PublishProfiles/FolderProfile.pubxml` — Visual Studio artifact-publishing profile; **not** used by `build-all.bat`.
- 5 separate `.sln` files (`BluetoothHeadphoneTest.sln`, `MicroTestCloud.sln`, `AskForSerial2.sln`, `AudioTest.sln`, `HeadPhoneTest2.sln`). There is no repo-root solution and no `.sln` for `SennheiserTestRunner`.

## Project Matrix

| Project | TFM | Output | NAudio | Notes |
|---|---|---|---|---|
| `apps/SennheiserTestRunner/SennheiserTestRunner.csproj` | `net9.0-windows` | WinExe | — | Orchestrator. `ProjectReference`s all 5 other apps + `VolumeHelper` (`:14-19`); no `.sln`. |
| `apps/FunctionalButtonTest/BluetoothHeadphoneTest.csproj` | `net9.0-windows` | WinExe | 2.2.1 | Only project with `Nullable=disable`, `ImplicitUsings=disable`, `AllowUnsafeBlocks=true`. Embeds `assets/**/*` + a linked miniDSP JPEG (`:19-22`). |
| `apps/MicroTestCloud/MicroTestCloud/MicroTestCloud.csproj` | `net9.0-windows` | WinExe | 2.2.1 | Also references `System.Management` 10.0.5 (`:27`). |
| `apps/pruebasAudifonos/AudioTest/AudioTest/AudioTest.csproj` | `net8.0-windows` | WinExe | 2.3.0 | `Content` copy of `karmaPolice.wav` (`:20-22`). |
| `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/HeadPhoneTest2.csproj` | `net8.0-windows` | WinExe | 2.3.0 | `AssemblyName=LevelTest` (differs from project name). Copies `audio/audioSweep.mp3` + `Resources/miniDSP-headphones.jpg`. |
| `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/AskForSerial2.csproj` | `net8.0-windows` | WinExe | — | Serial dialog. **Zero package references.** |
| `tools/VolumeHelper/VolumeHelper.csproj` | `net9.0-windows` | Exe | 2.3.0 | Console volume setter, 32 lines (`tools/VolumeHelper/Program.cs`). |

## Key Dependencies

**Critical:**
- **NAudio** - the only real NuGet dependency in the repo, and the reason the whole toolchain exists. Used for WASAPI playback/capture and endpoint enumeration:
  - `NAudio.Wave` — `WaveOutEvent`, `WaveInEvent`, `SampleProviders` (`apps/FunctionalButtonTest/AudioPlayer.cs`, `apps/MicroTestCloud/MicroTestCloud/Form1.cs`, `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs`)
  - `NAudio.CoreAudioApi` — `MMDeviceEnumerator`, `MMDevice`, `DataFlow.Render`, `Role.Multimedia` (`tools/VolumeHelper/Program.cs:13-14`, `apps/FunctionalButtonTest/VolumeMonitor.cs:25`, `apps/FunctionalButtonTest/BluetoothDetector.cs:175-178`)
  - **Two versions coexist**: `2.2.1` in `apps/FunctionalButtonTest/BluetoothHeadphoneTest.csproj:15` and `apps/MicroTestCloud/MicroTestCloud/MicroTestCloud.csproj:26`; `2.3.0` in `apps/pruebasAudifonos/AudioTest/AudioTest/AudioTest.csproj:12`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/HeadPhoneTest2.csproj:13`, `tools/VolumeHelper/VolumeHelper.csproj:13`. When published together through `SennheiserTestRunner.csproj`, NuGet unifies to 2.3.0; building the two older projects standalone still resolves 2.2.1.
- **`requests` (pip)** - installed on demand by the runner but **not imported anywhere**: `scripts/converter.py` uses `urllib.request` (`scripts/converter.py:2,164`). The install at `apps/SennheiserTestRunner/Program.cs:408-428` is vestigial and adds a network dependency (or a silent failure) to every LevelTest run for nothing.

**Infrastructure:**
- `System.Management` 10.0.5 - declared in `apps/MicroTestCloud/MicroTestCloud/MicroTestCloud.csproj:27` but there is **no `using System.Management`, `ManagementObject`, or WMI query anywhere in the C# sources**. Unused reference, and it is a .NET 10-era package inside a `net9.0-windows` project.
- Windows PnP device management via PowerShell (`Get-PnpDevice` / `Remove-PnpDevice`), not a NuGet library — `apps/SennheiserTestRunner/Program.cs:405`.

## Configuration

**Environment:**
- Per-machine JSON config: `scripts/config.json` — **gitignored** (`.gitignore`: "may contain API endpoint/keys; edit locally, don't commit"). The committed copy is `{"endpoint": ""}`. `batch/build-all.bat:44-47` copies the whole `scripts/` folder to `bin/scripts/`.
- Config keys: `endpoint`, `golden_left_dbfs`, `golden_right_dbfs`, `golden_tolerance_db` (default 3.0), `balance_max_db` (2.0), `ambient_max_dbfs` (-30.0), `connection_type` ("USB"), `calibration_max_age_hours` (12). Read by `scripts/common.py:7-31` (`load_config`).
- The same file is also read **directly from C#** for one field: `CalibrationMaxAgeHours()` parses `bin/scripts/config.json` then `bin/config.json` with `System.Text.Json` (`apps/SennheiserTestRunner/Program.cs:212-231`).
- Environment variables:
  | Var | Consumer | Location |
  |---|---|---|
  | `AZURE_API_ENDPOINT` | converter.py (overrides `endpoint`) | `scripts/converter.py:14` |
  | `USERNAME` | converter.py, default `tester1` | `scripts/converter.py:19` |
  | `DEVICE_NAME` | runner (set), AudioTest, LevelTest | `Program.cs:281`; `AudioTest/Form1.cs:78`; `HeadPhoneTest2/Form1.cs:103,283` |
  | `STATION_CALIB` | runner (set), LevelTest | `Program.cs:152,160`; `HeadPhoneTest2/Form1.cs:195` |
  | `QUICK_AUDIO` | AudioTest, LevelTest | `AudioTest/Form1.cs:42`; `HeadPhoneTest2/Form1.cs:709` |
  | `CALIBRATION` | LevelTest | `HeadPhoneTest2/Form1.cs:187` |
- Hard-coded tuning constants (no config path):
  - Signal-presence thresholds: `SIGNAL_MIN_DBFS`, `SIGNAL_MAX_CREST_DB`, `SIGNAL_MIN_SNR_DB` — `scripts/db_chart.py:16-19`
  - Station-calibration defaults: `scripts/station_calibration.py:24-28`
  - Backend identity defaults: `Contract "10083"`, `MachineName "AudioTester"`, `TestArea "MEXICALI_R2"`, `Program "HP_MXLR2"` — `scripts/converter.py:21-30`
  - Retry policy: 5 attempts, 2000 ms delay — `apps/SennheiserTestRunner/Program.cs:21-22`

**Build:**
- Build config files: only the 7 `.csproj` files. No `global.json` (SDK version floats with whatever .NET 9 SDK is installed), no `Directory.Build.props`, no `.editorconfig`, no `appsettings.json`, no `App.config`.
- `.gitattributes` forces CRLF for `*.log`, `*.bat`, `*.ps1`.
- `.gitignore` excludes `bin/`, `obj/`, `.vs/`, `.vscode/`, all `*.dll`/`*.exe`/`*.pdb`, `__pycache__/`, `.venv/`, and all run artifacts.

## Platform Requirements

**Development:**
- Windows 10/11 workstation with the .NET 9 SDK installed (must resolve `net8.0-windows` and `net9.0-windows` targets).
- Python 3.9+ reachable as `python` on `PATH`.
- Windows PowerShell.
- Hardware for manual verification: a miniDSP E.A.R.S. coupler on a stereo USB input, a working output device, and a Bluetooth radio (`README.md:338-345`).

**Production:**
- `batch/build-all.bat` publishes **self-contained, single-file `win-x64`** binaries, so no .NET runtime needs to be installed on the line PC: `bin/SennheiserTestRunner.exe` (hosts every test form in-process) and `bin/VolumeHelper.exe` (`batch/build-all.bat:22-37`).
- **Python is still a hard runtime requirement** on the test-bench PC — the runner shells out to `python` for `getFinalResults.py` and `converter.py`, and LevelTest shells out for `db_chart.py` and `station_calibration.py`.
- `batch/run.bat` is the station launcher; it prefers `bin\SennheiserTestRunner.exe` and falls back to a sibling `SennheiserTestRunner.exe`.

## Repository Size

~94 MB working tree, 101 tracked files, dominated by binary media — every byte of it unavoidable at runtime, none of it compressible in git:

| File | Size | Role |
|---|---|---|
| `apps/pruebasAudifonos/AudioTest/AudioTest/karmaPolice.wav` | 15 MB | Listening-test stimulus |
| `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/audio/tone_1khz.wav` | 6.7 MB | Station-calibration tone |
| `apps/FunctionalButtonTest/assets/**` (gifs/jpgs) | 11 MB | Embedded per-model UI assets |
| `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.resx` | 1.5 MB | **base64-encoded images** |
| `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.resx` | 1.3 MB | **base64-encoded images** |
| `audioSweep.mp3`, `PistaAudio/*.mp3` | 1.4 MB each | Playback/record stimuli |

The two `.resx` files hold ~2.8 MB of base64 image payloads as XML text — they inflate diffs, defeat line-based review, and cannot be deduplicated by git's binary detection because they are stored as UTF-8 XML. `FunctionalButtonTest` already does it the right way (`BluetoothHeadphoneTest.csproj:19-22` embeds real asset files rather than base64 blobs); consider moving the LevelTest/AudioTest images into an `assets/` folder with the same `EmbeddedResource Include="assets\**\*.*"` pattern.

---

*Stack analysis: 2026-09-28*
