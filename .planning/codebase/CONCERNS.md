# Codebase Concerns

**Analysis Date:** 2026-09-28

Repo: `Sennheiser_tests` — WinForms factory test-bench (Bluetooth controls, listening, microphone, level/sweep) orchestrated by a C# runner that shells out to Python for DSP and XML upload. All C# targets `net8.0-windows`/`net9.0-windows`; `bin/` is the deployable.

**Reading this document:** every "Priority" is about *production data correctness* first, then operator confusion, then code health. The single most important theme: **the orchestrator's idea of "stage passed" and the aggregation script's idea of "stage result" are computed independently and disagree.** Most High items below are instances of that.

---

## Tech Debt

### Site identity hardcoded in the uploader, not in per-machine config
- Issue: `scripts/converter.py:21-30` hardcodes `Contract: "10083"`, `MachineName: "AudioTester"`, `TestArea: "MEXICALI_R2"`, `Program: "HP_MXLR2"`. `scripts/config.json` exists specifically for per-machine settings (`common.py:7-31`, README "Site configuration") and is *not* consulted for any of these four fields.
- Files: `scripts/converter.py:21-30`, `scripts/config.json`, `scripts/common.py:7-31`
- Impact: a second station (or a second test area) uploads records claiming `TestArea=MEXICALI_R2` and `MachineName=AudioTester` for every unit. Backend production data is silently mis-attributed and unattributable per station. `MachineName` in particular is a hardcoded string when the real value is `Environment.MachineName`.
- Fix approach: move all four into `config.json` via `load_config()`, defaulting to today's literals. `MachineName` should default to `os.environ.get("COMPUTERNAME")`.

### Daily ambient calibration is unreachable from the current orchestrator
- Issue: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:187` gates the ambient-capture flow on `Environment.GetEnvironmentVariable("CALIBRATION") == "1"`. `apps/SennheiserTestRunner/Program.cs` sets only `STATION_CALIB` (line 152) and never `CALIBRATION`. On `main`, `batch/run.bat` sets `CALIBRATION=1`; the new runner does not.
- Files: `apps/SennheiserTestRunner/Program.cs:142-180`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:187-193`, `README.md:87-116`
- Impact: `calibracion.txt` is never refreshed by the current branch. `db_chart.py:189-195` then computes the SNR check against a stale (possibly missing) baseline — when absent, `common.load_baseline` returns `None` and the SNR check silently no-ops. `station_calibration.py:39-41` requires the baseline for `check1_ambient`, so the station gate fails for a reason that is *not* the station. README documents this step as part of the pipeline.
- Fix approach: have `RunDailyStationCalibration` ensure `calibracion.txt` exists and is dated today before running the golden-unit phase (the same date check `batch/run.bat` performed).

### `QUICK_AUDIO`, `MAX_RETRIES`, `RUN_MICROPHONE`, `RUN_LEVEL`, `SKIP_SERIAL_PROMPT` are silently dead
- Issue: the batch orchestrator on `main` honoured these env vars. `apps/SennheiserTestRunner/Program.cs:21-22` hardcodes `MaxRetries => 5` and declares `RetryDelayMs` which is never read. `RunLevelTest` (line 352) has no HD/IE model gate, and `RunMicrophoneTest` (line 329) always runs. `QUICK_AUDIO` is read by `AudioTest/Form1.cs:41-42` and `HeadPhoneTest2/Form1.cs:709` but never set.
- Files: `apps/SennheiserTestRunner/Program.cs:21-22,66-73,305-374`, `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:41-42`
- Impact: on this branch the level test runs for RS models that have no level spec, the microphone test cannot be skipped, and listening runs at full duration. Nobody notices because the level test then just records a FAIL. README ("Included apps") states the microphone is "always run", but the level-test gate is undocumented.
- Fix approach: either re-read these env vars in `Program.cs` or delete the branches that reference them (`batch/run.bat:detect_level_test`, `:set_audio_volume`) so there is one source of truth.

### Two independent writers of `Prueba_*.txt`
- Issue: `apps/FunctionalButtonTest/MainForm.cs:110` writes `Prueba_{device}_{yyyyMMdd_HHmmss}.txt`; `apps/FunctionalButtonTest/SummaryPanel.cs:243` writes `Prueba_{device}_{yyyyMMdd_HHmm}.txt` (minute precision). Both write to `AppDomain.CurrentDomain.BaseDirectory` *and* copy to `Directory.GetCurrentDirectory()`.
- Files: `apps/FunctionalButtonTest/MainForm.cs:110-121`, `apps/FunctionalButtonTest/SummaryPanel.cs:243-254`, `scripts/getFinalResults.py:20,298`
- Impact: a run that reaches the summary panel leaves two report files. `getFinalResults.first_match("Prueba_*")` (`scripts/getFinalResults.py:62-64`) returns `matches[0]` in unsorted OS directory order, so the model string and the six Bluetooth verdicts can be read from the *fallback* report instead of the real one. Minute precision also means a retried save in the same minute silently overwrites the previous.
- Fix approach: one writer. Delete the `MainForm` fallback or rename it to a distinct pattern (e.g. `Prueba_fallback_*`) that `getFinalResults.py` ignores; make the summary filename second-precision.

### Dead and duplicated shell assets
- Issue: `show_bluetooth.ps1` (131 lines) is referenced by nothing on this branch — `apps/SennheiserTestRunner/Program.cs:233-245` calls `show_bluetooth_connect.ps1` / `show_bluetooth_disconnect.ps1`, and `batch/build-all.bat:44-45` only copies those two. The `connect` branch of `show_bluetooth.ps1:10-25` duplicates `show_bluetooth_connect.ps1` step-for-step (same 7 steps, same `FromArgb(0,103,192)`, same 520×520 geometry); the `disconnect` branch duplicates `show_bluetooth_disconnect.ps1` the same way. `show_bluetooth.ps1` is still the script `main`'s `run.bat` calls with `-Mode connect`.
- Files: `show_bluetooth.ps1`, `show_bluetooth_connect.ps1`, `show_bluetooth_disconnect.ps1`, `apps/SennheiserTestRunner/Program.cs:233-245`, `batch/build-all.bat:44-45`
- Impact: two sources of truth for the operator prompt. A fix applied to the dead copy silently does nothing on this branch and does something after the next `main` merge.
- Fix approach: keep `show_bluetooth.ps1` (single script, `-Mode` param), delete the two split copies, and copy the consolidated file in `build-all.bat`.

