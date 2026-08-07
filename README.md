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
   batch\run.bat
   ```
   This runs the tests without requiring you to manually enter a serial number. If you want to use a custom serial, edit `serial.txt` before running, or use `run.bat` instead.

### On USB stick setup

If you copy the repo to a USB stick:
1. The source code will be there, but **you must rebuild**.
2. Open PowerShell on the destination machine, navigate to the repo root, and run:
   ```powershell
   batch\build-all.bat
   ```
3. Then execute:
   ```powershell
   batch\run.bat
   ```

The `.gitignore` ensures only source code is tracked, so you won't have outdated `.exe` files from different machines.

### Audio + controls + automatic level test (optional)

One build produces all executables; the run variant decides which tests to execute.

If you only need the **audio test**, **functional button test**, and **automatic level test** (skipping only the microphone test), use the `-some` variant:

```powershell
batch\build-all.bat
batch\run-some.bat
```

In the `-some` variant the audio clip is shortened to 7 seconds (starting partway through the song), and the level test runs automatically, measuring dB levels / balance / clipping instead of relying on operator input. Missing tests are auto-filled with `N/A` in the results, and the converter still uploads to the API as normal.

### Audio + controls only (no level test)

If you only need the **audio test** and **functional button test** (skipping the level test and the microphone test), use the `-less` variant:

```powershell
batch\build-all.bat
batch\run-less.bat
```

In the `-less` variant the level test is not run; level fields are auto-filled with `N/A` in the results.

## Included apps

- `apps/FunctionalButtonTest` - BluetoothHeadphoneTest (controls test)
- `apps/MicroTestCloud` - MicroTestCloud (microphone test)
- `apps/pruebasAudifonos/AskForSerial2` - AskForSerial2 (device serial selection)
- `apps/pruebasAudifonos/AudioTest` - AudioTest (audio playback and distortion test)
- `apps/pruebasAudifonos/LevelTest` - LevelTest (recovered from `origin/LevelTest` as `HeadPhoneTest2`)

## Scripts

- `batch/build-all.bat` - Compiles all projects to Release and copies `.exe` files to `bin/` (single build for every run variant)
- `batch/run.bat` - Orchestrates sequential test execution, file cleanup, and result aggregation
- `batch/run-some.bat` / `batch/run-less.bat` - Subset run variants (audio + level, or audio only)
- `scripts/getFinalResults.py` - Parses test output files and creates `final_results.json`

## Pending source

None.

## Root solution

[Sennheiser_tests.sln](Sennheiser_tests.sln) groups all four projects for IDE browsing and coordinated builds in Visual Studio.

## Requirements

- .NET 9 SDK (or higher 8.0 for individual projects)
- Python 3.9+ (for result aggregation)