# Sennheiser_tests

Automated test bench for Sennheiser headphone refurbishing. A Windows batch
orchestrator runs a sequence of .NET test apps against each unit (operator
listening checks, Bluetooth controls, and an automatic level measurement via a
miniDSP E.A.R.S. coupler), aggregates the verdicts, and uploads a single XML
result to the cloud API.

## Test pipeline

```
run.bat  (SennheiserTestRunner.exe is the active orchestrator)
  |
  |-- [daily] Station calibration (LevelTest /STATION_CALIB=1) -> station_calibration.json
  |           Golden-unit verification, 4 pasos; estación bloqueada hasta PASS
  |-- [daily] Ambient calibration (LevelTest /CALIBRATION=1) -> calibracion.txt
  |
  |-- AskForSerial2 ............ serial.txt
  |-- BluetoothHeadphoneTest ... Prueba_*.txt   (controls + device model)
  |-- AudioTest ................ hearingPassResults.txt ("True"/"False")
  |       - operator says "No"  -> FAIL is recorded, pipeline continues,
  |                               unit fails in the final XML
  |-- [HD/IE models only] LevelTest
  |       |- plays audioSweep through the headphones
  |       |- records the E.A.R.S. coupler mics -> recorded.wav
  |       |- db_chart.py -> results.json (+ signal-presence verdict)
  |       '- red on-screen warning if only ambient noise was captured
  |
  |-- getFinalResults.py ....... final_results.json (includes station_calibration)
  '- converter.py .............. XML upload (overall PASS/FAIL)
```

## Quick start

1. **Install .NET 9 SDK** from https://aka.ms/dotnet/download
2. **Build all projects** from the repo root:
   ```powershell
   batch\build-all.bat
   ```
   This compiles all test apps to `.exe` files in `bin\` and copies `scripts\config.json` to `bin\scripts\`.
3. **Run the full sequence**:
   ```powershell
   bin\run.bat
   ```
   Enter the serial when prompted, or pre-create `bin\serial.txt` and set `SKIP_SERIAL_PROMPT=1`.

### Environment variables

| Variable | Default | Effect |
|---|---|---|
| `RUN_MICROPHONE` | `0` | `1` enables MicroTestCloud (disabled for all models by default). |
| `QUICK_AUDIO` | auto (`1` unless mic test enabled) | Shortens the audio clip to ~7 s. |
| `SKIP_SERIAL_PROMPT` | unset | `1` uses existing `serial.txt` instead of the prompt app. |
| `MAX_RETRIES` / `RETRY_DELAY` | `5` / `2` | Retry policy for each test stage. |
| `AZURE_API_ENDPOINT` | from `config.json` | Overrides the upload endpoint. |

## Daily ambient calibration

Once per day, per machine, `run.bat` measures what the station hears when
**nothing** is playing. That baseline is what makes "the ears are only hearing
ambient noise" detectable automatically.

- **Trigger**: at startup, if `calibracion.txt` is missing, corrupt, or its
  `date` field is not today's PC date.
- **Operator steps**: select the E.A.R.S. input, click *Iniciar calibración*,
  and **leave the couplers empty** while it records 30 seconds. No audio is played.
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
- If calibration is cancelled or fails, testing continues with fixed thresholds only.

> The baseline is written by LevelTest running with `CALIBRATION=1`; run.bat sets
> this automatically. Do not launch it manually unless you know why.

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
  de repetir desde el Paso 1.
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

The unit **fails** `deteccion_senal` when ANY channel trips one of:

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

## AudioTest verdict handling

AudioTest writes `hearingPassResults.txt` containing `True` or `False`
(operator's call). `run.bat` reads the **content**, not just the file's existence:

- `True` → continue normally.
- `False` → `[AUDIO] FAILED por operador`; the pipeline still runs the remaining
  tests, but the verdict survives and the unit fails downstream.
- File missing (crash/cancelled) → retried up to `MAX_RETRIES`.

`getFinalResults.py` maps the file to `distorsion` and additionally emits
`audio_fail = FAIL` so the uploaded XML carries a dedicated failing subtest.

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
  "signal_reason": "Left: factor cresta 27.3 dB > 20 dB (...); ..."
}
```

### `final_results.json` (aggregated)