### Duplicated and unreachable `miniDSP` image
- Issue: `miniDSP.jpg` at the repo root is byte-identical to `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Resources/miniDSP-headphones.jpg` (md5 `17ece5823ef84db0a854b30502192f5e`). `batch/build-all.bat:48` copies the root copy into `bin\`, but nothing reads a loose `miniDSP.jpg` — `apps/FunctionalButtonTest/Deviceassets.cs:19,29` resolves `miniDSP-headphones.jpg` as an *embedded* resource (linked in `BluetoothHeadphoneTest.csproj:21`). `HeadPhoneTest2.csproj:33-36` also copies `Resources\miniDSP-headphones.jpg` to its output, and no LevelTest code references it.
- Files: `miniDSP.jpg`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Resources/miniDSP-headphones.jpg`, `batch/build-all.bat:48`, `apps/FunctionalButtonTest/Deviceassets.cs:19,29`, `apps/FunctionalButtonTest/BluetoothHeadphoneTest.csproj:21`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/HeadPhoneTest2.csproj:33-36`
- Impact: 3 copies of a 100 KB asset; one dead `copy` line in the build that a reader will assume is load-bearing.
- Fix approach: delete the root `miniDSP.jpg` and the `HeadPhoneTest2.csproj` `None Update` + the `build-all.bat` copy line. Keep the single file under `HeadPhoneTest2/Resources/` as the embedded source.

### Three identical `.github/copilot-instructions.md` boilerplates
- Issue: `apps/FunctionalButtonTest/.github/copilot-instructions.md`, `apps/pruebasAudifonos/LevelTest/.github/copilot-instructions.md`, `apps/pruebasAudifonos/AudioTest/.github/copilot-instructions.md` are byte-identical and instruct the agent to use Azure MCP tooling. The only Azure connection in the repo is the env var name `AZURE_API_ENDPOINT` (`scripts/converter.py:14`); the actual endpoint is a winit webservice.
- Files: the three `.github/copilot-instructions.md` files
- Impact: misleads any AI tool that reads them; three copies to keep in sync.
- Fix approach: delete all three or replace with one repo-root file that states the real constraints (Spanish UI strings, `ponytail:` comment convention, env-var contract).

### Unused `System.Management` package reference
- Issue: `apps/MicroTestCloud/MicroTestCloud/MicroTestCloud.csproj:27` references `System.Management` `Version="10.0.5"`. Zero uses in the repo (no `ManagementObject`, no `using System.Management` — Bluetooth enumeration lives in `apps/FunctionalButtonTest/BluetoothDetector.cs` via `BluetoothApis.dll` P/Invoke).
- Files: `apps/MicroTestCloud/MicroTestCloud/MicroTestCloud.csproj:27`
- Impact: a `10.0.x` package is the only .NET 10 artifact in an otherwise .NET 8/9 solution, so it drags an extra restore graph into every build and will warn (`NU1701`) or fail (`NETSDK` asset mismatch) once the .NET 10 targeting pack differs from the installed SDK. Pure build-latency and future-breakage cost for zero functionality.
- Fix approach: delete the `PackageReference`.

### The Python `requests` dependency is installed on every run and never imported
- Issue: `apps/SennheiserTestRunner/Program.cs:408-428` (`EnsurePythonRequests`) probes `python -c "import requests"` and, on failure, runs `python -m pip install --user requests`. No file in `scripts/` imports `requests` — all five scripts are stdlib-only (`urllib.request` in `scripts/converter.py:2`). `README.md:344` documents the install as a requirement.
- Files: `apps/SennheiserTestRunner/Program.cs:354,408-428`, `README.md:344`, `scripts/converter.py:2`
- Impact: on a locked-down bench machine without a user-site-packages write path, `pip install` prints to the console and fails; the whole `try` is `catch { }`, so the failure is invisible and the run continues to depend on a package that is never used. Adds seconds to every cold run.
- Fix approach: delete `EnsurePythonRequests` and its call at line 354, and drop the README line.

### Two `.resx` files carry ~28k lines of dead base64 image
- Issue: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.resx` is 17,734 lines, of which `pictureBox1.BackgroundImage` (`Form1.resx:2868`) runs to EOF. `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.resx` is 14,987 lines, of which `pictureBox1.Image` (`Form1.resx:1621`) runs to EOF. Both are neutralised at runtime: `pictureBox1.Image = null; pictureBox1.Visible = false;` at `HeadPhoneTest2/Form1.cs:181-182` and `AudioTest/Form1.cs:360-361`. `Visible = true` never appears for that control anywhere in the repo.
- Files: the two `.resx` files, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:181-182`, `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:360-361`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.Designer.cs:538-548`, `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.Designer.cs:237-246`
- Impact: 28,232 of 33,323 `.resx` lines (85% of all resx content) are unread. Merge conflicts in these two files are guaranteed to be unreadable, `git blame` is useless on them, and the images stay compiled into the assembly as managed resources. Note that `HeadPhoneTest2/Form1.cs:181` clears only `.Image`; the designer's `BackgroundImage` is never cleared.
- Fix approach: delete the control from both designers and the two `Form1.cs` null/hide lines, then remove the `<data name="pictureBox1.*">` node from each `.resx`.

### Mojibake in source comments and UI strings
- Issue: 14 U+FFFD replacement characters in `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs` and 2 in `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs` — double-encoded UTF-8 that has already been damaged in the working tree. Affected visible strings include the connection-type labels at `AudioTest/Form1.cs:50,52` (`"2. �ptico"`, `"2. Anal�gico 3.5"`) and `"Tipo de conexi�n: "` at `AudioTest/Form1.cs:130`, plus damaged comments at `HeadPhoneTest2/Form1.cs:991,1019`.
- Files: `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:50,52,130,150-157,222,319`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:991,1019`
- Impact: operators see replacement glyphs in the RS connection-cycle prompts. `scripts/getFinalResults.py:39` matches the Bluetooth report field `"Conexión Bluetooth"` by exact substring — any future repair of these strings in the Bluetooth report writer breaks that match silently.
- Fix approach: repair the affected literals from the Spanish source text; add `.editorconfig` with `charset = utf-8` so a second pass cannot re-damage them.

### Blank and unreachable README / project docs
- Issue: `apps/FunctionalButtonTest/README.md`, `apps/MicroTestCloud/README.md`, `apps/pruebasAudifonos/README.md` are all 0 bytes. Five `.sln` files exist (`apps/MicroTestCloud/MicroTestCloud.sln`, `apps/pruebasAudifonos/AskForSerial2/AskForSerial2.sln`, `apps/pruebasAudifonos/AudioTest/AudioTest.sln`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2.sln`, `apps/FunctionalButtonTest/BluetoothHeadphoneTest.sln`) but `batch/build-all.bat` builds only `tools/VolumeHelper` and `apps/SennheiserTestRunner` directly.
- Files: the three empty READMEs, the five `.sln` files, `batch/build-all.bat:26-37`
- Impact: opening a `.sln` gives a different build result from the shipped build. `HeadPhoneTest2.sln` builds the `HeadPhoneTest2` project whose `AssemblyName` is `LevelTest` — the directory and the executable name disagree, which is a standing source of confusion when reading logs and `taskkill` lines.
- Fix approach: delete the five `.sln` files and let `SennheiserTestRunner.sln`-less `build-all.bat` be the only build entry point; or keep them and document that the `.sln` is for IDE-only use.

---

## Known Bugs

### The orchestrator reports PASSED for a cancelled or failed audio test
- Symptoms: `runner_log.txt` contains `[AUDIO] PASSED` for a unit the operator rejected or cancelled. `final_results.json` correctly carries `distorsion: FAIL`, so the XML upload is right but the bench log, the exit code, and the operator's mental model are wrong.
- Files: `apps/SennheiserTestRunner/Program.cs:305-327` (specifically line 314, `Directory.GetFiles(BaseDir, "hearingPass*.txt").Length > 0`), `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:104,136-138,173,188`
- Trigger: `AudioTest/Form1.cs:104` registers a `FormClosing` handler that writes `False` into `hearingPassResults.txt` if it does not already exist, and `btnCancel_Click` (line 137) does the same. The runner's success check is *existence of the file*, not its content. The `main`-branch orchestrator got this right with `findstr /x /c:"True" hearingPassResults.txt` in `batch/run.bat`; the port to C# dropped the content check.
- Workaround: none at runtime. Fix: read the file and require `"True"`, mirroring the old `findstr`:
  ```csharp
  var audioFile = WaitForFile("hearingPassResults.txt", 5);
  if (audioFile != null && File.ReadAllText(audioFile).Trim() == "True") { /* PASSED */ }
  ```

