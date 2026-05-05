import glob
import json

def first_match(pattern):
    matches = glob.glob(pattern)
    return matches[0] if matches else None

serialfile = first_match("serial*")

final_results = {}

if serialfile:
    with open(serialfile,"r",encoding="utf-8") as f:
        final_results["serial"] = f.read().strip()
else:
    final_results["serial"] = ""

audiofile = first_match("hearingPass*")
if audiofile:
    with open(audiofile,"r",encoding="utf-8") as f:
        final_results["distorsion"] = f.read().strip()
        if final_results["distorsion"]=="True":
            final_results["distorsion"] = "PASS"
        else:
            final_results["distorsion"] = "FAIL"
else:
    final_results["distorsion"] = "SKIPPED"

if glob.glob("results.json"):
    with open("results.json","r") as f:
        results = json.load(f)

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
    with open(btfile,"r",encoding="utf-8") as f:
        for line in f:
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
    with open(micfile,"r",encoding="utf-8") as f:
        for line in f:
            if "Resultado" in line:
                parts = line.split()
                if "PAS" in parts[2]:
                    final_results["resultado_mic"] = "PASS"
                else:
                    final_results["resultado_mic"] = "FAIL"
else:
    final_results["resultado_mic"] = "SKIPPED"

with open("final_results.json","w") as f:
    json.dump(final_results,f,indent=4)
