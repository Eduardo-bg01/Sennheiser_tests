# Sennheiser_tests

## LevelTest — Prueba de perilla de balance (RS195)

Solo se activa cuando el modelo seleccionado en FunctionalButtonTest es RS 195
(env var `DEVICE_NAME` → normalizado `rs195`).

Después de la prueba de nivel (audioSweep), al pulsar "Siguiente" se inicia la
toma de perilla:

1. El operador gira la perilla completamente a la IZQUIERDA (hasta el tope) y
   pulsa OK. Una música suena continua mientras el E.A.R.S. graba en estéreo
   a `recorded_knob_left.wav`.
2. A los ~10 s se pide girar la perilla a la DERECHA y pulsar "Siguiente". La
   música no se detiene; al confirmar, cambia solo la grabación a
   `recorded_knob_right.wav`.
3. A los 7 s la toma termina y se ejecuta `db_chart.py` sobre ambas grabaciones
   (`knob_left.json`, `knob_right.json`).

Criterio (agnóstico al espejo del jig E.A.R.S.): cada toma debe activar
exactamente UN canal, las dos tomas deben activar canales diferentes y la
separación entre canal activo y silenciado debe ser ≥ `KNOB_SEPARATION_DB`
(15 dB, provisional). El veredicto se muestra en pantalla; si falla, se ofrece
repetir la toma.

## Resultado en el pipeline

`getFinalResults.py` lee `knob_left.json`/`knob_right.json` (solo si ambos
existen) y emite `balance_knob` = PASS / FAIL en `final_results.json`;
`converter.py` lo sube como subtest `balance_knob`. Si no hay archivos de
perilla, el campo queda N/A/SKIPPED.