### The same "existence means pass" bug for the microphone and level stages
- Symptoms: `[MICROPHONE] PASSED` for the "no microphone" report; `[LEVELS] PASSED` for a unit with a FAIL sweep.
- Files: `apps/SennheiserTestRunner/Program.cs:314,338-343,363-368`, `apps/MicroTestCloud/MicroTestCloud/Form1.cs:493-541` (`SaveReport2` writes `Resultado: N/A`), `scripts/db_chart.py:288-291` (always writes `results.json` when it runs)
- Trigger: `MicroTestCloud.SaveReport2` is the "REPORTE SIN MICRÓFONO" path — it writes a `MicroTest_*.txt` whose `Resultado` is `N/A`. `RunMicrophoneTest` only checks that a `MicroTest_*.txt` appeared. Likewise `db_chart.py` writes `results.json` unconditionally once the sweep runs, so a FAIL level test is logged as PASSED and the pipeline advances.
- Workaround: none. Fix: parse the verdict out of each report (`MicroTest_*.txt` `Resultado` line, `results.json` measurements) instead of checking file existence.

### `serial.txt` from the previous unit is silently reused
- Symptoms: a unit's test results are uploaded under the *previous* unit's serial number.
- Files: `apps/SennheiserTestRunner/Program.cs:127-136` (`CleanOldFiles`), `apps/SennheiserTestRunner/Program.cs:247-254` (`GetSerial`), `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/Form1.cs:33-45,5`, `scripts/getFinalResults.py:15,278-282`
- Trigger: `CleanOldFiles` (line 129) does **not** include `serial*` in its pattern list. `GetSerial` shows the dialog; if the operator clicks Cancel, `AskForSerial2/Form1.cs:41` never writes, the form closes, and `GetSerial` reads whatever `serial.txt` was left from the last run and returns it as a valid serial. `main`'s `run.bat` deleted `serial*` unless `SKIP_SERIAL_PROMPT` was set; that cleanup was lost in the port.
- Workaround: delete `bin\serial.txt` by hand between units. Fix: add `"serial*"` to the `CleanOldFiles` pattern list and make `GetSerial` fail closed when the dialog returns without a fresh file.

### `getFinalResults.py` can pick the microphone `.wav` instead of the `.txt`
- Symptoms: `resultado_mic` is absent from `final_results.json`, so the `<subtest>` `resultado_mic` is omitted from the uploaded XML and the unit ships with a missing test — no FAIL, no error.
- Files: `scripts/getFinalResults.py:21,62-64,337-344`, `apps/MicroTestCloud/MicroTestCloud/Form1.cs:1457-1461`
- Trigger: `MicroTestCloud.SaveReport` writes **two** files with the same timestamp: `MicroTest_YYYYMMDD_HHMMSS.txt` and `MicroTest_YYYYMMDD_HHMMSS.wav`. The pattern `FILE_PATTERN_MICROPHONE = "MicroTest_*"` matches both, and `first_match` returns `glob.glob(pattern)[0]` in unsorted OS directory order. When the `.wav` wins, `read_text_file` decodes binary as `utf-8` → `cp1252` → `latin-1` → `errors="ignore"` and the `Resultado` loop never matches, so the key is simply never assigned. `apps/SennheiserTestRunner/Program.cs:338` correctly uses `MicroTest_*.txt`; only the Python side is wrong.
- Workaround: none. Fix: `FILE_PATTERN_MICROPHONE = "MicroTest_*.txt"` and sort `first_match`'s result so behaviour is deterministic.

### `getFinalResults.py` crashes on a truncated `results.json` or a short `Resultado` line
- Symptoms: Python traceback, no `final_results.json`, and the upload silently never happens.
- Files: `scripts/getFinalResults.py:305-308,339-342`
- Trigger: line 307 `json.loads(results_text)` is unguarded — a `results.json` half-written by a killed `db_chart.py` raises `JSONDecodeError`. Line 342 indexes `parts[2]` with no length check, unlike the sibling parser `parse_bluetooth_results` which guards every index (lines 128,131,134,137,140,143). A `Resultado` line with fewer than three whitespace-separated tokens raises `IndexError`.
- Downstream effect: `converter.py:47-58` then raises `FileNotFoundError` looking for `final_results.json`, `RunResultsScripts` ignores the exit code, and `Main` still reaches `Log($"Tests completed")` — a run with no upload reports success.
- Fix approach: wrap the `results.json` read in the same best-effort `try` already used for timestamps (line 390), and add `len(parts) > 2` guards to the `Resultado` parser.

### Failed uploads are invisible and exit 0
- Symptoms: the bench reports "Tests completed" while nothing reached the backend.
- Files: `apps/SennheiserTestRunner/Program.cs:376-401,476-498`, `scripts/converter.py:162-169,188-198`
- Trigger: `RunResultsScripts` (line 376) discards the exit code of both `getFinalResults` and `converter`. `converter.py:run()` prints `Upload status: FAILED` but never sets a non-zero exit status. `RunProcess` returns the code but no caller inspects it, and `Main` falls through to `Log($"Tests completed. Total time: {minutes} min")` and returns 0.
- Compounding: the `.exe` fallbacks at lines 378 and 390 (`getFinalResults.exe`, `converter.exe`) are never produced by `batch/build-all.bat`, so this branch always silently falls through to `python`.
- Fix approach: make `converter.py` `sys.exit(1)` on upload failure, have `RunResultsScripts` check both return codes, and exit non-zero from `Main` when aggregation or upload fails.

### A Bluetooth device name with `:` produces no report at all
- Symptoms: the controls stage times out 5 times with `[CONTROLS] FAILED - attempt N/5` and no visible cause; a 100 KB WAV per attempt accumulates uncollected.
- Files: `apps/FunctionalButtonTest/SummaryPanel.cs:236-257` (line 243, empty `catch { }` at 256), `apps/FunctionalButtonTest/MainForm.cs:103-108`
- Trigger: `SummaryPanel.AutoSaveTxtReport` interpolates `session.SelectedDevice?.Name` straight into a filename with no sanitisation, then wraps the whole thing in `catch { }`. Windows Bluetooth names routinely contain `:` (e.g. `MOMENTUM 4 Wireless: Audio`), which is an invalid filename character on NTFS, so `WriteAllText` throws and the report is discarded silently. `MainForm.WriteFallbackReportIfMissing` (line 105) *does* sanitise via `Path.GetInvalidFileNameChars()` — the two writers disagree, and only the one without sanitisation matters here because it runs first and sets nothing.
- Workaround: none. Fix: share one filename-sanitising helper between the two writers and log the exception instead of swallowing it.

