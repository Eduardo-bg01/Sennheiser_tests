using System;
using System.Collections.Generic;

namespace BluetoothHeadphoneTest
{
    public enum TestResult { Pending, Pass, Fail }

    public class TestRecord
    {
        public string Name { get; set; } = string.Empty;
        public TestResult Result { get; set; } = TestResult.Pending;
        public DateTime? Timestamp { get; set; }
    }

    public class TestSession
    {
        public string Folio { get; private set; }
        public int CurrentTestIndex { get; set; } = 0;
        public List<TestRecord> Records { get; private set; }
        public DateTime StartTime { get; private set; }

        public TestSession()
        {
            Folio = GenerateFolio();
            StartTime = DateTime.Now;
            Records = new List<TestRecord>
            {
                new TestRecord { Name = "Conexión Bluetooth" },
                new TestRecord { Name = "Play / Pausa" },
                new TestRecord { Name = "Anterior" },
                new TestRecord { Name = "Siguiente" },
                new TestRecord { Name = "Subir Volumen" },
                new TestRecord { Name = "Bajar Volumen" },
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
            foreach (var r in Records) { r.Result = TestResult.Pending; r.Timestamp = null; }
        }

        public BluetoothDeviceInfo SelectedDevice { get; set; }

        public bool AllPassed
        {
            get { foreach (var r in Records) if (r.Result != TestResult.Pass) return false; return true; }
        }
    }
}
