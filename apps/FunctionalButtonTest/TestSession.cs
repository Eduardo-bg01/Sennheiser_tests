using System;
using System.Collections.Generic;
using System.Text;

namespace BluetoothHeadphoneTest
{
    public enum TestResult { Pending, Pass, Fail, NotApplicable }

    public class TestRecord
    {
        public string Name { get; set; } = string.Empty;
        public TestResult Result { get; set; } = TestResult.Pending;
        public DateTime? Timestamp { get; set; }

        public bool IsApplicable => Result != TestResult.NotApplicable;
    }

    public class TestSession
    {
        public int CurrentTestIndex { get; set; } = 0;

        /// <summary>
        /// Todos los records en orden fijo (6 pruebas siempre).
        /// Las que no aplican al modelo tienen Result = NotApplicable.
        /// </summary>
        public List<TestRecord> Records { get; private set; }

        public DateTime StartTime { get; private set; }

        /// <summary>Perfil activo del dispositivo seleccionado.</summary>
        public DeviceProfile Profile { get; private set; }

        public TestSession()
        {
            Profile = new DeviceProfile("(pendiente)");
            StartTime = DateTime.Now;
            Records = BuildRecords(Profile);
        }

        /// <summary>
        /// Aplica el perfil del modelo y reconstruye los Records.
        /// Las pruebas que no aplican quedan como NotApplicable.
        /// Llamado desde TestStepManager.Initialize().
        /// </summary>
        public void ApplyProfile(DeviceProfile profile)
        {
            Profile = profile ?? new DeviceProfile("(genérico)");
            Records = BuildRecords(Profile);
        }

        /// <summary>
        /// Siempre crea los 6 Records en el mismo orden.
        /// Los que no aplican al perfil se marcan como NotApplicable desde el inicio.
        /// </summary>
        private static List<TestRecord> BuildRecords(DeviceProfile p)
        {
            return new List<TestRecord>
            {
                new TestRecord
                {
                    Name   = "Conexión Bluetooth",
                    Result = p.HasBluetooth ? TestResult.Pending : TestResult.NotApplicable
                },
                new TestRecord
                {
                    Name   = "Play / Pausa",
                    Result = p.HasPlayPause ? TestResult.Pending : TestResult.NotApplicable
                },
                new TestRecord
                {
                    Name   = "Anterior",
                    Result = p.HasPreviousTrack ? TestResult.Pending : TestResult.NotApplicable
                },
                new TestRecord
                {
                    Name   = "Siguiente",
                    Result = p.HasNextTrack ? TestResult.Pending : TestResult.NotApplicable
                },
                new TestRecord
                {
                    Name   = "Subir Volumen",
                    Result = p.HasVolumeUp ? TestResult.Pending : TestResult.NotApplicable
                },
                new TestRecord
                {
                    Name   = "Bajar Volumen",
                    Result = p.HasVolumeDown ? TestResult.Pending : TestResult.NotApplicable
                },
            };
        }

        public void Reset()
        {
            StartTime = DateTime.Now;
            CurrentTestIndex = 0;
            Records = BuildRecords(Profile);
        }

        public BluetoothDeviceInfo SelectedDevice { get; set; }

        /// <summary>
        /// Solo considera aprobado si todas las pruebas APLICABLES son Pass.
        /// Las NotApplicable no cuentan.
        /// </summary>
        public bool AllPassed
        {
            get
            {
                foreach (var r in Records)
                    if (r.IsApplicable && r.Result != TestResult.Pass)
                        return false;
                return true;
            }
        }

        public string GetDisplayName()
        {
            var selected = SelectedDevice;
            return selected != null && selected.IsWired && !string.IsNullOrWhiteSpace(selected.SelectedJackModel)
                ? selected.SelectedJackModel
                : selected?.Name ?? "—";
        }

        public string BuildReportText()
        {
            var sb = new StringBuilder();
            string sep = new string('─', 54);

            int passCount = 0;
            foreach (var r in Records) if (r.Result == TestResult.Pass) passCount++;

            sb.AppendLine($"Dispositivo : {GetDisplayName()}");
            sb.AppendLine($"MAC         : {SelectedDevice?.Address ?? "—"}");
            sb.AppendLine($"Fecha       : {StartTime:dd/MM/yyyy  HH:mm}");
            sb.AppendLine();
            sb.AppendLine(sep);
            sb.AppendLine($"  {"PRUEBA",-32} {"RESULTADO",-10} {"HORA"}");
            sb.AppendLine(sep);

            foreach (var rec in Records)
            {
                string res = rec.Result == TestResult.Pass ? "PASS" :
                             rec.Result == TestResult.Fail ? "FAIL" :
                             rec.Result == TestResult.NotApplicable ? "N/A" : "PEND";
                string time = rec.Result == TestResult.NotApplicable ? "—"
                    : (rec.Timestamp.HasValue
                        ? rec.Timestamp.Value.ToString("HH:mm:ss") : "--:--:--");
                sb.AppendLine($"  {rec.Name,-32} {res,-10} {time}");
            }

            sb.AppendLine(sep);
            int totalApplicable = 0, naCount = 0;
            foreach (var r in Records)
            {
                if (r.Result == TestResult.NotApplicable) naCount++;
                else totalApplicable++;
            }

            sb.AppendLine(sep);
            sb.AppendLine($"  Resultado final: {(AllPassed ? "APROBADO" : "FALLIDO")}  ({passCount}/{totalApplicable})  •  N/A: {naCount}");

            return sb.ToString();
        }
    }
}