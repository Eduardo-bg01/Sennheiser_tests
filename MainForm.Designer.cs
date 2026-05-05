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
            this.Load += MainForm_Load;

            // ── HEADER ────────────────────────────────────────────
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = ColorTranslator.FromHtml("#E8EEF8"),
                Padding = new Padding(20, 0, 20, 0)
            };

            labelTitle = new Label
            {
                Text = "⬡  SISTEMA DE PRUEBA — AUDÍFONOS BLUETOOTH",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0099BB"),
                AutoSize = false,
                Size = new Size(580, 40),
                Location = new Point(20, 12),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            labelDateTime = new Label
            {
                Text = DateTime.Now.ToString("dd/MMM/yyyy  HH:mm"),
                Font = new Font("Segoe UI", 9f),
                ForeColor = ColorTranslator.FromHtml("#5A6F90"),
                AutoSize = false,
                Size = new Size(200, 40),
                Location = new Point(panelHeader.Width - 220, 12),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            panelHeader.Resize += (s, e) =>
                labelDateTime.Location = new Point(panelHeader.Width - 220, 12);

            panelHeader.Controls.AddRange(new Control[] { labelTitle, labelDateTime });

            var clockTimer = new System.Windows.Forms.Timer(components) { Interval = 1000, Enabled = true };
            clockTimer.Tick += (s, e) => RefreshDateTime();

            // ── PROGRESS ──────────────────────────────────────────
            panelProgress = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = ColorTranslator.FromHtml("#EAF0FA"),
                Padding = new Padding(20, 8, 20, 8)
            };

            labelProgress = new Label
            {
                Text = "Prueba 1 de 6",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0099BB"),
                AutoSize = false,
                Size = new Size(160, 28),
                Location = new Point(20, 8),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            progressBarMain = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Size = new Size(400, 18),
                Location = new Point(190, 13),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            panelProgress.Resize += (s, e) =>
                progressBarMain.Size = new Size(
                    Math.Max(100, panelProgress.Width - 210), 18);

            panelProgress.Controls.AddRange(new Control[] { labelProgress, progressBarMain });

            // ── CONTENT ───────────────────────────────────────────
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTranslator.FromHtml("#F4F7FC"),
                Padding = new Padding(20)
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
                Height = 72,
                BackColor = ColorTranslator.FromHtml("#E8EEF8"),
                Padding = new Padding(20, 10, 20, 10)
            };

            btnPass = new Button
            {
                Text = "✔  APROBADO",
                Size = new Size(160, 46),
                Location = new Point(20, 13),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#00A85A"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnPass.FlatAppearance.BorderSize = 0;
            btnPass.Visible = false;

            btnFail = new Button
            {
                Text = "✘  FALLIDO",
                Size = new Size(160, 46),
                Location = new Point(196, 13),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#CC2222"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnFail.FlatAppearance.BorderSize = 0;
            btnFail.Visible = false;

            labelStatus = new Label
            {
                Text = "Esperando inicio de prueba...",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = ColorTranslator.FromHtml("#5A6F90"),
                AutoSize = false,
                Size = new Size(400, 46),
                Location = new Point(20, 13),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            panelFooter.Resize += (s, e) =>
                labelStatus.Size = new Size(
                    Math.Max(200, panelFooter.Width - 420), 46);

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