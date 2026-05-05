using System;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    public class TestStepManager
    {
        public const int TotalTests = 7;

        private readonly MainForm form;
        private TestPanel currentPanel;

        public TestStepManager(MainForm form)
        {
            this.form = form;
        }

        public void Initialize()
        {
            form.Session.Reset();

            // Sincronizar DeviceAssets con el dispositivo seleccionado actualmente
            // (puede haber cambiado si el operador eligió uno diferente en "Nueva prueba")
            DeviceAssets.DeviceName = form.Session.SelectedDevice?.Name ?? string.Empty;

            // Re-register hotkeys in case they were lost (e.g. after another app grabbed them)
            AppCommandRouter.Unregister();
            AppCommandRouter.Register(form.Handle);
            ShowTest(0);
        }

        public void ShowTest(int index)
        {
            form.Session.CurrentTestIndex = index;
            form.UpdateOperatorPanel();

            currentPanel?.Dispose();
            form.panelTestArea.Controls.Clear();

            if (index >= TotalTests)
            {
                ShowSummary();
                return;
            }

            TestPanel panel;
            switch (index)
            {
                case 0: panel = new BluetoothConnectionPanel(); break;
                case 1: panel = new HeadphonesOnPanel(); break;
                case 2: panel = new PlayPausePanel(); break;
                case 3: panel = new PreviousTrackPanel(); break;
                case 4: panel = new NextTrackPanel(); break;
                case 5: panel = new VolumeUpPanel(); break;
                case 6: panel = new VolumeDownPanel(); break;
                default: return;
            }

            // Wire auto-detection result
            panel.TestCompleted += (passed) => OnTestAutoCompleted(index, passed);

            currentPanel = panel;
            currentPanel.Dock = DockStyle.Fill;
            form.panelTestArea.Controls.Add(currentPanel);

            form.BtnPass.Visible = false;
            form.BtnFail.Visible = false;

            // Índice 1 = preparación, no tiene Record en la sesión
            if (index == 1)
                form.LabelStatus.Text = "Preparación — coloque los audífonos";
            else
                form.LabelStatus.Text = $"Prueba activa: {form.Session.Records[RecordIndex(index)].Name}";
        }

        private void OnTestAutoCompleted(int idx, bool passed)
        {
            if (idx != form.Session.CurrentTestIndex) return;

            // Índice 1 = preparación, no guarda resultado
            if (idx != 1)
            {
                var rec = form.Session.Records[RecordIndex(idx)];
                rec.Result = passed ? TestResult.Pass : TestResult.Fail;
                rec.Timestamp = DateTime.Now;
            }

            form.LabelStatus.Text = passed ? "✔ Aprobado — continuando..." : "✘ Fallido — continuando...";

            var timer = new System.Windows.Forms.Timer { Interval = 800 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                ShowTest(idx + 1);
            };
            timer.Start();
        }

        /// <summary>
        /// Convierte el índice de paso (0-6) al índice de Records (0-5),
        /// saltando el paso de preparación en índice 1.
        /// idx 0 → record 0  (Conexión BT)
        /// idx 1 → preparación, no tiene record
        /// idx 2 → record 1  (Play/Pausa)
        /// idx 3 → record 2  (Anterior)
        /// idx 4 → record 3  (Siguiente)
        /// idx 5 → record 4  (Volumen +)
        /// idx 6 → record 5  (Volumen -)
        /// </summary>
        private static int RecordIndex(int stepIndex)
            => stepIndex > 1 ? stepIndex - 1 : stepIndex;

        private void ShowSummary()
        {
            form.BtnPass.Visible = false;
            form.BtnFail.Visible = false;
            form.LabelStatus.Text = "Secuencia completa.";

            var summaryPanel = new SummaryPanel(form.Session);
            summaryPanel.OnRestart += (device) =>
            {
                form.Session.SelectedDevice = device;
                Initialize();
            };
            summaryPanel.Dock = DockStyle.Fill;
            form.panelTestArea.Controls.Add(summaryPanel);
        }
    }
}