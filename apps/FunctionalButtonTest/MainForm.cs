using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    public partial class MainForm : Form
    {
        private TestSession _session;
        public TestStepManager stepManager;

        // Hook global activo durante toda la sesión
        private GlobalKeyHook _globalHook;

        public MainForm()
        {
            InitializeComponent();
            _session = new TestSession();
            stepManager = new TestStepManager(this);
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Arrancar hook global — canal secundario (teclado físico)
            _globalHook = new GlobalKeyHook();
            stepManager.Initialize();
            UpdateOperatorPanel();
        }

        /// <summary>
        /// Canal principal para audífonos BT: WM_APPCOMMAND (AVRCP).
        /// Se llama para TODOS los mensajes de la ventana.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            AppCommandRouter.ProcessMessage(ref m);
            base.WndProc(ref m);
        }

        public void UpdateOperatorPanel()
        {
            labelDateTime.Text = DateTime.Now.ToString("dd/MMM/yyyy  HH:mm");

            if (_session.CurrentTestIndex == 1)
            {
                labelProgress.Text = "Preparación";
                progressBarMain.Value = 0;
            }
            else if (_session.CurrentTestIndex >= TestStepManager.TotalTests)
            {
                labelProgress.Text = "Completado";
                progressBarMain.Value = 100;
            }
            else
            {
                // idx 0 → Prueba 1, idx 2 → Prueba 2, idx 3 → Prueba 3 ... idx 6 → Prueba 6
                int displayIndex = _session.CurrentTestIndex <= 1
                    ? 1
                    : _session.CurrentTestIndex;
                int totalReal = TestStepManager.TotalTests - 1; // 6 pruebas reales
                labelProgress.Text = $"Prueba {displayIndex} de {totalReal}";
                progressBarMain.Value = Math.Min((displayIndex - 1) * 100 / totalReal, 100);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AppCommandRouter.Register(this.Handle);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WriteFallbackReportIfMissing();
            AppCommandRouter.Unregister();
            _globalHook?.Dispose();
            base.OnFormClosed(e);
        }

        private void WriteFallbackReportIfMissing()
        {
            try
            {
                bool hasActivity = _session.CurrentTestIndex > 1;
                if (!hasActivity)
                {
                    foreach (var rec in _session.Records)
                    {
                        if (rec.Result != TestResult.Pending || rec.Timestamp.HasValue)
                        {
                            hasActivity = true;
                            break;
                        }
                    }
                }

                if (!hasActivity)
                    return;

                if (Directory.GetFiles(Environment.CurrentDirectory, "Prueba_*.txt").Length > 0)
                    return;

                string deviceName = _session.SelectedDevice?.Name ?? "BT";
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    deviceName = deviceName.Replace(c, '_');
                }

                string fileName = $"Prueba_{deviceName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(Environment.CurrentDirectory, fileName);

                using var sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
                sw.WriteLine($"Dispositivo: {_session.SelectedDevice?.Name ?? "—"}");
                sw.WriteLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                sw.WriteLine("Resultado final: CERRADO_POR_OPERADOR");
                sw.WriteLine();
                foreach (var rec in _session.Records)
                {
                    string res = rec.Result == TestResult.Pass ? "PASS" : rec.Result == TestResult.Fail ? "FAIL" : "N/A";
                    sw.WriteLine($"{rec.Name}: {res}");
                }
            }
            catch
            {
                // Best-effort fallback; never block app shutdown.
            }
        }

        public void RefreshDateTime() =>
            labelDateTime.Text = DateTime.Now.ToString("dd/MMM/yyyy  HH:mm");

        public TestSession Session => _session;
    }
}