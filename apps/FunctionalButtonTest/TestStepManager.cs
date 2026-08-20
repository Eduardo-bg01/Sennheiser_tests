using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    public class TestStepManager
    {
        // TotalTests ahora es una propiedad dinámica de instancia, no una constante.
        // Se actualiza en Initialize() según el perfil del dispositivo.
        public int TotalTests { get; private set; } = 7; // valor inicial conservador

        /// <summary>
        /// Acceso estático al TotalTests de la instancia activa.
        /// Usado por TestPanel para mostrar "PRUEBA X / Y" sin depender de una constante.
        /// </summary>
        public static int ActiveTotalTests { get; private set; } = 7;

        private readonly MainForm form;
        private TestPanel currentPanel;
        private List<Func<TestPanel>> _steps = new();

        public TestStepManager(MainForm form)
        {
            this.form = form;
        }

        public void Initialize()
        {
            form.Session.Reset();

            // Sincronizar DeviceAssets con el dispositivo seleccionado
            DeviceAssets.DeviceName = form.Session.SelectedDevice?.Name ?? string.Empty;

            // Obtener perfil según tipo de conexión
            DeviceProfile profile;
            var device = form.Session.SelectedDevice;

            if (device != null && device.IsWired)
            {
                // Jack 3.5 mm: el operador eligió el modelo en DeviceSelectForm
                profile = DeviceProfileRegistry.GetJackProfile(device.SelectedJackModel);
                // Mostrar el nombre comercial del modelo en los paneles
                DeviceAssets.DeviceName = device.SelectedJackModel ?? DeviceAssets.DeviceName;
            }
            else
            {
                // Bluetooth: usar el nombre detectado automáticamente
                profile = DeviceProfileRegistry.GetProfile(DeviceAssets.DeviceName);
            }

            form.Session.ApplyProfile(profile);

            // Construir lista de pasos dinámicamente según el perfil
            _steps = BuildSteps(profile);
            TotalTests = _steps.Count;
            ActiveTotalTests = TotalTests; // sincronizar acceso estático para los paneles

            // Re-registrar hotkeys
            AppCommandRouter.Unregister();
            AppCommandRouter.Register(form.Handle);
            ShowTest(0);
        }

        /// <summary>
        /// Construye la secuencia de paneles según las capacidades del perfil.
        /// El paso de preparación (HeadphonesOnPanel) siempre es el primero.
        /// </summary>
        private static List<Func<TestPanel>> BuildSteps(DeviceProfile profile)
        {
            var steps = new List<Func<TestPanel>>();

            steps.Add(() => new HeadphonesOnPanel());          // índice 0: siempre

            if (profile.HasBluetooth)
                steps.Add(() => new BluetoothConnectionPanel());

            if (profile.HasPlayPause)
                steps.Add(() => new PlayPausePanel());

            if (profile.HasPreviousTrack)
                steps.Add(() => new TrackPanel(4, "Canción Anterior ◀◀", "⏮", "previous.gif",
                    System.Windows.Forms.Keys.MediaPreviousTrack, "Pista Anterior"));

            if (profile.HasNextTrack)
                steps.Add(() => new TrackPanel(5, "Canción Siguiente ▶▶", "⏭", "next.gif",
                    System.Windows.Forms.Keys.MediaNextTrack, "Pista Siguiente"));

            if (profile.HasVolumeUp)
                steps.Add(() => new VolumePanel(6, "Subir Volumen (+)", "🔊", "volumeup.gif", isUp: true));

            if (profile.HasVolumeDown)
                steps.Add(() => new VolumePanel(7, "Bajar Volumen (−)", "🔉", "volumedown.gif", isUp: false));

            return steps;
        }

        public void ShowTest(int index)
        {
            form.Session.CurrentTestIndex = index;
            form.UpdateOperatorPanel();

            currentPanel?.Dispose();
            form.panelTestArea.Controls.Clear();

            if (index >= _steps.Count)
            {
                ShowSummary();
                return;
            }

            var panel = _steps[index]();

            // Wire auto-detection result
            panel.TestCompleted += (passed) => OnTestAutoCompleted(index, passed);

            currentPanel = panel;
            currentPanel.Dock = DockStyle.Fill;
            form.panelTestArea.Controls.Add(currentPanel);

            form.BtnPass.Visible = false;
            form.BtnFail.Visible = false;

            // Índice 0 = preparación (HeadphonesOnPanel), no tiene Record en la sesión
            if (index == 0)
                form.LabelStatus.Text = "Preparación — coloque los audífonos";
            else
                form.LabelStatus.Text = $"Prueba activa: {form.Session.Records[RecordIndex(index)].Name}";
        }

        private void OnTestAutoCompleted(int idx, bool passed)
        {
            if (idx != form.Session.CurrentTestIndex) return;

            // Índice 0 = preparación, no guarda resultado
            if (idx != 0)
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
        /// Convierte el índice de paso al índice del Record correspondiente
        /// dentro de los 6 Records fijos de la sesión.
        /// El paso 0 es preparación (sin record). Los pasos 1..N son las pruebas
        /// aplicables en orden; hay que saltar los NotApplicable para encontrar
        /// cuál Record le toca a ese paso.
        ///   Ej: perfil sin BT → Records[0]=NotApplicable, Records[1]=PlayPausa
        ///       paso 1 → Records[1] (primer aplicable)
        ///       paso 2 → Records[2] (segundo aplicable), etc.
        /// </summary>
        private int RecordIndex(int stepIndex)
        {
            // stepIndex 0 = preparación, no tiene record → no llamar con 0
            int applicableTarget = stepIndex; // queremos el Nth aplicable (1-based)
            int found = 0;
            var records = form.Session.Records;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].IsApplicable)
                {
                    found++;
                    if (found == applicableTarget)
                        return i;
                }
            }
            return stepIndex - 1; // fallback seguro
        }

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