### `EvaluateResults` shows a dialog, then crashes on the same condition
- Symptoms: a message box reading "No existe el archivo de resultados.json" is immediately followed by an unhandled `NullReferenceException` / `FileNotFoundException` that kills the form.
- Files: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:941-968` (line 945 and line 956 show the dialog; line 948 and line 959 dereference anyway)
- Trigger: the `if (!File.Exists(jsonFile))` branch at line 943 has no `return`, so line 948 `File.ReadAllText(jsonFile)` throws. Likewise, when `left`/`right` are `null` the dialog at 956 is followed by `left.dbfs` at 959. The typo "Error de preuba" is at lines 945 and 956.
- Fix approach: `return` after each `MessageBox.Show`.

### The Bluetooth prompt's outcome is discarded
- Symptoms: an operator who clicks the window's X instead of "Listo, Continuar" proceeds into the test with no pairing.
- Files: `show_bluetooth_connect.ps1:88-90`, `show_bluetooth_disconnect.ps1`, `apps/SennheiserTestRunner/Program.cs:233-245`
- Trigger: the scripts end with `$form.ShowDialog() | Out-Null`, discarding the `DialogResult`. `ShowBluetoothConnectPrompt` does not gate on it and does not even return a value. `-ExecutionPolicy Bypass` is used, but there is no equivalent of the `Unblock-File` that `main`'s `run.bat` performs before calling `show_bluetooth.ps1` — a GPO-blocked or Mark-of-the-Web-marked copy of the script on a fresh clone fails silently and the operator is never prompted.
- Fix approach: return the `DialogResult` from `RunProcess`, surface `DialogResult.Cancel` as a hard stop, and add the `Unblock-File` call to `batch/build-all.bat` or the runner.

### `RunStationCalibrationScript` can deadlock on stderr
- Symptoms: the station-calibration dialog freezes indefinitely after the golden-unit tone; the runner's own `WaitForExit` never returns.
- Files: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:400-424` (no `RedirectStandardError`), `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:887-919` (has `RedirectStandardError`)
- Trigger: `RunStationCalibrationScript` sets `RedirectStandardOutput = true` only, drains stdout in a loop, then calls `WaitForExit()`. If `station_calibration.py` writes more than the pipe buffer (~4 KB) to stderr — an unhandled exception with a long traceback does exactly this — the child blocks writing and the parent blocks in `WaitForExit`. Commit `346275f` fixed precisely this for `RunPythonScript` (line 902) but the same fix was not applied to this sibling, which is the one the station gate depends on.
- Fix approach: mirror line 902 here — set `RedirectStandardError = true` and drain both streams, or use `BeginOutputReadLine`/`BeginErrorReadLine`.

### Wrong device name read back from a stale report
- Symptoms: `[CONTROLS] PASSED` on retry 2 with the previous unit's model, and the wrong `DEVICE_NAME` in the audio and level stages.
- Files: `apps/SennheiserTestRunner/Program.cs:288,463-474`, `apps/FunctionalButtonTest/MainForm.cs:84`
- Trigger: `CleanOldFiles` runs once, before the stage. A `Prueba_*.txt` left by a *failed* attempt is not deleted before the next attempt, so `WaitForFile` returns it on the first poll and the retry short-circuits to PASSED. `MainForm.WriteFallbackReportIfMissing` makes this worse: it returns early at line 84 whenever *any* `Prueba_*.txt` exists, so a stale file also suppresses the diagnostic fallback report.
- Fix approach: delete the stage's result files at the top of each retry iteration, not once at startup.

### The C# and Python knob verdicts can disagree on the same JSON
- Symptoms: the operator screen says the balance knob PASSed while the uploaded XML says FAIL (or the reverse).
- Files: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:1220-1286` (line 1284 uses `Math.Abs(left - right)`), `scripts/getFinalResults.py:183-225` (line 203 uses the signed `active_dbfs - muted_dbfs`)
- Trigger: the same `KNOB_SEPARATION_DB = 15.0` is duplicated in `HeadPhoneTest2/Form1.cs:90` and `scripts/getFinalResults.py:53`, and the two implementations measure separation differently. When the E.A.R.S. "active" channel is actually the quieter one, C# takes the absolute value (PASS) while Python takes the signed difference (FAIL). `scripts/test_signal_detection.py:159-174` covers only the Python implementation, so the divergence is invisible to the test suite.
- Fix approach: have the C# side read `channel_active` plus the two `dbfs` values and apply the same signed formula, or — better — delete the C# verdict and have `Form1` call a shared helper. Do not keep the rule in two languages.

---

## Security Considerations

### `scripts/config.json` is committed, and the `.gitignore` entry that should protect it is inert
- Risk: the API endpoint is a credential-bearing URL. `README.md:296` documents the shape as `https://.../api/DataWipeResult?code=...` — the `code` query parameter *is* the authorisation, and `scripts/converter.py:162-169` sends no other auth header. The one file meant to hold it is tracked.
- Files: `scripts/config.json` (tracked), `.gitignore:37-38`, `README.md:290-296`, `scripts/converter.py:14,162-169`
- Current state: the committed value is `{"endpoint": ""}` — empty, and both commits that touched the file (`42477f8 scripts/config.json added, remove from .gitignore`, `6317c0a`) recorded an empty endpoint, so **no secret is in the history today**. The exposure is prospective, not actual.
- Current mitigation (and why it is not enough): `.gitignore:37-38` says "Site config (may contain API endpoint/keys); edit locally, don't commit" and lists `scripts/config.json`. `.gitignore` only affects *untracked* files, so the rule does nothing. `42477f8` explicitly removed the file from `.gitignore` and committed it, and `README.md:290` still tells operators the file is "gitignored". An operator who follows the README, drops a real endpoint in, and runs `git add -A` stages the credential with no warning.
- Recommendations:
  1. `git rm --cached scripts/config.json` and ship `scripts/config.json.example` with the documented keys and empty values.
  2. Delete `.gitignore:37-38` so the file and the docs stop disagreeing.
  3. Change the sample to `https://host/api/DataWipeResult` and carry the credential in an `Authorization` header read from an env var, not in the URL — a URL secret leaks into proxy logs, browser history, and Referer.
  4. Add a `pre-commit` check that rejects any non-empty `endpoint` in a staged `config.json`.

### The upload is unauthenticated and unencrypted-by-policy
- Risk: `scripts/converter.py:162-169` POSTs the full `DataWipeResultV2` document — serial number, model, operator username, timestamps, MAC field — with only a `Content-Type` header. There is no `Authorization` header, no `accesstoken` value (line 142 writes an empty element), and no scheme check on the endpoint. A `http://` endpoint configured by a well-meaning technician sends every unit's serial and operator identity in cleartext.
- Files: `scripts/converter.py:142,162-169`, `scripts/config.json`
- Current mitigation: `api_endpoint` validation is absent; the only guard is the 15 s timeout.
- Recommendations: reject any endpoint that is not `https://` before the first upload; send the credential as a header from `AZURE_API_TOKEN`; drop `USERNAME` from the payload or source it from a signed-in identity rather than the OS environment variable (`scripts/converter.py:18-19` falls back to the literal `"tester1"`, which silently attributes every unit to one operator when the env var is unset).

### The Bluetooth cleanup removes PnP devices by fuzzy name match
- Risk: `apps/SennheiserTestRunner/Program.cs:403-406` runs, on the production bench, un-elevated-by-default PowerShell that enumerates every `Get-PnpDevice -Class Bluetooth` and pipes it to `Remove-PnpDevice -Force`, excluding names matching `Radio|Adapter|Enumerator|LE Enumerator|Microsoft|Intel|Qualcomm|Broadcom`. The exclusion is a substring blacklist, not a scope. Any paired BT peripheral whose friendly name lacks one of those tokens — a test phone, a barcode scanner, a keyboard — is force-removed from the machine, and the operator is not told it happened.
- Files: `apps/SennheiserTestRunner/Program.cs:403-406`, `batch/run.bat` (same command on `main`)
- Current mitigation: `$ErrorActionPreference = 'SilentlyContinue'` hides failures; there is no confirmation prompt, no dry-run, and no log of what was removed.
- Recommendations: restrict removal to device instance IDs observed as paired during this run, log every removed device to `runner_log.txt`, and require an explicit operator confirmation.

