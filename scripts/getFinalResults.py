import glob
import json

def first_match(pattern):
    matches = glob.glob(pattern)
    return matches[0] if matches else None

def read_text_file(path):
    encodings = ["utf-8", "cp1252", "latin-1"]
    for enc in encodings:
        try:
            with open(path, "r", encoding=enc) as f:
                return f.read().lstrip("\ufeff")
        except UnicodeDecodeError:
            continue
    with open(path, "r", encoding="utf-8", errors="ignore") as f:
        return f.read().lstrip("\ufeff")

serialfile = first_match("serial*")

final_results = {}

if serialfile:
    final_results["serial"] = read_text_file(serialfile).strip()
else:
    final_results["serial"] = ""

audiofile = first_match("hearingPass*")
if audiofile:
    final_results["distorsion"] = read_text_file(audiofile).strip()
    if final_results["distorsion"]=="True":
        final_results["distorsion"] = "PASS"
    else:
        final_results["distorsion"] = "FAIL"
else:
    final_results["distorsion"] = "SKIPPED"

if glob.glob("results.json"):
    results_text = read_text_file("results.json").lstrip("\ufeff")
    results = json.loads(results_text)

    for m in results["measurements"]:
        if m["channel"] == "Left":
            final_results["left_dbfs"] = round(m["dbfs"],2)
            final_results["left_peak"] = round(m["peak_dbfs"],2)
        if m["channel"] == "Right":
            final_results["right_dbfs"] = round(m["dbfs"],2)
            final_results["right_peak"] = round(m["peak_dbfs"],2)

    diff = abs(final_results["right_dbfs"]-final_results["left_dbfs"])
    if diff>2:
        final_results["balance"] = "FAIL"
    else:
        final_results["balance"] = "PASS"

    if final_results["left_dbfs"]>-10 or final_results["left_dbfs"]<-30 or final_results["right_dbfs"]>-10 or final_results["right_dbfs"]<-30:
        final_results["volume"] = "FAIL"
    else:
        final_results["volume"] = "PASS"

    peak = max(final_results["left_peak"],final_results["right_peak"])
    if peak > 0:
        final_results["clipping"] = "FAIL"
    else:
        final_results["clipping"] = "PASS"
else:
    final_results["balance"] = "SKIPPED"
    final_results["volume"] = "SKIPPED"
    final_results["clipping"] = "SKIPPED"

btfile = first_match("Prueba_*")
if btfile:
    for line in read_text_file(btfile).splitlines():
        if("Conexión Bluetooth" in line):
            parts=line.split()
            final_results["bluetooth"] = parts[2]
        if("Play / Pausa" in line):
            parts=line.split()
            final_results["play_pausa"] = parts[3]
        if("Anterior" in line):
            parts = line.split()
            final_results["anterior"] = parts[1]
        if("Siguiente" in line):
            parts = line.split()
            final_results["siguiente"] = parts[1]
        if("Subir Volumen" in line):
            parts = line.split()
            final_results["subir_volumen"] = parts[2]
        if("Bajar Volumen" in line):
            parts = line.split()
            final_results["bajar_volumen"] = parts[2]

micfile = first_match("MicroTest_*")
if micfile:
    for line in read_text_file(micfile).splitlines():
        if "Resultado" in line:
            parts = line.split()
            if "PAS" in parts[2]:
                final_results["resultado_mic"] = "PASS"
            else:
                final_results["resultado_mic"] = "FAIL"
else:
    final_results["resultado_mic"] = "SKIPPED"

with open("final_results.json","w") as f:
    # Attach StartTime/EndTime from tiempo files if available (ms since epoch -> UTC)
    try:
        def read_ms_file(p):
            import os
            if os.path.exists(p):
                with open(p,'r') as tf:
                    txt = tf.read().strip()
                    if txt.isdigit():
                        return int(txt)
            return None

        t1 = read_ms_file('tiempo1.txt')
        t2 = read_ms_file('tiempo2.txt')
        if t1 is not None:
            from datetime import datetime
            final_results['StartTime'] = datetime.utcfromtimestamp(t1/1000.0).strftime('%Y-%m-%d %H:%M:%S')
        if t2 is not None:
            from datetime import datetime
            final_results['EndTime'] = datetime.utcfromtimestamp(t2/1000.0).strftime('%Y-%m-%d %H:%M:%S')
    except Exception:
        # best-effort; if anything fails, leave StartTime/EndTime absent
        pass

    json.dump(final_results,f,indent=4)
