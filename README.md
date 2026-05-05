# Sennheiser_tests

Unified workspace for the Bluetooth headphone test apps recovered from the branch snapshots.

## Quick start

After installing the .NET 9 SDK, run from the repo root:

```powershell
build-all.bat
run.bat
```

The first compiles all projects to `.exe` files in `bin/`. The second orchestrates running them in order and aggregates results into `final_results.json`.

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

- `apps/pruebasAudifonos/LevelTest` - Recovered from `origin/LevelTest` under the `HeadPhoneTest2` project folder.

## Root solution

[Sennheiser_tests.sln](Sennheiser_tests.sln) groups all four projects for IDE browsing and coordinated builds in Visual Studio.

## Requirements

- .NET 9 SDK (or higher 8.0 for individual projects)
- Python 3 (for result aggregation)