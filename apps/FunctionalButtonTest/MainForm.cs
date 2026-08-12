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

                var selected = _session.SelectedDevice;
                string displayName = selected != null && selected.IsWired && !string.IsNullOrWhiteSpace(selected.SelectedJackModel)
                    ? selected.SelectedJackModel
                    : selected?.Name ?? "BT";

                string deviceName = displayName;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    deviceName = deviceName.Replace(c, '_');
                }

                string fileName = $"Prueba_{deviceName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(baseDir, fileName);
                string currentDir = Environment.CurrentDirectory;

                var sb = new System.Text.StringBuilder();
                string sep = new string('─', 54);

                sb.AppendLine($"Dispositivo : {displayName}");
                sb.AppendLine($"MAC         : {_session.SelectedDevice?.Address ?? "—"}");
                sb.AppendLine($"Fecha       : {_session.StartTime:dd/MM/yyyy  HH:mm}");
                sb.AppendLine();
                sb.AppendLine(sep);
                sb.AppendLine($"  {"PRUEBA",-32} {"RESULTADO",-10} {"HORA"}");
                sb.AppendLine(sep);

                foreach (var rec in _session.Records)
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
                foreach (var r in _session.Records)
                {
                    if (r.Result == TestResult.NotApplicable) naCount++;
                    else totalApplicable++;
                }

                sb.AppendLine(sep);
                sb.AppendLine($"  Resultado final: {(_session.AllPassed ? "APROBADO" : "FALLIDO")}  ({CountPassed()}/{totalApplicable})  •  N/A: {naCount}");

                File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);

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

        private int CountPassed()
        {
            int count = 0;
            foreach (var rec in _session.Records)
            {
                if (rec.Result == TestResult.Pass)
                    count++;
            }

            return count;
        }

        public void RefreshDateTime() =>
            labelDateTime.Text = DateTime.Now.ToString("dd/MMM/yyyy  HH:mm");

        public TestSession Session => _session;
    }
}