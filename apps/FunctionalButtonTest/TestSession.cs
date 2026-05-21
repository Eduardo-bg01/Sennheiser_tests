using System;
using System.Collections.Generic;

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
        public string Folio { get; private set; }
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
            Folio = GenerateFolio();
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

        private string GenerateFolio()
        {
            var rng = new Random();
            return $"BT-{DateTime.Now:yyMMdd}-{rng.Next(1000, 9999)}";
        }

        public void Reset()
        {
            Folio = GenerateFolio();
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
    }
}