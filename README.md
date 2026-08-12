# Sennheiser_tests

Unified workspace for the Bluetooth headphone test apps recovered from the branch snapshots.

## Quick start

### On your computer (or USB stick)

1. **Install .NET 9 SDK** from https://aka.ms/dotnet/download if you haven't already.
2. **Build all projects** from the repo root:
   ```powershell
   batch\build-all.bat
   ```
   This compiles all 5 test apps to `.exe` files in `bin/`.
3. **Run all tests in sequence**:
   ```powershell
   bin\run.bat
   ```
   This runs the tests without requiring you to manually enter a serial number. If you want to use a custom serial, edit `serial.txt` before running.

### On USB stick setup

If you copy the repo to a USB stick:
1. The source code will be there, but **you must rebuild**.
2. Open PowerShell on the destination machine, navigate to the repo root, and run:
   ```powershell
   batch\build-all.bat
   ```
3. Then execute:
   ```powershell
   bin\run.bat
   ```

The `.gitignore` ensures only source code is tracked, so you won't have outdated `.exe` files from different machines.

### Audio + controls + automatic level test (optional)

One build produces all executables; the run variant decides which tests to execute. Set `VARIANT` before running `bin\run.bat`:

- **`full`** (default) — audio, controls, microphone, and level tests.
- **`some`** — audio, controls, and level (skips only the microphone test).
- **`less`** — audio and controls only (skips level and microphone tests).

```powershell
bin\run.bat                          :: full
set VARIANT=some && bin\run.bat      :: no microphone test
set VARIANT=less && bin\run.bat      :: audio + controls only
```

In the `some`/`less` variants the audio clip is shortened to 7 seconds (starting partway through the song), missing tests are auto-filled with `N/A` in the results, and the converter still uploads to the API as normal.

### Site configuration

Per-unit settings live in `scripts/config.json` (not tracked in git — edit it on each machine after build, it is copied to `bin\scripts\config.json` by `build-all.bat`):

```json
{
  "endpoint": "https://.../api/DataWipeResult?code=...",
  "machine_name": "AudioTester",
  "contract": "10083",
  "test_area": "MEXICALI_R2",
  "program": "HP_MXLR2"
}
```

The `endpoint` can also be supplied via the `AZURE_API_ENDPOINT` environment variable, which takes precedence over `config.json`. If neither is set, the converter skips the upload and prints a warning.

## Included apps

- `apps/FunctionalButtonTest` - BluetoothHeadphoneTest (controls test)
- `apps/MicroTestCloud` - MicroTestCloud (microphone test)
- `apps/pruebasAudifonos/AskForSerial2` - AskForSerial2 (device serial selection)
- `apps/pruebasAudifonos/AudioTest` - AudioTest (audio playback and distortion test)
- `apps/pruebasAudifonos/LevelTest` - LevelTest (recovered from `origin/LevelTest` as `HeadPhoneTest2`)

## Scripts

- `batch/build-all.bat` - Compiles all projects to Release and copies `.exe` files to `bin/` (single build for every run variant)
- `batch/run.bat` - Orchestrates sequential test execution, file cleanup, and result aggregation (variant selected via `VARIANT=full|some|less`)
- `scripts/getFinalResults.py` - Parses test output files and creates `final_results.json`
- `scripts/converter.py` - Converts `final_results.json` to XML and uploads it (stdlib only, no pip dependencies)

## Pending source

None.

## Root solution

Each app ships its own `.sln` (used by the build script); there is no root solution.

## Requirements

- .NET 9 SDK (or higher 8.0 for individual projects)
- Python 3.9+ (for result aggregation and the level-test measurement script)