### The report files contain operator and unit identity in a shared, world-readable directory
- Risk: `Prueba_*.txt` embeds the device name and per-test operator verdicts; `MicroTest_*.txt` embeds `_deviceName`; `runner_log.txt` (`apps/SennheiserTestRunner/Program.cs:20`) embeds the working directory, machine paths, and every stage outcome; `final_results_converted.xml` embeds serial, model, and username. `MainForm.cs:120` and `SummaryPanel.cs:253` additionally *copy* each report to `Directory.GetCurrentDirectory()`. `CleanOldFiles` never removes `.wav` recordings other than by the `MicroTest_*`/`recorded*` patterns, so microphone audio accumulates in `bin\` after failed runs.
- Files: `apps/SennheiserTestRunner/Program.cs:20,34,127-136`, `apps/FunctionalButtonTest/MainForm.cs:116-121`, `apps/FunctionalButtonTest/SummaryPanel.cs:249-254`, `apps/MicroTestCloud/MicroTestCloud/Form1.cs:1457-1464`
- Current mitigation: `.gitignore:15-24` keeps them out of git. Nothing restricts filesystem access; on a shared bench the next operator can read the previous unit's serial and recordings.
- Recommendations: write run artefacts under a per-run subdirectory keyed by serial; delete the directory at the end of a successful upload; add the `recorded_knob_*`/`ear_microphone_capture*` names to `CleanOldFiles`.

---

## Performance Bottlenecks

### `db_chart.py` materialises the entire recording in Python floats
- Problem: `pcm_to_floats` (`scripts/db_chart.py:39-65`) returns a `List[float]` for the whole file, and `read_stereo_wav` (line 93-108) then slices it twice into left/right lists (line 87-88). `main` additionally builds `combined_samples = [l + r for l, r in zip(...)]` (line 260), a third full-length list.
- Files: `scripts/db_chart.py:39-65,87-88,93-108,255-262`
- Cause: a 40 s stereo 44.1 kHz capture is 3.5 M frames = 7.1 M samples. CPython floats in a list cost ~32 B each (8 B pointer + 24 B object), so the peak working set is roughly 220 MB for the samples, another ~110 MB for `combined_samples`, plus the ~14 MB raw `bytes` — call it 350-400 MB RSS, transient, three times per RS195 unit (sweep + two knob takes) and once per station-calibration take.
- Improvement path: `wave` already gives frame counts — accumulate `sum(x*x)`, `max(abs(x))` and a sample counter in a single pass over `readframes` blocks (e.g. 64 k frames) and never build a list. `calc_rms` and `measure_from_samples` only need sum-of-squares, peak, and length. This is a strict simplification, not a rewrite: it deletes `pcm_to_floats`' output allocation, the two slices, and the `combined_samples` list.

### A 25 Hz UI timer drives the microphone analysis
- Problem: `apps/MicroTestCloud/MicroTestCloud/Form1.cs:488` creates `timerUI = new System.Windows.Forms.Timer { Interval = 40 }` — 25 wakeups per second on the UI thread for the duration of the test, inside a 1,551-line form that also owns a `WaveInEvent`, a `WaveOutEvent`, a recorded stream, and the report writers.
- Files: `apps/MicroTestCloud/MicroTestCloud/Form1.cs:488-489,1096-1105`
- Cause: no batching between the audio callback and the UI refresh; every tick marshals level data across threads (`Invoke` calls at `apps/FunctionalButtonTest/TestPanels.cs:183,469,484,490,766,835,983` show the pattern repeated seven times in the sibling app).
- Improvement path: 40 ms is below the perceptual threshold for a level meter. Raise to 100 ms and move the sample accumulation off the UI thread; on a bench machine also running a WinForms sweep app, a 25 Hz marshalled timer is pure scheduler noise.

### Repo weight is dominated by committed audio
- Problem: `git count-objects -vH` reports a 52.4 MiB pack. The largest tracked files are `apps/pruebasAudifonos/AudioTest/AudioTest/karmaPolice.wav` (15.5 MB), `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/audio/tone_1khz.wav` (6.9 MB), and `apps/FunctionalButtonTest/assets/ACCENTUM/playpause.gif` (1.7 MB) — ~26 MB of the pack is stimuli and UI GIFs, plus the ~2.8 MB of dead `.resx` base64.
- Files: `apps/pruebasAudifonos/AudioTest/AudioTest/karmaPolice.wav`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/audio/tone_1khz.wav`, `apps/FunctionalButtonTest/assets/`, the two `.resx` files
- Cause: 16-bit WAV instead of compressed audio for `tone_1khz.wav` (a 1 kHz sine needs no 16-bit PCM at 44.1 kHz), and per-model GIF assets that could be one shared sprite sheet.
- Improvement path: `tone_1khz.wav` is a pure tone — generate it at run time or ship it as a few KB. The GIFs are 3 near-duplicate sets across `ACCENTUM`, `Momentum 4`, and `Momentum TW 4`; one set keyed by action, not by model, would cut ~5 MB. Removing the dead `.resx` blobs helps the working tree, not the pack (already in history) — do not rewrite history for this alone.

---

## Fragile Areas

### Subprocess plumbing: `python` by name, four-candidate script probing, flat-file CWD protocol
- Files: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:921-939` (`ResolvePythonScriptPath`, 4 candidates), `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:206-248` (`ResolveConfigPath`, 4 candidates), `apps/SennheiserTestRunner/Program.cs:32` (`Environment.CurrentDirectory = BaseDir`), `apps/SennheiserTestRunner/Program.cs:408-428` (`EnsurePythonRequests`), `scripts/getFinalResults.py:14-25,395`
- Why fragile: every stage communicates through flat files in the process working directory (`results.json`, `knob_left.json`, `station_calibration.json`, `audio_plays.json`, `serial.txt`, `tiempo1.txt`, `hearingPassResults.txt`, `Prueba_*.txt`, `MicroTest_*.txt`) with no locking, no run id, and no atomic rename. Two concurrent runs on one machine interleave and produce a `final_results.json` that mixes two units. If `python` is not first on `PATH` (a pyenv shim, the Microsoft Store alias, a venv not active in the runner's environment), `Process.Start` throws `Win32Exception` and the stage fails with a message that does not mention Python. `ResolvePythonScriptPath` returns `Path.GetFullPath(scriptName)` as its fallback — a relative path resolved against the CWD, which is *not* the scripts directory — so a missing script produces `python "<cwd>\db_chart.py"` and a `FileNotFoundError` from the interpreter.
- Safe modification: keep the four-candidate probe (it is what makes the dev-tree and `bin\` layouts both work) but resolve `python` once at startup into a field, fail loudly with the interpreter path in the message, and stage each unit's files into `base/serial-<serial>/` written with `File.Move` from a temp name so `getFinalResults.py` never reads a half-written file.
- Test coverage: none. `scripts/test_signal_detection.py` covers the *pure* verdict functions of all four Python modules, but nothing covers the file protocol or the C# side of it.

### Station calibration computes its verdict twice, and only one copy gates
- Files: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:426-494` (`ShowStationGoldenVerdict`), `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:442-446` (`chk2`/`chk3`/`chk4`), `scripts/station_calibration.py:30-119`, `apps/SennheiserTestRunner/Program.cs:182-210`
- Why fragile: `ShowStationGoldenVerdict` computes `chk2`/`chk3`/`chk4` in C# (lines 442-446) purely to *display* them in `lblPlay.Text` (lines 447-451), then sets `pass` from the Python script's verdict alone (line 439). The operator can therefore read `PASO 2/4 IZQ: FAIL | PASO 3/4 DER: FAIL | PASO 4/4 BALANCE: PASS` and be told `ESTACIÓN LIBERADA` one line later. `check1Pass` (line 44) is also initialised to `true` and only overwritten inside the `FinishCalibration` `Invoke` (line 597), so a flow that reaches the verdict without completing step 1 displays `PASO 1/4 AMBIENTE: PASS` unconditionally.
- Safe modification: delete the C# `chk2`/`chk3`/`chk4` and render the `checks` object that `station_calibration.py` already emits (`station_calibration.json` → `checks` → per-check `pass`/`detail`). One implementation, one display, one gate.
- Test coverage: `scripts/test_signal_detection.py:226-309` covers the Python side thoroughly, including `test_converter_station_calib_flips`. The C# display path is uncovered.

