using System;
using System.Drawing;
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
            AppCommandRouter.Unregister();
            _globalHook?.Dispose();
            base.OnFormClosed(e);
        }

        public void RefreshDateTime() =>
            labelDateTime.Text = DateTime.Now.ToString("dd/MMM/yyyy  HH:mm");

        public TestSession Session => _session;
    }
}