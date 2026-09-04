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

        public MainForm()
        {
            InitializeComponent();
            _session = new TestSession();
            stepManager = new TestStepManager(this);
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
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

            int totalSteps = stepManager.TotalTests;     // dinámico según perfil
            int totalReal = totalSteps - 1;             // sin contar preparación
            int currentIndex = _session.CurrentTestIndex;

            if (currentIndex == 0)
            {
                // Paso de preparación
                labelProgress.Text = "Preparación";
                progressBarMain.Value = 0;
            }
            else if (currentIndex >= totalSteps)
            {
                // Secuencia completa
                labelProgress.Text = "Completado";
                progressBarMain.Value = 100;
            }
            else
            {
                // Prueba activa: currentIndex 1..N → Prueba 1..N
                int displayIndex = currentIndex;
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
            base.OnFormClosed(e);
        }

        private void WriteFallbackReportIfMissing()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (Directory.GetFiles(baseDir, "Prueba_*.txt").Length > 0)
                    return;

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

                string displayName = _session.GetDisplayName();
                string deviceName = displayName;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    deviceName = deviceName.Replace(c, '_');
                }

                string fileName = $"Prueba_{deviceName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(baseDir, fileName);
                string currentDir = Environment.CurrentDirectory;

                string report = _session.BuildReportText();

                File.WriteAllText(filePath, report, System.Text.Encoding.UTF8);

                if (!string.Equals(baseDir, currentDir, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(filePath, Path.Combine(currentDir, fileName), true);
                }
            }
            catch
            {
                // Best-effort fallback, never block app shutdown.
            }
        }

        public void RefreshDateTime() =>
            labelDateTime.Text = DateTime.Now.ToString("dd/MMM/yyyy  HH:mm");

        public TestSession Session => _session;
    }
}