### The production level verdict and the calibration verdict use different hardcoded thresholds
- Files: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:816,824-825` (production: `level_diff <= 2`, `-30 <= level <= -10`), `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:219-223` (calibration: thresholds read from `config.json`), `scripts/getFinalResults.py:28-31` (aggregation: `2`, `-30`, `-10`, `0`), `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:82` vs `scripts/getFinalResults.py:50` (`NoVolumeModels` / `MODELS_WITHOUT_VOLUME` duplicated)
- Why fragile: the balance/volume/clipping limits appear as literals in three places and as config keys in a fourth. Change `VOLUME_MIN` in `getFinalResults.py` and the operator's on-screen ✔/✘ in `Form1.cs:824-825` will disagree with the XML. The no-volume model list is duplicated C#/Python, including the `norm.StartsWith("ie")` special case, and the two lists already differ in nothing today — which is exactly the point.
- Safe modification: single-sourced constants in `config.json` with today's literals as defaults, read by all three call sites. Add a test in `scripts/test_signal_detection.py` that asserts the C# literal set and the Python constant set are equal (a source-text assertion is enough and cheap).
- Test coverage: `scripts/test_signal_detection.py:199-213` covers `analyze_audio_levels` for one input. No test pins the constants.

### `converter.py`'s `SUBTESTS` order is a load-bearing contract with the backend
- Files: `scripts/converter.py:34-40` (the list), `scripts/converter.py:117-121` (`enumerate(SUBTESTS, start=1)` → `TestIDNumber`), `scripts/converter.py:22-23` (`dbType`/`servicename` are written empty)
- Why fragile: the comment at line 33 says "Appended entries keep existing `TestIDNumber` values stable for the backend", and the `continue` at line 118 is what makes that true — a subtest absent from `final_results.json` leaves a gap in the sequence rather than shifting everything after it. Insert a name anywhere but the end and every subsequent `TestIDNumber` silently changes meaning in the backend. The list is the *only* place this contract lives; there is no test asserting ID stability, though `scripts/test_signal_detection.py:194-197,403-424` already reads the emitted XML and could.
- Safe modification: append-only, enforced by a test that snapshots the current `name -> index` mapping and fails on any change to an existing entry.
- Test coverage: partial — `test_converter_new_subtests` and `test_converter_schema` exist but do not pin the numeric IDs.

### `AudioTest` re-records and re-tests the unit once per physical connection
- Files: `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:41-105` (model detection and connection arrays), `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:202-250` (the cycle), `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:67-69` (`RecordedAudioPath`)
- Why fragile: `isRSModel` and the connection count are inferred from the `DEVICE_NAME` env var by substring match on a normalised name (`norm.Contains("rs255")` etc., lines 81-96). The env var is set by `SennheiserTestRunner/Program.cs:278-282` from either the wired jack-model combo or the BT device name. If the operator picks "RS 195" on a machine where the BT name also matched, the arrays chosen are whichever branch runs first (`FullCycleModels` is checked before `NoUsbCycleModels`). `ConnectionTypes[connectionIndex]` is indexed unguarded at line 130 and `ConnectionFileSuffixes[connectionIndex]` at line 68 — both are `Array.Empty<string>()` for non-RS models, so any path that reaches them for a non-RS model throws `IndexOutOfRangeException` inside a `FormClosing` handler.
- Safe modification: the model→cycle mapping belongs in `DeviceProfileRegistry` (which already carries per-model capability flags at `apps/FunctionalButtonTest/Deviceprofileregistry.cs:18-88`) rather than in duplicated string arrays in a sibling app. Guard the `ConnectionTypes.Length == 0` case explicitly.
- Test coverage: none on the C# side.

### Global media hotkeys are registered with unchecked return values
- Files: `apps/FunctionalButtonTest/AppCommandRouter.cs:28-47`, `apps/FunctionalButtonTest/MainForm.cs:66-77`
- Why fragile: `Register` calls `RegisterHotKey` five times and discards every return value. If any of these VKs is already claimed — another app on the bench, a previous instance that did not reach `Unregister` because the form was killed, or a second bench process left over — the registration fails silently and the AVRCP/hotkey channel of the Bluetooth controls test stops receiving events. The failure surfaces as "the buttons don't work" in `Prueba_*.txt` with nothing in any log. `Register` runs in `OnHandleCreated`, so a handle recreation double-registers. `ProcessMessage` also returns `true` for handled messages, but `MainForm.WndProc:31-35` ignores the return value, so `WM_APPCOMMAND` is never marked handled and Windows still applies its default media behaviour.
- Safe modification: check the return codes and log a warning naming the failed VK; call `Unregister` in `Dispose(bool)` rather than `OnFormClosed` so a handle recreation is symmetric; return the handled flag through to `base.WndProc` via `m.Result`.
- Test coverage: none.

### `BluetoothDetector` swallows every enumeration failure
- Files: `apps/FunctionalButtonTest/BluetoothDetector.cs:130-158` (`catch { }` at 153), `apps/FunctionalButtonTest/BluetoothDetector.cs:230-247` (`catch { }` at 246), `apps/FunctionalButtonTest/BluetoothDetector.cs:296-303` (`catch { }` at 302)
- Why fragile: all three P/Invoke blocks discard their exceptions. `GetPairedDevices` returning an empty list is indistinguishable from "the operator has no device paired", so `DeviceSelectForm` shows an empty dialog and the operator's only evidence is a blank window. On the line this reads as a hardware fault and burns time. The radio-handle loop at lines 145-149 also calls `CloseHandle(hRadio)` inside the `do`/`while` that `BluetoothFindNextRadio` drives, which is the classic shape for using a handle the enumeration has not finished with.
- Safe modification: log the `Marshal.GetLastWin32Error()` value for each P/Invoke failure to `runner_log.txt`, and surface a distinct "enumeration failed" state in `DeviceSelectForm` rather than an empty list.
- Test coverage: none. This code is Windows- and hardware-specific so it cannot be unit tested off the bench, which is exactly why the logging matters.

### Per-instance audio resources are never disposed
- Files: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:62-63` (`new WaveOutEvent()` / `new WaveInEvent()` as field initialisers), `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:99` (`knobTimer`), `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:669-675,779-787` (`stopRecording`/`stopAudio`)
- Why fragile: `Form1` has no `Dispose(bool)` override. `WaveOutEvent` and `WaveInEvent` are `IDisposable` and are *not* `Control`s, so `using var form = new HeadPhoneTest2.Form1()` in `apps/SennheiserTestRunner/Program.cs:155,360` disposes the form but not the audio handles. The runner creates up to 5 `Form1` instances per level attempt plus one per station-calibration gate, each allocating a fresh pair. `stopAudio` disposes `audioFile` but never `outputDevice`; `playAudio` (line 681-684) works around this by disposing and reallocating on every call. `knobTimer` is likewise never disposed.
- Safe modification: add `protected override void Dispose(bool disposing)` disposing `outputDevice`, `waveIn`, `writer`, `timer`, and `knobTimer`; stop creating a new `WaveOutEvent` inside `playAudio`.
- Test coverage: not unit-testable; detectable only by watching handle counts across retries.

