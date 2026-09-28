# External Integrations

**Analysis Date:** 2026-09-28

## APIs & External Services

**Backend / cloud result upload (the only outbound network call in the repo):**
- Sennheiser production-result backend, referenced as "Azure API" / `DataWipeResult`
  - Client: Python stdlib `urllib.request` — no third-party HTTP library (`scripts/converter.py:2,162-169`)
  - Endpoint resolution order: `AZURE_API_ENDPOINT` env var → `endpoint` in `scripts/config.json` → empty string (`scripts/converter.py:14`)
  - Method: `POST` with `Content-Type: application/xml`, 15 s timeout
  - Payload: XML document `DataWipeResultV2`, built by `build_xml()` (`scripts/converter.py:63-143`) and namespace-stamped with `http://winit/webservices/` by string replacement rather than `ET.register_namespace` (`scripts/converter.py:145-156`)
  - Documented shape: `https://.../api/DataWipeResult?code=...` (`README.md:297`) — the `code=` query parameter carries the access code, so authorization is by URL secret, not by header or token
  - Auth: none. No `Authorization` header; the XML `<accesstoken>` element exists but is always written empty (`scripts/converter.py:142`)
  - Retries: none. `upload()` makes exactly one attempt and converts any exception into a printed message (`scripts/converter.py:165-169,193-198`). The runner does not inspect `converter.py`'s exit code (`apps/SennheiserTestRunner/Program.cs:390-400`), so a failed upload never fails the station run.
  - Failure behavior: the XML is written to `final_results_converted.xml` **before** the upload is attempted (`scripts/converter.py:185-193`), so a network failure never loses the record; output is `Upload status: FAILED` + the exception text. With no endpoint configured, upload is skipped with a warning (`scripts/converter.py:15-16,189-191`).
  - Azure hosting is inferred from the env-var name `AZURE_API_ENDPOINT` and the function-key URL shape; nothing in the repo proves the host or its auth model.

