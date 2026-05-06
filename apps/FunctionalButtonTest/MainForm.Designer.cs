using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Header
        private Panel panelHeader;
        private Label labelTitle;
        private Label labelDateTime;

        // Progress
        private Panel panelProgress;
        private Label labelProgress;
        private ProgressBar progressBarMain;

        // Main content
        private Panel panelContent;
        public Panel panelTestArea;

        // Footer
        private Panel panelFooter;
        private Button btnPass;
        private Button btnFail;
        private Label labelStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            if (disposing) _globalHook?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            // ── WINDOW STATE ─────────────────────────────────────
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.MinimumSize = new Size(800, 600);
            // Sin MaximumSize fijo => funciona en cualquier resolución

            // ── FORM ──────────────────────────────────────────────
            this.Text = "PRUEBA DE AUDÍFONOS BLUETOOTH";
            this.BackColor = ColorTranslator.FromHtml("#F4F7FC");
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10f);
            this.MinimumSize = new Size(1080, 700);
            this.Load += MainForm_Load;

            // ── HEADER ────────────────────────────────────────────
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = ColorTranslator.FromHtml("#E8EEF8"),
                Padding = new Padding(28, 0, 28, 0)
            };

            labelTitle = new Label
            {
                Text = "SISTEMA DE PRUEBA - AUDIFONOS BLUETOOTH",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0099BB"),
                AutoSize = false,
                Size = new Size(680, 44),
                Location = new Point(28, 14),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            labelDateTime = new Label
            {
                Text = DateTime.Now.ToString("dd/MMM/yyyy  HH:mm"),
                Font = new Font("Segoe UI", 10f),
                ForeColor = ColorTranslator.FromHtml("#5A6F90"),
                AutoSize = false,
                Size = new Size(230, 44),
                Location = new Point(panelHeader.Width - 250, 14),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            panelHeader.Resize += (s, e) =>
                labelDateTime.Location = new Point(panelHeader.Width - 250, 14);

            panelHeader.Controls.AddRange(new Control[] { labelTitle, labelDateTime });

            var clockTimer = new System.Windows.Forms.Timer(components) { Interval = 1000, Enabled = true };
            clockTimer.Tick += (s, e) => RefreshDateTime();

            // ── PROGRESS ──────────────────────────────────────────
            panelProgress = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = ColorTranslator.FromHtml("#EAF0FA"),
                Padding = new Padding(28, 10, 28, 10)
            };

            labelProgress = new Label
            {
                Text = "Prueba 1 de 6",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0099BB"),
                AutoSize = false,
                Size = new Size(180, 30),
                Location = new Point(28, 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            progressBarMain = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Size = new Size(400, 20),
                Location = new Point(220, 14),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            panelProgress.Resize += (s, e) =>
                progressBarMain.Size = new Size(
                    Math.Max(120, panelProgress.Width - 248), 20);

            panelProgress.Controls.AddRange(new Control[] { labelProgress, progressBarMain });

            // ── CONTENT ───────────────────────────────────────────
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTranslator.FromHtml("#F4F7FC"),
                Padding = new Padding(28, 22, 28, 22)
            };

            panelTestArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            panelContent.Controls.Add(panelTestArea);

            // ── FOOTER ────────────────────────────────────────────
            panelFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 88,
                BackColor = ColorTranslator.FromHtml("#E8EEF8"),
                Padding = new Padding(28, 14, 28, 14)
            };

            btnPass = new Button
            {
                Text = "✔  APROBADO",
                Size = new Size(176, 54),
                Location = new Point(28, 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#00A85A"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnPass.FlatAppearance.BorderSize = 0;
            btnPass.Visible = false;

            btnFail = new Button
            {
                Text = "✘  FALLIDO",
                Size = new Size(176, 54),
                Location = new Point(214, 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#CC2222"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnFail.FlatAppearance.BorderSize = 0;
            btnFail.Visible = false;

            labelStatus = new Label
            {
                Text = "Esperando inicio de prueba...",
                Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                ForeColor = ColorTranslator.FromHtml("#5A6F90"),
                AutoSize = false,
                Size = new Size(420, 54),
                Location = new Point(28, 16),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            panelFooter.Resize += (s, e) =>
                labelStatus.Size = new Size(
                    Math.Max(240, panelFooter.Width - 430), 54);

            panelFooter.Controls.AddRange(new Control[] { btnPass, btnFail, labelStatus });

            // ── ASSEMBLE ──────────────────────────────────────────
            this.Controls.Add(panelContent);
            this.Controls.Add(panelProgress);
            this.Controls.Add(panelHeader);
            this.Controls.Add(panelFooter);

            this.BtnPass = btnPass;
            this.BtnFail = btnFail;
            this.LabelStatus = labelStatus;

            this.ResumeLayout(false);
        }

        public Button BtnPass { get; private set; }
        public Button BtnFail { get; private set; }
        public Label LabelStatus { get; private set; }
    }
}