---

## Scaling Limits

### The pipeline is single-station and single-operator by construction
- Current capacity: exactly one unit at a time per machine, on one bench.
- Limit: two operators on one machine, or two machines sharing one `bin\`, will corrupt each other's runs. Every inter-stage channel is a flat filename in a shared directory (`scripts/getFinalResults.py:14-25`, `apps/SennheiserTestRunner/Program.cs:32`). `SennheiserTestRunner` even calls `KillOldProcesses` (`apps/SennheiserTestRunner/Program.cs:119-125`) to kill the other bench apps by name — so starting a second run kills the first run's UI windows mid-test.
- Scaling path: a per-run working directory keyed by serial, and replacing `KillOldProcesses` with a mutex/lock file so a second launch is refused rather than destructive. The flat-file protocol can stay; it just needs a directory.

### Unit throughput is bounded by fixed-duration waits, not by work
- Current capacity: `HeadPhoneTest2` spends 40 s on the sweep (`Form1.cs:338`), 40 s on the golden tone (`Form1.cs:392`), 10 s + 7 s on the two knob takes (`Form1.cs:91-92`), and 30 s on each ambient capture (`Form1.cs:30`). `AudioTest` spends 7-15 s per connection and cycles up to 3 times for RS255/RS275 (`Form1.cs:50-53`). `SennheiserTestRunner` gates the whole bench on a station calibration that runs whenever `station_calibration.json` is missing, `FAIL`, or older than 12 h (`Program.cs:204`).
- Limit: 5 retries × 5 stages, each retry being a full operator-present form, means a hard stage failure costs minutes of bench time and produces five `recorded*.wav` files nobody deletes.
- Scaling path: the retry loop is a blunt instrument — the stages that genuinely need a retry (bluetooth pairing) are interleaved with the ones that do not (level sweep, which is deterministic given the audio path). Separate the retryable stages from the deterministic ones and the bench recovers its time without touching correctness.

---

## Dependencies at Risk

### `System.Management` 10.0.5 referenced from a `net9.0-windows` project
- Risk: `apps/MicroTestCloud/MicroTestCloud/MicroTestCloud.csproj:27` pulls a .NET 10 package into a .NET 9 build while being entirely unused. It is the newest dependency in the repo and the only one past the solution's TFM.
- Impact: restore-time asset-compatibility warning or hard failure once the installed SDK's targeting pack no longer resolves the package's asset group; plus a slower restore for every developer. Zero functional impact today because nothing uses it.
- Migration plan: delete the reference. If WMI is ever needed for audio-device enumeration, use `NAudio`'s `MMDeviceEnumerator`, which the rest of the codebase already uses.

### Mixed target frameworks across one build
- Risk: `net8.0-windows` (`HeadPhoneTest2`, `AudioTest`, `AskForSerial2`) and `net9.0-windows` (`MicroTestCloud`, `BluetoothHeadphoneTest`, `SennheiserTestRunner`, `VolumeHelper`) in a single `build-all.bat` invocation, where the .NET 9 runner ProjectReferences the .NET 8 projects.
- Impact: both the .NET 8 *and* .NET 9 SDKs (plus Windows Desktop targeting packs) must be installed on every bench, though `build-all.bat:11-13` only tells the operator to install the .NET 9 SDK. The three `net8.0-windows` projects also require an 8.0 runtime unless the publish is self-contained — and only the runner's publish is (`build-all.bat:7`), so a bench with only the .NET 9 desktop runtime fails at `Form` load with a `FileNotFoundException` on `System.Windows.Forms.dll`.
- Migration plan: move the three `net8.0-windows` projects to `net9.0-windows`; the APIs they use (`ColorTranslator`, `Environment.ProcessPath`, `System.Text.Json`) are all present in 9.0.

### NAudio pinned to two different versions
- Risk: `NAudio` `2.3.0` in `HeadPhoneTest2.csproj:15`, `AudioTest.csproj:15`, `tools/VolumeHelper/VolumeHelper.csproj:13`; `2.2.1` in `MicroTestCloud.csproj:26` and `BluetoothHeadphoneTest.csproj:17`.
- Impact: because `SennheiserTestRunner` ProjectReferences all of them, NuGet unifies to `2.3.0` at build time and the two `2.2.1` declarations are misleading. A future `2.4.0` release that changes an audio API will break the two projects that were silently compiling against `2.3.0` while claiming `2.2.1`, and the error will point at the wrong csproj.
- Migration plan: align all five on one version and let a single `Directory.Packages.props` own it.

### Global media-key P/Invoke against `user32.dll` and `BluetoothApis.dll`
- Risk: `apps/FunctionalButtonTest/AppCommandRouter.cs:25-26` and `apps/FunctionalButtonTest/BluetoothDetector.cs:87-109` bind to undocumented-or-version-sensitive Win32 exports. `BluetoothApis.dll`'s `BluetoothFindFirstRadio`/`BluetoothFindDevice` structs are hand-declared with `[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]` (lines 44, 75) — if that size is ever wrong the marshaller silently truncates or overruns.
- Impact: a Windows feature update that changes the struct layout produces a device list that is subtly wrong rather than an error, and the Bluetooth controls test reports per-button failures that look like defective units.
- Migration plan: validate `Marshal.SizeOf` against the documented value at startup and log a hard warning on mismatch; the `dwSize` fields are already populated from `Marshal.SizeOf`, so the only missing guard is an expected-value assertion.

---

## Missing Critical Features

### Aggregate results are never verified after the upload
- Problem: `scripts/converter.py:162-198` prints `Upload status: <code>` and nothing consumes it — the exit code is discarded (`apps/SennheiserTestRunner/Program.cs:376-401`). There is no retry queue, no local outbox, and no check that the backend accepted the record.
- Blocks: if the network drops or the endpoint 500s, the unit's results are lost with no trace beyond one line in a console nobody reads, and the bench reports success. On a factory line this is silent data loss of serials and verdicts.

### No validation that `results.json` and `calibracion.txt` belong to the same run
- Problem: `apps/SennheiserTestRunner/Program.cs:127-136` cleans once at startup, and every subsequent writer uses a fixed filename (`results.json`, `calibracion.txt`, `knob_left.json`). If a stage is skipped by a retry-exhaustion path, a previous run's file is still there to be read.
- Blocks: trustworthy per-unit attribution. `scripts/getFinalResults.py:14-25` globs whatever it finds with no run identity.

### The Bluetooth-prompt result and the device-selection result are both non-blocking
- Problem: `apps/SennheiserTestRunner/Program.cs:233-245` (prompt result discarded) and `show_bluetooth_connect.ps1:90` (`ShowDialog() | Out-Null`). The device-selection dialog at `Program.cs:265-270` *is* checked, so the two are inconsistent.
- Blocks: a real "stop and let the operator fix the setup" path for the Bluetooth stage, which is the one stage that genuinely depends on physical setup being right before the other stages measure.

### `AskForSerial2` has no serial-format validation
- Problem: `apps/pruebasAudifonos/AskForSerial2/AskForSerial2/Form1.cs:33-45` accepts any non-whitespace text and writes it to `serial.txt`, which flows into the `<SerialNumber>` element at `scripts/converter.py:68` and into the file name at `apps/FunctionalButtonTest/SummaryPanel.cs:243`.
- Blocks: rejecting a mistyped serial before the 10 minutes of testing it costs, and preventing a malformed serial from producing an unopenable report filename.

---

## Test Coverage Gaps

### The entire C# codebase is untested
- What's not tested: all 7,500+ lines of C# — the orchestrator's stage sequencing and verdict interpretation (`apps/SennheiserTestRunner/Program.cs`), the level/station/knob state machines (`apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs`), the RS connection cycle (`apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs`), the Bluetooth device-detection priority ladder (`apps/FunctionalButtonTest/BluetoothDetector.cs`), and the profile registry (`apps/FunctionalButtonTest/Deviceprofileregistry.cs`).
- Files: every `.cs` file in `apps/` and `tools/`. There is no test project, no test framework reference, and no CI configuration anywhere in the repo.
- Risk: this is why every High-severity item above exists. The stage-passed/stage-resulted split-brain, the unsanitised report filename, the missing `return` after `MessageBox.Show`, the stale-serial reuse, and the C#/Python knob divergence are all pure logic that a handful of unit tests would have caught. The Python side is well covered by contrast.
- Priority: **High.** The cheapest high-value target is the *pure* logic that already exists as free functions: extract `RunControlsTest`'s result interpretation, `KnobVerdict`, `ShowStationGoldenVerdict`'s `chk2/chk3/chk4`, and `getFinalResults.first_match` into static methods and cover them.

### `Form1.KnobVerdict` has no coverage, unlike its Python twin
- What's not tested: `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:1220-1286`. `scripts/test_signal_detection.py:159-174` (`test_knob_verdict`) and `:141-157` (`test_knob_channel_active`) cover `getFinalResults.knob_verdict` and `db_chart.channel_active` — the Python half only.
- Files: the two locations above.
- Risk: the two implementations compute separation differently (abs vs signed) and nothing would notice. This is the single clearest case where the untested C# and the tested Python are already drifting.
- Priority: **High**, and the fix is small: add a test that feeds the same fixture JSON to both and asserts identical verdicts.

### `db_chart.py`'s signal thresholds are explicitly uncalibrated
- What's not tested: `scripts/db_chart.py:16-20` carries the comment "provisional values until calibrated against known-good units in the field. Tune after a week of real data." `scripts/test_signal_detection.py:88-116` asserts the thresholds behave on synthetic chirps and synthetic ambient — it does not validate them against a real capture.
- Files: `scripts/db_chart.py:16-20`, `scripts/test_signal_detection.py:88-116`
- Risk: the RS195 knob verdict depends on `channel_signal_ok` declaring exactly one channel active (`apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:1230-1233`, `scripts/getFinalResults.py:196-199`). A crest-factor threshold of `20.0` that is too tight for a real music excerpt flips the knob verdict for a perfectly good unit. `README.md:171-207` documents the feature and the tuning is still pending.
- Priority: **Medium** — needs field data, not code, but the threshold should at least be `config.json`-drivable so a station can tune it without a code change.

### `test_signal_detection.py` is the entire test suite and is not wired into anything
- What's not tested: nothing runs `scripts/test_signal_detection.py` automatically. There is no CI workflow, no pre-commit hook, and `batch/build-all.bat` does not invoke it.
- Files: `scripts/test_signal_detection.py` (437 lines, the only test file in the repo), `batch/build-all.bat:26-37`
- Risk: a refactor that breaks `converter.py`'s XML shape or `getFinalResults.knob_verdict` ships undetected. `test_converter_schema` and `test_converter_new_subtests` exist specifically to guard the backend contract, and they only run if a human remembers.
- Priority: **Medium** — one line in `build-all.bat` (`python scripts\test_signal_detection.py` before the publish, with `if errorlevel 1 exit /b 1`) makes the existing suite load-bearing.

### `converter.py`'s `TestIDNumber` stability and `getFinalResults` crash paths are uncovered
- What's not tested: (a) nothing asserts the `SUBTESTS` index → `TestIDNumber` mapping, so the backend-contract invariant at `scripts/converter.py:33` can be broken silently; (b) nothing exercises `getFinalResults.main()` against a truncated `results.json` or a short `Resultado` line — the two unguarded crash paths documented above.
- Files: `scripts/converter.py:34-40,117-121`, `scripts/getFinalResults.py:305-308,339-342`, `scripts/test_signal_detection.py:185-197,403-424`
- Risk: (a) renumbering the backend's subtest IDs by inserting a list entry; (b) losing a unit's aggregate record with only a traceback in the console.
- Priority: **Medium** — the infrastructure to test both already exists in `test_signal_detection.py` (`xml_result`/`subtest_names` helpers at lines 175-183).

### Bluetooth P/Invoke and device-detection priorities are structurally untestable
- What's not tested: `apps/FunctionalButtonTest/BluetoothDetector.cs` (the 5-tier detection priority ladder, the P/Invoke marshalling) and `apps/FunctionalButtonTest/AppCommandRouter.cs` (message decoding, hotkey registration). Both are Windows- and hardware-bound.
- Files: as above.
- Risk: a Windows update or a device with an unexpected friendly name changes which devices appear, and the only symptom is a per-button FAIL in `Prueba_*.txt` that reads like a defective unit.
- Priority: **Low** for automated tests (not worth mocking P/Invoke), **High** for the logging fix listed under Fragile Areas — the gap is best closed by diagnostics, not by tests.

---

## Quick Wins (ordered by value per unit of effort)

1. `git rm --cached scripts/config.json` + ship a `.example` — removes the credential-commit risk entirely.
2. Require `"True"` in `RunAudioTest` (`apps/SennheiserTestRunner/Program.cs:314`) — one line, closes a false-PASSED production bug.
3. Add `"serial*"` to `CleanOldFiles` (`apps/SennheiserTestRunner/Program.cs:129`) and fail closed in `GetSerial` — closes wrong-serial-on-cancel.
4. `FILE_PATTERN_MICROPHONE = "MicroTest_*.txt"` + `sorted()` in `first_match` (`scripts/getFinalResults.py:21,62-64`) — closes a missing-subtest class.
5. Delete `pictureBox1` from both designers and both `.resx` files — removes 28,232 lines and the worst merge-conflict surface in the repo.
6. Delete `EnsurePythonRequests` (`apps/SennheiserTestRunner/Program.cs:408-428`) and the `System.Management` reference — two unused dependencies, one prompt, one restore.
7. Propagate `converter.py`'s upload failure to a non-zero exit and check it in `RunResultsScripts` — closes silent data loss.
8. Delete the two `show_bluetooth*.ps1` split copies, keep `show_bluetooth.ps1`, update `build-all.bat` — removes ~130 duplicated lines and one source-of-truth split.
9. Add `RedirectStandardError` to `RunStationCalibrationScript` (`apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:407`) — matches the fix commit `346275f` already applied to its sibling.
10. `python scripts\test_signal_detection.py` as a gate in `batch/build-all.bat` — makes the existing 437-line suite load-bearing.

---

*Concerns audit: 2026-09-28*