| Field | Source | Values |
|---|---|---|
| `serial` | serial.txt | text |
| `model` | Prueba_*.txt (`Dispositivo:` line) | device model text |
| `distorsion` | hearingPassResults.txt | PASS / FAIL / N/A |
| `audio_fail` | derived | present only when `distorsion == FAIL` |
| `left_dbfs`, `left_peak`, `right_dbfs`, `right_peak` | results.json | numbers |
| `balance` | \|L−R\| ≤ 2 dB | PASS / FAIL |
| `volume` | −30 ≤ dbfs ≤ −10 (N/A for HD 400U, HD 550/560S/569/599/600/650/660S and IE*; values still displayed without a pass/fail icon) | PASS / FAIL / N/A |
| `clipping` | peak ≤ 0 dBFS | PASS / FAIL |
| `deteccion_senal` | results.json `signal_present` | PASS / FAIL |
| `bluetooth`, `play_pausa`, `anterior`, `siguiente`, `subir_volumen`, `bajar_volumen` | Prueba_*.txt | PASS / FAIL / N/A |
| `resultado_mic` | MicroTest_*.txt | PASS / FAIL / N/A |
| `balance_knob` | knob_left+knob_right.json (RS195 only) | PASS / FAIL / N/A / SKIPPED |
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
| `batch/build-all.bat` | Builds all apps to `bin\` (Release). |
| `batch/run.bat` | Orchestrates calibration + full test sequence, cleanup, aggregation, upload. |
| `scripts/db_chart.py` | WAV analysis: RMS/peak/crest per channel, JSON out, optional `--baseline calibracion.txt`. |
| `scripts/getFinalResults.py` | Aggregates raw outputs into `final_results.json` (incl. `station_calibration`). |
| `scripts/station_calibration.py` | Golden-unit verdict (4 checks) → `station_calibration.json`; used by LevelTest and getFinalResults. |
| `scripts/converter.py` | `final_results.json` → XML (`DataWipeResultV2`) + API upload. Stdlib only. |
| `scripts/test_signal_detection.py` | Self-checks: `python3 scripts/test_signal_detection.py` |

## Site configuration

Per-machine settings live in `scripts/config.json` (not tracked in git;
`build-all.bat` copies it to `bin\scripts\`):

```json
{
  "endpoint": "https://.../api/DataWipeResult?code=...",
  "machine_name": "AudioTester",
  "contract": "10083",
  "test_area": "MEXICALI_R2",
  "program": "HP_MXLR2",
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
| `golden_left_dbfs` / `golden_right_dbfs` | — (required) | Reference L/R level of a known-good unit on this station (see "Obtener los valores golden" above). |
| `golden_tolerance_db` | `3.0` | Max |channel − golden| deviation for Checks 2/3. |
| `balance_max_db` | `2.0` | Max \|L − R\| for Check 4. |
| `ambient_max_dbfs` | `-30.0` | Room-noise ceiling for Check 1 (mirrors `SIGNAL_MIN_DBFS`). |
| `connection_type` | `USB` | Expected Golden-Unit connection shown to the operator before Check 2 (`USB`, `Óptico`, `Analógico`, `HDMI`). |
| `calibration_max_age_hours` | `12` | Freshness window; station must be re-verified after this. |

`AZURE_API_ENDPOINT` takes precedence over `endpoint`. Without either, the
converter saves the XML but skips the upload with a warning.

## Included apps

- `apps/pruebasAudifonos/AskForSerial2` – serial entry dialog.
- `apps/pruebasAudifonos/AudioTest` – operator listening check (writes `hearingPassResults.txt`).
- `apps/pruebasAudifonos/LevelTest` – automatic sweep/record level test; runs the daily ambient calibration (`CALIBRATION=1`) and the station calibration (`STATION_CALIB=1`).
- `apps/FunctionalButtonTest` – Bluetooth controls test.
- `apps/MicroTestCloud` – microphone test (disabled by default).

## Requirements

- Windows 10/11 with .NET 9 SDK (WinForms apps).
- Python 3.9+ on PATH (`python`) for aggregation, analysis, and upload.
- miniDSP E.A.R.S. coupler (stereo USB input) and a working output device.

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| Red "no se está detectando suficiente audio" banner | Headphones not playing or not seated on the couplers; check Windows output device and volume. |
| `deteccion_senal: FAIL` but audio audibly plays | Baseline stale or thresholds too tight — recalibrate, then tune constants in `db_chart.py`. |
| Calibration warning about loud ambient | Background music, mic gain too high, or E.A.R.S. disconnected. Fix before testing. |
| Unit uploaded as FAIL unexpectedly | Inspect `final_results.json`: some subtest is exactly `FAIL` (including `deteccion_senal` / `audio_fail`). |
| Upload status: FAILED (no endpoint) | Set `endpoint` in `scripts/config.json` or `AZURE_API_ENDPOINT`. |
