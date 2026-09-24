# Sennheiser_tests

Automated test bench for Sennheiser headphone refurbishing. A single .NET
orchestrator (`SennheiserTestRunner`) runs the test apps in-process — operator
listening checks, Bluetooth controls, and an automatic level measurement via a
miniDSP E.A.R.S. coupler — aggregates the verdicts, and uploads a single XML
result to the cloud API.

## Test pipeline

`run.bat` is a thin launcher for `SennheiserTestRunner.exe`, which is the
active orchestrator. It hosts the WinForms test apps as in-process forms:

```
bin\run.bat  →  SennheiserTestRunner.exe
  |
  |-- kill stale processes; clean previous result files
  |-- [gate] Daily station calibration (LevelTest STATION_CALIB=1, 4 pasos)
  |           runs when station_calibration.json is missing / FAIL / vencida
  |           (> calibration_max_age_hours); on FAIL the station is locked
  |           and the runner exits with code 6
  |-- RefurbishTool (external, launched if present)
  |-- prompt Bluetooth connectado (show_bluetooth_connect.ps1)
  |-- AskForSerial2 ............ serial.txt              (sin serial → exit 1)
  |-- volume 50%
  |-- BluetoothHeadphoneTest ... Prueba_*.txt   controls + device model (exit 3)
  |-- volume 100%
  |-- AudioTest ................ hearingPassResults.txt  (exit 2)
  |-- MicroTestCloud ........... MicroTest_*.txt         (exit 4)
  |-- volume 80% (MOMENTUM TW 4) / 100% (resto)
  |-- LevelTest
  |       |- plays audioSweep through the headphones
  |       |- records the E.A.R.S. coupler mics -> recorded.wav
  |       |- [RS195] balance-knob test -> knob_left.json / knob_right.json
  |       |- db_chart.py -> results.json (+ signal-presence verdict/banner)
  |       '- red on-screen warning if only ambient noise was captured
  |
  |-- getFinalResults.py ....... final_results.json (incl. station_calibration)
  '- converter.py .............. XML upload (overall PASS/FAIL)
      '- CleanupBluetooth + disconnect prompt; tiempo1/tiempo2.txt, diferencia_minutos.txt
```

Each retryable stage (controls, audio, microphone, level) is attempted up to
5 times with a 2 s wait between attempts. Runner exit codes:

| Code | Meaning |
|---|---|
| 1 | No serial provided |
| 2 | AudioTest failed (max retries, no `hearingPass*.txt`) |
| 3 | Controls test failed (max retries, no `Prueba_*.txt`) |
| 4 | Microphone test failed (max retries, no `MicroTest_*.txt`) |
| 5 | LevelTest failed (max retries, no `results.json`) |
| 6 | Station calibration failed — station LOCKED |

## Quick start

