# Sennheiser_tests

Unified workspace for the Bluetooth headphone test apps recovered from the branch snapshots.

## Quick start

### On your computer (or USB stick)

1. **Install .NET 9 SDK** from https://aka.ms/dotnet/download if you haven't already.
2. **Build all projects** from the repo root:
   ```powershell
   build-all.bat
   ```
   This compiles all 5 test apps to `.exe` files in `bin/`.
3. **Run all tests in sequence**:
   ```powershell
   run-auto.bat
   ```
   This runs the tests without requiring you to manually enter a serial number. If you want to use a custom serial, edit `serial.txt` before running, or use `run.bat` instead.

### On USB stick setup

If you copy the repo to a USB stick:
1. The source code will be there, but **you must rebuild**.
2. Open PowerShell on the destination machine, navigate to the repo root, and run:
   ```powershell
   build-all.bat
   ```
3. Then execute:
   ```powershell
   run-auto.bat
   ```

The `.gitignore` ensures only source code is tracked, so you won't have outdated `.exe` files from different machines.

### Audio + controls + automatic level test (optional)

If you only need the **audio test**, **functional button test**, and **automatic level test** (skipping only the microphone test), use the `-some` variant:

```powershell
build-some.bat
run-some.bat
```

In the `-some` variant the audio clip is shortened to 7 seconds (starting partway through the song), and the level test runs automatically, measuring dB levels / balance / clipping instead of relying on operator input. Missing tests are auto-filled with `N/A` in the results, and the converter still uploads to the API as normal.

## Included apps

- `apps/FunctionalButtonTest` - BluetoothHeadphoneTest (controls test)
- `apps/MicroTestCloud` - MicroTestCloud (microphone test)
- `apps/pruebasAudifonos/AskForSerial2` - AskForSerial2 (device serial selection)
- `apps/pruebasAudifonos/AudioTest` - AudioTest (audio playback and distortion test)
- `apps/pruebasAudifonos/LevelTest` - LevelTest (recovered from `origin/LevelTest` as `HeadPhoneTest2`)

## Scripts

- `build-all.bat` - Compiles all projects to Release and copies `.exe` files to `bin/`
- `run.bat` - Orchestrates sequential test execution, file cleanup, and result aggregation
- `getFinalResults.py` - Parses test output files and creates `final_results.json`

## Pending source

None.

## Root solution

[Sennheiser_tests.sln](Sennheiser_tests.sln) groups all four projects for IDE browsing and coordinated builds in Visual Studio.

## Requirements

- .NET 9 SDK (or higher 8.0 for individual projects)
- Python 3 (for result aggregation)