**Hardware / audio (on-premises, no cloud):**
- miniDSP E.A.R.S. coupler — stereo USB input used as the measurement microphone for the level test and the golden-unit station calibration (`README.md:145`, `README.md:341-345`). Reached through Windows WASAPI, not a vendor SDK.
- Windows audio stack via NAudio — `WaveOutEvent` (playback) and `WaveInEvent` (capture) in `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:62-63`, `apps/MicroTestCloud/MicroTestCloud/Form1.cs:23-24`, `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:26,31`, `apps/FunctionalButtonTest/AudioPlayer.cs:24`.
- Default playback endpoint volume is read/written through `MMDeviceEndpoint.AudioEndpointVolume` (`NAudio.CoreAudioApi`) — `tools/VolumeHelper/Program.cs:22-23`, `apps/FunctionalButtonTest/VolumeMonitor.cs:25`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:334`.
- No audio-driver-level or vendor-SDK dependency; standard Windows multimedia only.

**OS / Windows platform:**
- Bluetooth enumeration via Win32 P/Invoke on `BluetoothApis.dll` — paired/remembered/connected devices are listed, never paired programmatically (`apps/FunctionalButtonTest/BluetoothDetector.cs:87-110,132-159`). Wired/USB audio endpoints are detected through NAudio `EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)` and matched against a hardcoded Realtek/Synaptics name list (`apps/FunctionalButtonTest/BluetoothDetector.cs:112-123,170-248`).
- Windows Settings is opened for pairing guidance: `Start-Process "ms-settings:bluetooth"` (`show_bluetooth_connect.ps1:75`, `show_bluetooth_disconnect.ps1:82`, `show_bluetooth.ps1:115`).
- Bluetooth teardown uses PnP cmdlets, not an API: `Get-PnpDevice -Class Bluetooth | Where-Object {...} | Remove-PnpDevice -Confirm:$false -Force`, run with `$ErrorActionPreference = 'SilentlyContinue'` (`apps/SennheiserTestRunner/Program.cs:403-406`). This needs PnP/admin rights; failures are silently swallowed, so the disconnect step can quietly no-op on a locked-down bench.

**External executables (subprocess boundary):**
- `VolumeHelper.exe` — repo-built console helper, launched with a volume percentage and a 5 s timeout from three places: `apps/SennheiserTestRunner/Program.cs:441`, `apps/MicroTestCloud/MicroTestCloud/Form1.cs:614`, `apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:702`.
- `python` — every analysis/aggregation script, launched via `ProcessStartInfo` (`apps/SennheiserTestRunner/Program.cs:387,399,412,424`; `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:408,899`). Script paths are resolved by `ResolvePythonScriptPath()` against cwd, `cwd/scripts`, exe dir, and `exe/scripts` (`apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:924-936`).
- `RefurbishToolArvato\RefurbishTool.exe` — **optional, external, not in this repo**. Probed at `<root>/RefurbishToolArvato/RefurbishTool.exe` and `<exeDir>/RefurbishToolArvato/RefurbishTool.exe`; missing is a logged warning, not an error (`apps/SennheiserTestRunner/Program.cs:99-117`).
- `powershell` — runs the three Bluetooth prompt scripts (`apps/SennheiserTestRunner/Program.cs:237,244`).
- `pip` — invoked as `python -m pip install --user requests` when the import probe fails (`apps/SennheiserTestRunner/Program.cs:408-428`). Nothing under `scripts/` imports `requests`; `converter.py` uses `urllib.request`. This is an unnecessary runtime network dependency on every LevelTest run.

## Data Storage

**Databases:**
- None. No SQL, no SQLite, no ORM, no `csv`/`pandas`/`numpy` anywhere in `scripts/`.
- The effective "database" is a flat set of files in the runner's working directory (`bin/`). Schema and producer for each:

| File | Written by | Read by |
|---|---|---|
| `serial.txt` | `AskForSerial2` | `SennheiserTestRunner` (`Program.cs:252-253`), `getFinalResults.py` |
| `Prueba_*.txt` | `BluetoothHeadphoneTest` | runner (`Program.cs:288`), `getFinalResults.py` |
| `hearingPassResults.txt` | `AudioTest` | `getFinalResults.py` |
| `MicroTest_*.txt` | `MicroTestCloud` | runner (`Program.cs:338`), `getFinalResults.py` |
| `recorded.wav` | LevelTest capture | `db_chart.py --input` |
| `results.json` | `db_chart.py --json-out` | runner (`Program.cs:363`), `getFinalResults.py`, `station_calibration.py` |
| `knob_left.json`, `knob_right.json` | LevelTest (RS195 knob takes) | `getFinalResults.py` |
| `audio_plays.json` | LevelTest playback log | `getFinalResults.py` |
| `calibracion.txt` | LevelTest calibration pass 1 | `db_chart.py --baseline`, `station_calibration.py --baseline` |
| `station_calibration.json` | `station_calibration.py --out` | runner gate (`Program.cs:184-210`), `getFinalResults.py` |
| `final_results.json` | `getFinalResults.py` | `converter.py` |
| `final_results_converted.xml` | `converter.py --output` | uploaded, then manual inspection |
| `tiempo1/tiempo2.txt`, `diferencia_minutos.txt` | runner | `getFinalResults.py` |
| `runner_log.txt` | runner (`Program.cs:20,34`) | troubleshooting only |

  Consumed by glob patterns declared at `scripts/getFinalResults.py:15-25`. All are wiped by `CleanOldFiles()` at the start of every run (`apps/SennheiserTestRunner/Program.cs:127-136`) and are gitignored (`.gitignore`).

**File Storage:**
- Local filesystem only. No cloud blob/object storage, no shared network share.
- Media assets travel next to the executable and are copied by `batch/build-all.bat:40-67`: `run.bat`, `show_bluetooth_*.ps1`, `scripts/`, `miniDSP.jpg`, `PistaAudio/`, `audio/`, `karmaPolice.wav`, optional `serial.txt`.
- `MicroTestCloud` and `AudioTest` declare their media as `Content` with `CopyToOutputDirectory=PreserveNewest` (`apps/MicroTestCloud/MicroTestCloud/MicroTestCloud.csproj:17-23`, `apps/pruebasAudifonos/AudioTest/AudioTest/AudioTest.csproj:20-22`); `FunctionalButtonTest` instead embeds its assets as `EmbeddedResource` (`apps/FunctionalButtonTest/BluetoothHeadphoneTest.csproj:19-22`).

**Caching:**
- None. No cache library, no memoization layer. The only persistence-as-cache is `station_calibration.json`, reused until it is `FAIL` or older than `calibration_max_age_hours` (`apps/SennheiserTestRunner/Program.cs:142-180,182-210`).

## Authentication & Identity

**Auth Provider:**
- None. No OAuth, no tokens, no user accounts, no Windows identity used for the backend call. Access to the result API is by the secret `code=` query parameter embedded in the endpoint URL (`README.md:297`).
- Station-operator authentication is delegated entirely to the Windows logon session; the app reads no credentials.

**Operator identity:**
- `USERNAME` env var, falling back to the literal `"tester1"` (`scripts/converter.py:18-19`), written into the XML `<Username>` element (`scripts/converter.py:105`).
- On Windows the `USERNAME` variable is always set, so the `"tester1"` fallback effectively never fires in production — every upload is attributed to the Windows account name, not to a named tester.

**Machine identity:**
- Hard-coded in `scripts/converter.py:21-30` and emitted into every XML: `MachineName = "AudioTester"`, `TestArea = "MEXICALI_R2"`, `Program = "HP_MXLR2"`, `Contract = "10083"`, `dbType = ""`. Not configurable via `config.json`.

**Bluetooth pairing:**
- Fully OS-managed. The app only enumerates devices Windows already knows (`apps/FunctionalButtonTest/BluetoothDetector.cs:132-159`) and opens `ms-settings:bluetooth` for the operator to pair manually (`show_bluetooth_connect.ps1:75`). No custom pairing credentials, no PIN handling.

## Monitoring & Observability

**Error Tracking:**
- None. No Sentry/Datadog/App Insights, no exception reporting SDK, no crash dump collection.

**Logs:**
- Single flat file: `bin\runner_log.txt`, opened truncate-not-append with `AutoFlush = true` (`apps/SennheiserTestRunner/Program.cs:20,34`). Format is `HH:mm:ss.fff | message`, written to the file and mirrored to stdout (or stderr when `isError: true`) — `apps/SennheiserTestRunner/Program.cs:89-97`. No log level, no structured fields, no rotation.
- User-facing diagnostics are WinForms `MessageBox` dialogs: the station-lock banner (`apps/SennheiserTestRunner/Program.cs:170-178`) and LevelTest's red "no se está detectando suficiente audio" banner (`README.md:194-196`).
- Python scripts log to stdout/stderr with a `[WARNING]` prefix (`scripts/common.py:46`, `scripts/converter.py:16,190,196`).

**Metrics:**
- None collected or exported. The only quantitative telemetry is the audio measurement set in `results.json`/`final_results.json` (`left_dbfs`, `right_dbfs`, peaks, balance, clipping), which reaches the backend as `<subtest>` rows in the XML (`scripts/converter.py:117-138`).

## CI/CD & Deployment

**Hosting:**
- None. The product is a Windows desktop app deployed to refurbishing-line test-bench PCs, not a hosted service.
- Deployment = run `batch\build-all.bat` on a Windows dev box, then copy the whole `bin\` folder to the station. `batch\build-all.bat:22-37` publishes `SennheiserTestRunner.exe` (self-contained single file, hosts every test form in-process) and `VolumeHelper.exe`; `:40-67` copies runtime files.

**CI Pipeline:**
- None. No `.github/workflows`, no Azure Pipelines, no AppVeyor, no Jenkinsfile, no pre-commit hooks. The only `.github/` directories are three 3-line `copilot-instructions.md` files (`apps/FunctionalButtonTest/.github/`, `apps/pruebasAudifonos/AudioTest/.github/`, `apps/pruebasAudifonos/LevelTest/.github/`) containing Azure MCP tool rules — no build config.
- Nothing compiles the repo automatically; `batch/build-all.bat` is the entire verification gate.

## Environment Configuration

**Required env vars:**
- `AZURE_API_ENDPOINT` — upload target; overrides `config.json → endpoint` (`scripts/converter.py:14`). Without it (or `endpoint`) the XML is saved and the upload is skipped with a warning.
- `USERNAME` — XML `Username` (`scripts/converter.py:19`); effectively always populated by Windows.
- `PATH` must resolve `python` (3.9+) and `powershell`.
- `DEVICE_NAME` and `STATION_CALIB` — written by the runner into its own process (`apps/SennheiserTestRunner/Program.cs:152,281`) and read back by the in-process forms; never set by hand.
- `QUICK_AUDIO`, `CALIBRATION` — optional operator switches (`apps/pruebasAudifonos/AudioTest/AudioTest/Form1.cs:42`, `apps/pruebasAudifonos/LevelTest/HeadPhoneTest2/Form1.cs:187,709`).

**Secrets location:**
- The backend access code is the `code=` query parameter of the `endpoint` URL stored in `scripts/config.json` (`README.md:297`). That file is gitignored — `.gitignore` states "Site config (may contain API endpoint/keys); edit locally, don't commit" — and the committed version ships with an empty `endpoint`.
- No Azure credential, no managed identity, no `user-secrets`, no key vault, no credential manager. Whoever installs a station must create `bin\scripts\config.json` manually and supply the endpoint locally.
- No secret is read from the C# side at all.

## Webhooks & Callbacks

**Incoming:**
- None. No ASP.NET, no Kestrel, no `HttpListener`, no listening socket, no message queue, no exposed RPC. Every `.csproj` is `OutputType=WinExe` or `Exe`; nothing serves or receives.

**Outgoing:**
- Exactly one: the XML `POST` in `scripts/converter.py:162-169` (see "Backend / cloud result upload"). One attempt, no retry, no backoff, no queue — the record is lost from the backend's perspective if the machine is offline at that moment, though the XML survives on disk.
- The optional `pip install` of `requests` (`apps/SennheiserTestRunner/Program.cs:424`) is a second, unintended outbound call to PyPI.

**Callbacks / polling:**
- None. No webhook receipts, no long-poll, no scheduled sync. Result delivery is synchronous, inline, at the end of a station run.

---

*Integration audit: 2026-09-28*