1. **Install .NET 9 SDK** from https://aka.ms/dotnet/download
2. **Build** from the repo root:
   ```powershell
   batch\build-all.bat
   ```
   Publishes `SennheiserTestRunner.exe` (self-contained single file; embeds the
   test apps) and `VolumeHelper.exe` into `bin\`, and copies the runtime files
   (`run.bat`, `show_bluetooth_*.ps1`, `scripts\`, `miniDSP.jpg`, `PistaAudio\`,
   `audio\`) there.
3. **Run the sequence**:
   ```powershell
   bin\run.bat
   ```
   Enter the serial when prompted (`AskForSerial2`); the runner stores it in
   `bin\serial.txt` and reads it back.

### Environment variables

| Variable | Used by | Effect |
|---|---|---|
| `DEVICE_NAME` | runner (set automatically), AudioTest, LevelTest | Model chosen in the controls test. Enables the RS195 knob test and the volume-neutral HD/IE handling. |
| `QUICK_AUDIO` | AudioTest, LevelTest | `1` shortens the listening clip (~7 s). |
| `CALIBRATION` | LevelTest | `1` runs the standalone daily-ambient calibration (manual tool; see below). |
| `STATION_CALIB` | runner, LevelTest | Set to `1` by the runner while the 4-step station calibration runs. |
| `AZURE_API_ENDPOINT` | converter.py | Overrides the upload endpoint. |
| `USERNAME` | converter.py | XML `Username` (default `tester1`). |

`STATION_CALIB` and `DEVICE_NAME` are managed by the orchestrator; you normally
never set them by hand.

## Daily ambient calibration

The ambient baseline (`calibracion.txt`) is what makes "the ears are only
hearing ambient noise" detectable automatically. Normal LevelTest runs pass it
to `db_chart.py` when present; without it, only fixed thresholds are used
(plus an on-screen warning if the room baseline is itself loud).

- **Automatic refresh**: Paso 1 of the daily station calibration records 30 s
  of the room and writes `calibracion.txt`. Because the station gate
  recalibrates whenever the calibration expires, the baseline is kept fresh at
  every line start.
- **Manual standalone mode**: run LevelTest with `CALIBRATION=1`, select the
  E.A.R.S. input, click *Iniciar calibración*, and **leave the couplers empty**
  while it records 30 seconds. No audio is played.
- **Output** (`calibracion.txt`, next to the other result files):
  ```json
  {
    "date": "2026-08-20",
    "time": "09:15:00",
    "left_dbfs": -52.1,
    "left_peak": -30.2,
    "right_dbfs": -51.8,
    "right_peak": -29.9
  }
  ```
- **Force a recalibration** any time (bench moved, hardware swapped): delete `calibracion.txt`.

> LevelTest only passes the baseline to `db_chart.py` when `calibracion.txt`
> exists; a missing/corrupt file makes it fall back to fixed thresholds.

## Daily station calibration (TV Listeners)

Before production, `SennheiserTestRunner` verifies the station against a
**Golden Unit** (known-good headphone) so a drifted bench can't pass bad units.
It runs automatically at startup whenever `station_calibration.json` is
missing, contains `FAIL`, or is older than `calibration_max_age_hours`
(default `12 h`, compared as ISO-8601 UTC). On **any** failure the station is
locked (runner exits with code **6**) and production does not start until the
operator fixes the root cause and repeats from CHECK 1.

| Paso | Check | Verdict rule |
|---|---|---|
| 1 | Ambiente: nada en las copas, sin DUT, 30 s | `max(L,R) ≤ ambient_max_dbfs` |
| 2 | Golden Unit canal I (tono 1 kHz) | `|L − golden_left_dbfs| ≤ golden_tolerance_db` |
| 3 | Golden Unit canal D | `|R − golden_right_dbfs| ≤ golden_tolerance_db` |
| 4 | Balance | `|L − R| ≤ balance_max_db` |

- **Flow** (LevelTest en modo estación, `STATION_CALIB=1`, 4 wizards):
  Paso 1 graba el ambiente y escribe `calibracion.txt`; los Pasos 2/3/4
  reproducen `tone_1khz.wav` **una sola vez** a través de las copas E.A.R.S.
  y miden los canales L y D por separado; el veredicto final lo escribe
  `scripts/station_calibration.py` → `station_calibration.json`.
- **Antes del Paso 2** un diálogo pide al operador conectar la Golden Unit por
  la conexión configurada en `config.json → connection_type` (USB / Óptico /
  Analógico / HDMI) y verificar el posicionamiento RS275/255 sobre los coples.
- **Rendición**: cada `final_results.json` (y el XML `DataWipeResultV2` del
  turno) lleva el campo `station_calibration`; un `FAIL` baja el registro
  completo, así el estado de calibración queda en el historial del lote.
- **Fallos**: diálogo/banner rojo *"ACCIÓN ANTE FALLA: Detener liberación,
  revisar ambiente, posicionamiento, conexiones USB, configuración de REW y
  nivel de salida. Corregir, registrar y repetir desde CHECK 1."* con opción
  de repetir desde el Paso 1; si al terminar sigue sin pasar, el runner
  bloquea la estación (exit 6).
- **Éxito**: *"ESTACIÓN LIBERADA - Registrar resultado e iniciar producción."*
- **Estados**: `station_calibration.json` sigue vigente hasta envejecer más de
  `calibration_max_age_hours`; borrar el archivo (o cambiar hardware) fuerza
  la recalibración en el siguiente arranque.

### Obtener los valores golden

`golden_left_dbfs` / `golden_right_dbfs` son la medida de referencia del
conjunto estación + Golden Unit. Configuración por estación, una vez al
arranque de la línea (y al recambiar cualquier hardware):

1. Tome una Golden Unit conocida-buena y colóquela en los coples E.A.R.S. con
   la conexión habitual.
2. Corra LevelTest en modo normal (`audioSweep`) y lea los niveles `I:` / `D:`
   del resultado, o el `dbfs` de `results.json` (canales Left/Right).
3. Escriba estos valores en `scripts/config.json`:
   `golden_left_dbfs` y `golden_right_dbfs`.
4. Corra `batch\build-all.bat` para copiar `config.json` a `bin\scripts\`.
5. Ejecute la calibración de estación y confirme que pasa; si no, revise la
   tolerancia (`golden_tolerance_db`).

## Signal presence detection ("are we hearing anything at all?")

Every normal LevelTest run passes `calibracion.txt` to `db_chart.py`, which
computes per channel:

| Metric | Formula | Catches |
|---|---|---|
| SNR over room | `dbfs − baseline_dbfs` | nothing playing: measured ≈ ambient |
| Crest factor | `peak_dbfs − dbfs` | sparse clicks over silence (ambient shape) |
| Absolute floor | `dbfs` | dead rig / silent capture |

`db_chart.py` reports `signal_present = false` (and `channel_active = none`)
when ANY channel trips one of:

| Check | Threshold | Constant |
|---|---|---|
| Below absolute floor | `< −30 dBFS` | `SIGNAL_MIN_DBFS` |
| Too peaky (ambient-like) | `crest > 20 dB` | `SIGNAL_MAX_CREST_DB` |
| Not enough above room | `SNR < 6 dB` | `SIGNAL_MIN_SNR_DB` |

Thresholds live at the top of `scripts/db_chart.py`. They are provisional —
calibrate them against known-good units after a week of real data.

On failure the operator sees a red banner on the LevelTest results screen:
*"Parece que no se está detectando suficiente audio. Asegúrese de que los
audífonos estén reproduciendo sonido."*

For models without a volume check (HD 400U, HD 550/560S/569/599/600/650/660S, IE* — see `volume` row below) the banner is suppressed when both `balance` and `clipping` pass; it is still shown if either `balance` or `clipping` fails. Volume level numbers (`I: … | D: …`) remain visible but without a pass/fail icon.

A sanity warning is printed if the ambient baseline itself is louder than
−30 dBFS (background music, mic gain too high, or a bad E.A.R.S. connection).

`signal_present` is **informational** in the final verdict: it drives the
in-app banner but does not fail the record by itself. `final_results.json`
uses `deteccion_senal` instead, which mirrors the operator's hearing verdict
(see below) to avoid invisible FAILs for the HD/IE families.

## AudioTest verdict handling

AudioTest writes `hearingPassResults.txt` containing `True` or `False` (the
operator's call). `SennheiserTestRunner` distinguishes the stage only by
**whether the file exists** (it retries up to 5 times if no file is produced,
then exits 2); the **content** is read by `getFinalResults.py`:

- `True` → `distorsion = PASS`.
- `False` → `distorsion = FAIL`, plus a dedicated `audio_fail = FAIL` subtest
  so an operator-rejected unit is clearly visible in the XML.
- If the form is cancelled or closed without a verdict, AudioTest writes
  `False` itself on close → `distorsion = FAIL`.
- If the app crashes before writing anything, the runner retries and exits 2.

`getFinalResults.py` also mirrors `distorsion` into `deteccion_senal`.

## Result files

### `results.json` (per-run, produced by db_chart.py)

```json
{
  "measurements": [
    { "channel": "Left",  "rms": 0.0207, "dbfs": -33.66,
      "peak_dbfs": -6.34, "crest_db": 27.32, "duration_sec": 40.0 },
    ...
  ],
  "signal_present": false,
  "signal_reason": "Left: factor cresta 27.3 dB > 20 dB (...); ...",
  "channel_active": "both"
}
```

### `final_results.json` (aggregated)

| Field | Source | Values |
|---|---|---|
| `serial` | serial.txt | text |
| `model` | Prueba_*.txt (`Dispositivo:` line) | device model text |
| `distorsion` | hearingPassResults.txt (`True`→PASS, else FAIL) | PASS / FAIL / N/A |
| `audio_fail` | derived | present only when `distorsion == FAIL` |
| `left_dbfs`, `left_peak`, `right_dbfs`, `right_peak` | results.json | numbers |
| `balance` | \|L−R\| ≤ 2 dB | PASS / FAIL |
| `volume` | −30 ≤ dbfs ≤ −10 (N/A for HD 400U, HD 550/560S/569/599/600/650/660S and IE*; values still displayed without a pass/fail icon) | PASS / FAIL / N/A |
| `clipping` | peak ≤ 0 dBFS | PASS / FAIL |
| `deteccion_senal` | mirrors `distorsion` (operator's AudioTest verdict) | PASS / FAIL / N/A |
| `bluetooth`, `play_pausa`, `anterior`, `siguiente`, `subir_volumen`, `bajar_volumen` | Prueba_*.txt | PASS / FAIL / N/A |
| `resultado_mic` | MicroTest_*.txt | PASS / FAIL / N/A |
| `balance_knob` | knob_left.json + knob_right.json (RS195 only) | PASS / FAIL / N/A / SKIPPED |
| `balance_knob_left`, `balance_knob_right` | per-take verdicts (RS195 only) | PASS / FAIL / N/A / SKIPPED |
| `audio_test` | audio_plays.json (LevelTest log) | `{runs, passed, result}` object |
| `station_calibration` | station_calibration.json | PASS / FAIL / N/A |
| `station_calibration_time` | station_calibration.json `time` | ISO-8601 UTC |
| `StartTime`, `EndTime` | tiempo1/tiempo2.txt | UTC timestamps |

### Overall PASS/FAIL rule

`converter.py` marks the record **FAIL** if any string-valued field equals
`FAIL`, or if the `audio_test` object reports `result: FAIL`. `N/A` (and
`SKIPPED`, e.g. knob tests on models without a balance knob) is neutral by
design. Numeric fields are informational; their pass/fail logic lives in
`getFinalResults.py`.

The DUT record `PartNumber` is filled with the device `model`, and `MiscInfo`
carries a compact trace for the backend:
`model=<model>; station_calibration=<PASS/FAIL>@<ISO-8601 UTC>; audio_test=<passed>/<runs> <result>`.
`audio_test` logs every recorded playback from LevelTest (the sweep, plus the
two RS195 knob takes); `runs` counts them and `passed` requires each one's own
verdict to pass. The station-calibration `tone_1khz` play is excluded because
it happens in a separate, pre-serial LevelTest instance.

## Scripts

| Script | Purpose |
|---|---|
| `batch/build-all.bat` | Publishes `SennheiserTestRunner.exe`, `VolumeHelper.exe` (Release, self-contained) and copies runtime files into `bin\`. |
| `scripts/db_chart.py` | WAV analysis: RMS/peak/crest per channel, signal-presence + `channel_active`, JSON out, optional `--baseline calibracion.txt`. |
| `scripts/getFinalResults.py` | Aggregates raw outputs into `final_results.json` (incl. `station_calibration`, `audio_test`). |
| `scripts/station_calibration.py` | Golden-unit verdict (4 checks) → `station_calibration.json`; used by LevelTest and getFinalResults. |
| `scripts/converter.py` | `final_results.json` → XML (`DataWipeResultV2`) + API upload. Stdlib only (supports `--no-upload`). |
| `scripts/test_signal_detection.py` | Self-checks: `python3 scripts/test_signal_detection.py` |

## Site configuration

Per-machine settings live in `scripts/config.json` (gitignored; `build-all.bat`
copies it to `bin\scripts\`):

```json
{
  "endpoint": "https://.../api/DataWipeResult?code=...",
  "golden_left_dbfs": -33.0,
  "golden_right_dbfs": -34.0,
  "golden_tolerance_db": 3.0,
  "balance_max_db": 2.0,
  "ambient_max_dbfs": -30.0,
  "connection_type": "USB",
  "calibration_max_age_hours": 12
}
```

Calibration fields:

| Field | Default | Meaning |
|---|---|---|
| `endpoint` | — | API upload URL (overridable via `AZURE_API_ENDPOINT`). |
| `golden_left_dbfs` / `golden_right_dbfs` | — (required) | Reference L/R level of a known-good unit on this station (see "Obtener los valores golden" above). |
| `golden_tolerance_db` | `3.0` | Max \|channel − golden\| deviation for Checks 2/3. |
| `balance_max_db` | `2.0` | Max \|L − R\| for Check 4. |
| `ambient_max_dbfs` | `-30.0` | Room-noise ceiling for Check 1 (mirrors `SIGNAL_MIN_DBFS`). |
| `connection_type` | `USB` | Expected Golden-Unit connection shown to the operator before Check 2 (`USB`, `Óptico`, `Analógico`, `HDMI`). |
| `calibration_max_age_hours` | `12` | Freshness window; station must be re-verified after this. |

`AZURE_API_ENDPOINT` takes precedence over `endpoint`. Without either, the
converter saves the XML but skips the upload with a warning.

## Included apps

- `apps/SennheiserTestRunner` – the orchestrator: station-calibration gate,
  serial entry, controls/audio/microphone/level sequence, aggregation and
  upload. References all other apps and hosts their forms in-process.
- `apps/FunctionalButtonTest` – Bluetooth controls test (`BluetoothHeadphoneTest`),
  includes the device-selection dialog.
- `apps/pruebasAudifonos/AskForSerial2` – serial entry dialog.
- `apps/pruebasAudifonos/AudioTest` – operator listening check (writes `hearingPassResults.txt`).
- `apps/pruebasAudifonos/LevelTest` – automatic sweep/record level test, RS195
  balance-knob test, ambient calibration (`CALIBRATION=1`) and station
  calibration (`STATION_CALIB=1`).
- `apps/MicroTestCloud` – microphone test (always run by the orchestrator).
- `tools/VolumeHelper` – sets the default playback volume; used by the runner.

## Requirements

- Windows 10/11 with .NET 9 SDK (WinForms apps; the runner is self-contained
  `win-x64` single-file).
- Python 3.9+ on `PATH` (`python`) for analysis and aggregation
  (`db_chart.py`, `getFinalResults.py`, `converter.py`, `station_calibration.py`).
  The runner installs the `requests` package on demand.
- miniDSP E.A.R.S. coupler (stereo USB input) and a working output device.

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| Red "no se está detectando suficiente audio" banner | Headphones not playing or not seated on the couplers; check Windows output device and volume. |
| `signal_present` false but audio audibly plays | Baseline stale or thresholds too tight — recalibrate, then tune constants in `db_chart.py`. |
| Calibration warning about loud ambient | Background music, mic gain too high, or E.A.R.S. disconnected. Fix before testing. |
| Unit uploaded as FAIL unexpectedly | Inspect `final_results.json`: some subtest is exactly `FAIL` (including `distorsion`/`audio_fail`/`station_calibration`). |
| Runner exits with code 6 | Station calibration FAILED — the bench is locked until the operator fixes the root cause and the Golden-Unit checks pass again. |
| Runner exits with code 2/3/4/5 | The stage could not produce its result file after 5 retries; check the app's dialog (crashed/cancelled) in `runner_log.txt`. |
| Upload status: FAILED (no endpoint) | Set `endpoint` in `scripts/config.json` or `AZURE_API_ENDPOINT`. |