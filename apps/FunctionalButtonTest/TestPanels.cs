using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace BluetoothHeadphoneTest
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  BASE PANEL
    // ═══════════════════════════════════════════════════════════════════════════
    public abstract class TestPanel : Panel
    {
        protected static readonly Color BgDark = ColorTranslator.FromHtml("#F4F7FC");
        protected static readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
        protected static readonly Color AccentCyan = ColorTranslator.FromHtml("#0099BB");
        protected static readonly Color AccentYellow = ColorTranslator.FromHtml("#D4A000");
        protected static readonly Color AccentGreen = ColorTranslator.FromHtml("#00A85A");
        protected static readonly Color AccentRed = ColorTranslator.FromHtml("#CC2222");
        protected static readonly Color AccentOrange = ColorTranslator.FromHtml("#D46800");
        protected static readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");
        protected static readonly Color TextMuted = ColorTranslator.FromHtml("#5A6F90");
        protected static readonly Color BorderColor = ColorTranslator.FromHtml("#C8D4E8");

        public event Action<bool> TestCompleted;
        protected void FireTestCompleted(bool passed) => TestCompleted?.Invoke(passed);

        protected Panel card;
        protected Label labelTestNumber;
        protected Label labelTestName;
        protected Label labelIcon;
        protected Panel stepsPanel;
        protected Label labelStatusIndicator;
        protected Panel statusBar;

        protected MiniPlayerWidget Player;

        protected TestPanel(int number, string name, string icon, bool withPlayer = false)
        {
            BackColor = BgDark;
            Padding = new Padding(16);

            card = new Panel { BackColor = BgCard, Dock = DockStyle.Fill };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // ── Etiqueta de número de prueba (esquina superior izquierda) ───
            labelTestNumber = new Label
            {
                Text = number > 1
                    ? $"PRUEBA {number - 1} / {TestStepManager.ActiveTotalTests - 1}"
                    : $"PRUEBA {number} / {TestStepManager.ActiveTotalTests - 1}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = AccentCyan,
                BackColor = ColorTranslator.FromHtml("#E0F4FA"),
                AutoSize = false,
                Size = new Size(160, 28),
                Location = new Point(16, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            // ── Nombre del dispositivo (esquina superior derecha) ────────────
            var labelDevice = new Label
            {
                Text = string.IsNullOrWhiteSpace(DeviceAssets.DeviceName)
                    ? "🎧  (sin dispositivo)"
                    : $"🎧  {DeviceAssets.DeviceName}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#1A2640"),
                BackColor = ColorTranslator.FromHtml("#E0F4FA"),
                AutoSize = false,
                Size = new Size(260, 28),
                Location = new Point(Math.Max(180, card.Width - 280), 14),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            card.Controls.Add(labelDevice);
            card.Resize += (s, e) =>
                labelDevice.Location = new Point(Math.Max(180, card.Width - 280), 14);

            // ── Ícono ────────────────────────────────────────────────────────
            labelIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 34f),
                ForeColor = AccentYellow,
                AutoSize = false,
                Size = new Size(72, 68),
                Location = new Point(16, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            // ── Nombre de la prueba ──────────────────────────────────────────
            labelTestName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = false,
                Size = new Size(600, 46),
                Location = new Point(96, 58),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Resize += (s, e) =>
                labelTestName.Size = new Size(Math.Max(200, card.Width - 380), 46);

            // ── Barra de estado (Dock=Bottom) ────────────────────────────────
            statusBar = new Panel
            { BackColor = ColorTranslator.FromHtml("#EAF0FA"), Height = 48, Dock = DockStyle.Bottom };
            labelStatusIndicator = new Label
            {
                Text = "⏳  Esperando acción del audífono...",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AccentYellow,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            statusBar.Controls.Add(labelStatusIndicator);

            // ── Panel de pasos (columna izquierda ~55% del card) ─────────────
            stepsPanel = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#EAF0FA"),
                Location = new Point(16, 128),
                Size = new Size(400, 200),          // tamaño inicial; se ajusta en Resize
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            card.Controls.AddRange(new Control[]
                { labelTestNumber, labelIcon, labelTestName, stepsPanel, statusBar });

            if (withPlayer)
            {
                Player = new MiniPlayerWidget
                {
                    Location = new Point(16, 128 + 200 + 8),
                    Size = new Size(400, 110),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left
                };
                card.Controls.Add(Player);

                card.Resize += (s, e) =>
                {
                    int colW = (int)(card.Width * 0.54) - 8;
                    stepsPanel.Size = new Size(colW, 160);
                    Player.Size = new Size(colW, 110);
                    Player.Location = new Point(16, 128 + stepsPanel.Height + 8);
                };
            }
            else
            {
                card.Resize += (s, e) =>
                {
                    int colW = (int)(card.Width * 0.54) - 8;
                    int colH = Math.Max(120, card.Height - 220);
                    stepsPanel.Size = new Size(colW, colH);
                };
            }

            Controls.Add(card);
        }

        protected void SetStatus(string text, Color color)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(text, color))); return; }
            labelStatusIndicator.Text = text;
            labelStatusIndicator.ForeColor = color;
        }

        protected void AutoPass(string message)
        {
            SetStatus("✔  " + message, AccentGreen);
            var t = new System.Windows.Forms.Timer { Interval = 1400 };
            t.Tick += (s, e) => { t.Stop(); FireTestCompleted(true); };
            t.Start();
        }

        protected void AutoFail(string message)
        {
            SetStatus("✘  " + message, AccentRed);
            var t = new System.Windows.Forms.Timer { Interval = 1500 };
            t.Tick += (s, e) => { t.Stop(); FireTestCompleted(false); };
            t.Start();
        }

        protected Label MakeStep(int num, string text, int yOffset)
        {
            var badge = new Label
            {
                Text = num.ToString(),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = BgDark,
                BackColor = AccentCyan,
                AutoSize = false,
                Size = new Size(30, 30),
                Location = new Point(16, yOffset + 2),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            badge.Paint += (s, e) =>
            {
                using var path = new GraphicsPath();
                path.AddEllipse(0, 0, ((Label)s).Width - 1, ((Label)s).Height - 1);
                ((Label)s).Region = new Region(path);
            };
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = TextPrimary,
                AutoSize = false,
                Size = new Size(stepsPanel.Width - 60, 36),
                Location = new Point(52, yOffset),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            stepsPanel.Controls.Add(badge);
            stepsPanel.Controls.Add(lbl);
            stepsPanel.Resize += (s, e) => lbl.Size = new Size(stepsPanel.Width - 60, 36);
            return lbl;
        }

        protected Label MakeInfo(string text, int y)
        {
            var lbl = new Label
            {
                Text = "ℹ  " + text,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AccentCyan,
                BackColor = ColorTranslator.FromHtml("#E0F4FA"),
                AutoSize = false,
                Size = new Size(stepsPanel.Width - 32, 30),
                Location = new Point(16, y),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            stepsPanel.Controls.Add(lbl);
            stepsPanel.Resize += (s, e) => lbl.Size = new Size(stepsPanel.Width - 32, 30);
            return lbl;
        }

        protected Label MakeWarning(string text, int y)
        {
            var lbl = new Label
            {
                Text = "⚠  " + text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = AccentYellow,
                BackColor = ColorTranslator.FromHtml("#FFF8E0"),
                AutoSize = false,
                Size = new Size(stepsPanel.Width - 32, 30),
                Location = new Point(16, y),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            stepsPanel.Controls.Add(lbl);
            stepsPanel.Resize += (s, e) => lbl.Size = new Size(stepsPanel.Width - 32, 30);
            return lbl;
        }

        /// <summary>
        /// GIF animado en la columna derecha del card (~45% del ancho).
        /// Se reposiciona y redimensiona automáticamente con el card.
        /// </summary>
        protected PictureBox MakeAnimatedGif(string gifFileName)
        {
            // Posición y tamaño inicial; se ajusta en card.Resize
            int gifX = (int)(card.Width * 0.56) + 8;
            int gifW = Math.Max(80, card.Width - gifX - 16);
            int gifH = Math.Max(80, card.Height - 220);

            var pic = new PictureBox
            {
                Size = new Size(gifW, gifH),
                Location = new Point(gifX, 128),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = ColorTranslator.FromHtml("#EAF0FA"),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
            };

            try
            {
                var img = DeviceAssets.LoadGif(gifFileName);
                if (img != null)
                {
                    pic.Image = img;

                    // El callback del ImageAnimator llega desde un hilo secundario.
                    // UpdateFrames y el Invalidate DEBEN ejecutarse en el hilo UI
                    // para evitar "Object is currently in use elsewhere".
                    ImageAnimator.Animate(img, (s, e) =>
                    {
                        if (pic.IsDisposed) return;
                        try
                        {
                            if (pic.InvokeRequired)
                                pic.BeginInvoke(new Action(() =>
                                {
                                    if (pic.IsDisposed) return;
                                    ImageAnimator.UpdateFrames(img);
                                    pic.Invalidate();
                                }));
                            else
                            {
                                ImageAnimator.UpdateFrames(img);
                                pic.Invalidate();
                            }
                        }
                        catch { /* control destruido justo en este momento */ }
                    });

                    pic.Disposed += (s, e) =>
                    {
                        ImageAnimator.StopAnimate(img, null);
                        img.Dispose();
                    };
                }
            }
            catch { }

            pic.Paint += (s, e) =>
            {
                if (pic.Image != null) return;
                using var pen = new Pen(BorderColor, 1);
                using var font = new Font("Segoe UI", 9f, FontStyle.Italic);
                using var brush = new SolidBrush(TextMuted);
                e.Graphics.DrawRectangle(pen, 0, 0, pic.Width - 1, pic.Height - 1);
                e.Graphics.DrawString("[ GIF ]", font, brush,
                    new RectangleF(0, 0, pic.Width, pic.Height),
                    new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
            };

            card.Controls.Add(pic);
            card.Resize += (s, e) =>
            {
                int x = (int)(card.Width * 0.56) + 8;
                int w = Math.Max(80, card.Width - x - 16);
                int h = Math.Max(80, card.Height - 220);
                pic.Location = new Point(x, 128);
                pic.Size = new Size(w, h);
            };
            return pic;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  MINI PLAYER WIDGET
    // ═══════════════════════════════════════════════════════════════════════════
    public class MiniPlayerWidget : Panel
    {
        public AudioPlayer Audio { get; private set; }

        private Label _lblTrack;
        private Label _lblState;
        private Label _lblLastCmd;
        private Panel _vizBar;
        private System.Windows.Forms.Timer _vizTimer;

        private static readonly Color Bg = ColorTranslator.FromHtml("#F4F7FC");
        private static readonly Color Cyan = ColorTranslator.FromHtml("#0099BB");
        private static readonly Color Green = ColorTranslator.FromHtml("#00A85A");
        private static readonly Color Yellow = ColorTranslator.FromHtml("#D4A000");
        private static readonly Color Muted = ColorTranslator.FromHtml("#5A6F90");
        private static readonly Color Orange = ColorTranslator.FromHtml("#D46800");

        private int _vizPhase = 0;

        public MiniPlayerWidget()
        {
            BackColor = Bg;
            BorderStyle = BorderStyle.None;
            Padding = new Padding(8);

            _lblState = new Label
            {
                Text = "▶  REPRODUCIENDO",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Green,
                AutoSize = false,
                Size = new Size(200, 26),
                Location = new Point(8, 6),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblTrack = new Label
            {
                Text = "♪  Pista 1 — Do (261 Hz)",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Cyan,
                AutoSize = false,
                Size = new Size(300, 26),
                Location = new Point(216, 6),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _lblLastCmd = new Label
            {
                Text = "Último comando: (ninguno)",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Muted,
                AutoSize = false,
                Size = new Size(Width - 16, 22),
                Location = new Point(8, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _vizBar = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#EAF0FA"),
                Location = new Point(8, 62),
                Size = new Size(Width - 16, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _vizBar.Paint += DrawViz;

            Controls.AddRange(new Control[] { _lblState, _lblTrack, _lblLastCmd, _vizBar });
            Resize += (s, e) =>
            {
                _lblTrack.Size = new Size(Width - 230, 26);
                _lblLastCmd.Size = new Size(Width - 16, 22);
                _vizBar.Size = new Size(Width - 16, 26);
            };

            Audio = new AudioPlayer();
            Audio.StateChanged += OnStateChanged;
            Audio.TrackChanged += OnTrackChanged;
            AppCommandRouter.OnMediaKey += OnCommand;
            AppCommandRouter.ActivePlayer = Audio;
            Audio.Play();

            _vizTimer = new System.Windows.Forms.Timer { Interval = 80 };
            _vizTimer.Tick += (s, e) =>
            {
                if (Audio.State == AudioPlayer.PlayerState.Playing)
                { _vizPhase++; _vizBar.Invalidate(); }
            };
            _vizTimer.Start();
        }

        private void OnStateChanged(AudioPlayer.PlayerState state)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnStateChanged(state))); return; }
            switch (state)
            {
                case AudioPlayer.PlayerState.Playing:
                    _lblState.Text = "▶  REPRODUCIENDO"; _lblState.ForeColor = Green; break;
                case AudioPlayer.PlayerState.Paused:
                    _lblState.Text = "⏸  PAUSADO"; _lblState.ForeColor = Orange; break;
                case AudioPlayer.PlayerState.Stopped:
                    _lblState.Text = "⏹  DETENIDO"; _lblState.ForeColor = Muted; break;
            }
            _vizBar.Invalidate();
        }

        private void OnTrackChanged(int track)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnTrackChanged(track))); return; }
            _lblTrack.Text = $"♪  {Audio.TrackName}";
        }

        private void OnCommand(System.Windows.Forms.Keys key)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnCommand(key))); return; }
            string name = key switch
            {
                System.Windows.Forms.Keys.MediaPlayPause => "Play / Pausa",
                System.Windows.Forms.Keys.MediaNextTrack => "Pista Siguiente ▶▶",
                System.Windows.Forms.Keys.MediaPreviousTrack => "Pista Anterior ◀◀",
                System.Windows.Forms.Keys.VolumeUp => "Subir Volumen +",
                System.Windows.Forms.Keys.VolumeDown => "Bajar Volumen −",
                _ => key.ToString()
            };
            _lblLastCmd.Text = $"Último comando detectado: {name}";
            _lblLastCmd.ForeColor = Cyan;
        }

        private void DrawViz(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var g = e.Graphics;
            var panel = (Panel)sender;
            g.Clear(ColorTranslator.FromHtml("#EAF0FA"));

            if (Audio.State != AudioPlayer.PlayerState.Playing)
            {
                using var b = new SolidBrush(ColorTranslator.FromHtml("#C8D4E8"));
                g.FillRectangle(b, 2, panel.Height / 2 - 1, panel.Width - 4, 2);
                return;
            }

            int bars = 40;
            float barW = (float)(panel.Width - 4) / bars;
            for (int i = 0; i < bars; i++)
            {
                double h = (Math.Sin((_vizPhase * 0.18) + i * 0.45) * 0.5 + 0.5)
                         * (Math.Sin((_vizPhase * 0.07) + i * 0.9) * 0.3 + 0.7)
                         * (panel.Height - 4);
                float x = 2 + i * barW;
                float y = (float)(panel.Height - 2 - h);
                using var b = new SolidBrush(Color.FromArgb(
                    0,
                    (int)Math.Min(255, 100 + h * 2),
                    (int)Math.Min(255, 150 + h)));
                g.FillRectangle(b, x, y, barW - 1, (float)h);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AppCommandRouter.OnMediaKey -= OnCommand;
                if (AppCommandRouter.ActivePlayer == Audio)
                    AppCommandRouter.ActivePlayer = null;
                _vizTimer?.Dispose();
                Audio?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PREPARACIÓN — COLOCARSE LOS AUDÍFONOS
    // ═══════════════════════════════════════════════════════════════════════════
    public class HeadphonesOnPanel : TestPanel
    {
        public HeadphonesOnPanel() : base(2, "Coloque los audífonos", "🎧")
        {
            labelTestNumber.Text = "PREPARACIÓN";
            labelTestNumber.BackColor = ColorTranslator.FromHtml("#FFF8E0");
            labelTestNumber.ForeColor = AccentYellow;
            stepsPanel.Visible = false;

            // ── Texto principal ──────────────────────────────────────
            var lblMain = new Label
            {
                Text = "Coloque los audífonos\nen las orejas",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = false,
                Size = new Size(400, 180),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.None
            };

            // ── Botón Continuar ──────────────────────────────────────
            var btnReady = new Button
            {
                Text = "🎧  Listo — Continuar",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = AccentGreen,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(260, 52),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.None
            };
            btnReady.FlatAppearance.BorderSize = 0;
            btnReady.Click += (s, e) => FireTestCompleted(true);

            // ── Imagen del dispositivo ───────────────────────────────
            var picBox = new PictureBox
            {
                Size = new Size(500, 340),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = ColorTranslator.FromHtml("#EAF0FA"),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.None
            };

            picBox.Paint += (s, e) =>
            {
                if (picBox.Image != null) return;
                using var pen = new Pen(BorderColor, 1);
                using var font = new Font("Segoe UI", 9f, FontStyle.Italic);
                using var brush = new SolidBrush(TextMuted);
                e.Graphics.DrawRectangle(pen, 0, 0, picBox.Width - 1, picBox.Height - 1);
                e.Graphics.DrawString("[ Imagen ]", font, brush,
                    new RectangleF(0, 0, picBox.Width, picBox.Height),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            };

            try
            {
                var img = DeviceAssets.LoadDeviceImage();
                if (img != null) picBox.Image = img;
            }
            catch { }

            card.Controls.AddRange(new Control[] { lblMain, btnReady, picBox });

            // ── Posicionamiento relativo — se recalcula en cada Resize ───
            Action relayout = () =>
            {
                int w = card.Width;
                int h = card.Height;
                int cy = 128;                        // inicio del área de contenido

                // Columna izquierda (~45% del ancho)
                int leftW = (int)(w * 0.44);
                int textH = 180;
                int textY = cy + (h - cy - 48 - 52 - textH) / 2;
                textY = Math.Max(cy + 16, textY);

                lblMain.Location = new Point(24, textY);
                lblMain.Size = new Size(leftW - 32, textH);

                btnReady.Location = new Point(24, textY + textH + 16);
                btnReady.Size = new Size(Math.Min(260, leftW - 32), 52);

                // Columna derecha — imagen
                int imgX = leftW + 16;
                int imgW = Math.Max(80, w - imgX - 16);
                int imgH = Math.Max(80, h - cy - 48 - 16);   // 48 = statusBar
                picBox.Location = new Point(imgX, cy);
                picBox.Size = new Size(imgW, imgH);
            };

            card.Resize += (s, e) => relayout();
            // Ejecutar al primer layout
            this.HandleCreated += (s, e) => relayout();
            relayout();

            SetStatus("⏳  Coloque los audífonos y presione Continuar...", AccentYellow);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  1. BLUETOOTH CONNECTION
    // ═══════════════════════════════════════════════════════════════════════════
    public class BluetoothConnectionPanel : TestPanel
    {
        private System.Windows.Forms.Timer _pollTimer;
        private int _attempts = 0;
        private const int MAX_ATTEMPTS = 30;
        private string _targetMac;
        private Label _lblDeviceName;

        public BluetoothConnectionPanel() : base(1, "Conexión Bluetooth", "🔵")
        {
            MakeStep(1, "Encienda los audífonos — el LED debe parpadear en AZUL.", 14);
            MakeStep(2, "Asegúrese de que el audífono ya esté PAREADO con este equipo.", 56);
            MakeStep(3, "Active el Bluetooth en el audífono para que se conecte.", 98);
            MakeStep(4, "El sistema detectará la conexión automáticamente.", 140);

            _lblDeviceName = new Label
            {
                Text = "Dispositivo objetivo: (cargando...)",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentCyan,
                BackColor = ColorTranslator.FromHtml("#E0F4FA"),
                AutoSize = false,
                Size = new Size(stepsPanel.Width - 10, 28),
                Location = new Point(16, 188),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            stepsPanel.Controls.Add(_lblDeviceName);
            stepsPanel.Resize += (s, e) =>
                _lblDeviceName.Size = new Size(stepsPanel.Width - 10, 28);

            MakeInfo("Tiempo máximo de espera: 30 segundos.", 224);
            SetStatus("⏳  Buscando dispositivo Bluetooth conectado...", AccentYellow);

            _pollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _pollTimer.Tick += PollBluetooth;
            _pollTimer.Start();
        }

        private void PollBluetooth(object sender, EventArgs e)
        {
            _attempts++;
            if (_targetMac == null)
            {
                var form = FindForm() as MainForm;
                var dev = form?.Session?.SelectedDevice;
                _targetMac = dev?.Address ?? "";
                if (_lblDeviceName != null && dev != null)
                    _lblDeviceName.Text = $"Dispositivo objetivo: {dev.Name}  ({dev.Address})";
            }

            bool connected = string.IsNullOrEmpty(_targetMac)
                ? BluetoothDetector.GetPairedDevices().Count > 0
                : BluetoothDetector.IsDeviceConnected(_targetMac);

            if (connected)
            {
                _pollTimer.Stop();
                AutoPass("Dispositivo Bluetooth conectado correctamente");
                return;
            }
            SetStatus($"⏳  Esperando conexión... ({_attempts}/{MAX_ATTEMPTS})", AccentYellow);
            if (_attempts >= MAX_ATTEMPTS)
            {
                _pollTimer.Stop();
                AutoFail("El dispositivo no se conectó en el tiempo límite");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _pollTimer?.Dispose();
            base.Dispose(disposing);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  2. PLAY / PAUSE
    // ═══════════════════════════════════════════════════════════════════════════
    public class PlayPausePanel : TestPanel
    {
        private System.Windows.Forms.Timer _timeout;
        private bool _pauseDetected = false;
        private bool _resumeDetected = false;
        private bool _done = false;

        public PlayPausePanel() : base(3, "Play / Pausa", "⏯", withPlayer: true)
        {
            MakeStep(1, "El reproductor está activo. Presione Play/Pausa en el audífono.", 14);
            MakeStep(2, "El audio debe PAUSAR — el reproductor lo mostrará.", 56);
            MakeStep(3, "Presione nuevamente — el audio debe REANUDAR.", 98);
            MakeAnimatedGif("playpause.gif");
            SetStatus("⏳  Presione Play/Pausa en el audífono para pausar el audio...", AccentYellow);

            Player.Audio.StateChanged += OnPlayerStateChanged;

            _timeout = new System.Windows.Forms.Timer { Interval = 25000 };
            _timeout.Tick += (s, e) =>
            {
                _timeout.Stop();
                if (!_done) AutoFail("No se detectó respuesta de Play/Pausa en el tiempo límite");
            };
            _timeout.Start();
        }

        private void OnPlayerStateChanged(AudioPlayer.PlayerState state)
        {
            if (_done) return;
            if (InvokeRequired) { Invoke(new Action(() => OnPlayerStateChanged(state))); return; }

            if (!_pauseDetected && state == AudioPlayer.PlayerState.Paused)
            {
                _pauseDetected = true;
                SetStatus("✔  Pausa detectada — presione nuevamente para reanudar...", AccentOrange);
                return;
            }
            if (_pauseDetected && !_resumeDetected && state == AudioPlayer.PlayerState.Playing)
            {
                _resumeDetected = true;
                _done = true;
                _timeout.Stop();
                AutoPass("Play y Pausa detectados correctamente");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _done = true;
                if (Player?.Audio != null)
                    Player.Audio.StateChanged -= OnPlayerStateChanged;
                _timeout?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  3. PREVIOUS TRACK
    // ═══════════════════════════════════════════════════════════════════════════
    public class PreviousTrackPanel : TestPanel
    {
        private System.Windows.Forms.Timer _timeout;
        private bool _done = false;

        public PreviousTrackPanel() : base(4, "Canción Anterior ◀◀", "⏮", withPlayer: true)
        {
            MakeStep(1, "El reproductor está activo.", 14);
            MakeStep(2, "Presione el botón de Pista Anterior en el audífono.", 56);
            MakeStep(3, "El reproductor mostrará el cambio de pista automáticamente.", 98);
            MakeAnimatedGif("previous.gif");
            SetStatus("⏳  Esperando comando de Pista Anterior...", AccentYellow);

            Player.Audio.NextTrack();
            AppCommandRouter.OnMediaKey += OnMediaKey;

            _timeout = new System.Windows.Forms.Timer { Interval = 20000 };
            _timeout.Tick += (s, e) =>
            {
                _timeout.Stop();
                if (!_done) AutoFail("No se detectó comando de Pista Anterior");
            };
            _timeout.Start();
        }

        private void OnMediaKey(Keys key)
        {
            if (_done) return;
            if (key != Keys.MediaPreviousTrack) return;
            if (InvokeRequired) { Invoke(new Action(() => OnMediaKey(key))); return; }

            _done = true;
            _timeout.Stop();
            AppCommandRouter.OnMediaKey -= OnMediaKey;
            AutoPass($"Pista Anterior detectada — cambió a {Player.Audio.TrackName}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _done = true; AppCommandRouter.OnMediaKey -= OnMediaKey; _timeout?.Dispose(); }
            base.Dispose(disposing);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  4. NEXT TRACK
    // ═══════════════════════════════════════════════════════════════════════════
    public class NextTrackPanel : TestPanel
    {
        private System.Windows.Forms.Timer _timeout;
        private bool _done = false;

        public NextTrackPanel() : base(5, "Canción Siguiente ▶▶", "⏭", withPlayer: true)
        {
            MakeStep(1, "El reproductor está activo.", 14);
            MakeStep(2, "Presione el botón de Pista Siguiente en el audífono.", 56);
            MakeStep(3, "El reproductor mostrará el cambio de pista automáticamente.", 98);
            MakeAnimatedGif("next.gif");
            SetStatus("⏳  Esperando comando de Pista Siguiente...", AccentYellow);

            AppCommandRouter.OnMediaKey += OnMediaKey;

            _timeout = new System.Windows.Forms.Timer { Interval = 20000 };
            _timeout.Tick += (s, e) =>
            {
                _timeout.Stop();
                if (!_done) AutoFail("No se detectó comando de Pista Siguiente");
            };
            _timeout.Start();
        }

        private void OnMediaKey(Keys key)
        {
            if (_done) return;
            if (key != Keys.MediaNextTrack) return;
            if (InvokeRequired) { Invoke(new Action(() => OnMediaKey(key))); return; }

            _done = true;
            _timeout.Stop();
            AppCommandRouter.OnMediaKey -= OnMediaKey;
            AutoPass($"Pista Siguiente detectada — cambió a {Player.Audio.TrackName}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _done = true; AppCommandRouter.OnMediaKey -= OnMediaKey; _timeout?.Dispose(); }
            base.Dispose(disposing);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  5. VOLUME UP
    // ═══════════════════════════════════════════════════════════════════════════
    public class VolumeUpPanel : TestPanel
    {
        private VolumeMonitor _monitor;
        private System.Windows.Forms.Timer _timeout;
        private ProgressBar _volBar;
        private Label _volLabel;
        private Label _lblTarget;
        private float _startVolume;
        private float _targetVolume;
        private const float REQUIRED_DELTA = 20f;
        private bool _completed;

        public VolumeUpPanel() : base(6, "Subir Volumen (+)", "🔊", withPlayer: true)
        {
            MakeStep(1, "El sistema registra el volumen inicial automáticamente.", 14);
            MakeStep(2, "Presione y mantenga el botón (+) del audífono.", 56);
            MakeStep(3, "Suba el volumen al menos 20 puntos por encima del nivel inicial.", 98);
            MakeAnimatedGif("volumeup.gif");

            var lblFreq = new Label
            {
                Text = "♪  Frecuencias de prueba: Do 261 Hz · Mi 330 Hz · Sol 392 Hz",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = AccentCyan,
                AutoSize = false,
                Size = new Size(stepsPanel.Width - 32, 24),
                Location = new Point(16, 138),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            stepsPanel.Controls.Add(lblFreq);
            stepsPanel.Resize += (s, e) => lblFreq.Size = new Size(stepsPanel.Width - 32, 24);

            // ── Controles de volumen (posición relativa al card) ─────
            _lblTarget = new Label
            {
                Text = "Registrando nivel inicial...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentYellow,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var lblBarTitle = new Label
            {
                Text = "NIVEL ACTUAL:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(130, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _volBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _volLabel = new Label
            {
                Text = "50%",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AccentYellow,
                AutoSize = false,
                Size = new Size(70, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            card.Controls.AddRange(new Control[] { _lblTarget, lblBarTitle, _volBar, _volLabel });

            // Posicionamiento relativo de los controles de volumen
            card.Resize += (s, e) => RelayoutVolumeControls(lblBarTitle);
            RelayoutVolumeControls(lblBarTitle);

            _monitor = new VolumeMonitor();
            _startVolume = _monitor.CurrentVolume;
            _targetVolume = Math.Min(_startVolume + REQUIRED_DELTA, 100f);
            _lblTarget.Text = $"Nivel inicial: {_startVolume:F0}%   →   Meta mínima: {_targetVolume:F0}%";
            UpdateBar(_startVolume);
            SetStatus($"⏳  Suba el volumen hasta {_targetVolume:F0}% o más...", AccentYellow);

            _monitor.VolumeChanged += OnVolumeChanged;

            _timeout = new System.Windows.Forms.Timer { Interval = 25000 };
            _timeout.Tick += (s, e) =>
            {
                _timeout.Stop();
                if (!_completed) AutoFail("No se alcanzó el nivel de volumen requerido");
            };
            _timeout.Start();
        }

        private void RelayoutVolumeControls(Label lblBarTitle)
        {
            // Zona de volumen: debajo del reproductor (~55% ancho, alineado con stepsPanel)
            int colW = (int)(card.Width * 0.54) - 8;
            int baseY = 128 + 160 + 110 + 24;   // stepsTop + stepsH + playerH + gap
            baseY = Math.Min(baseY, card.Height - 200);

            _lblTarget.Location = new Point(16, baseY);
            _lblTarget.Size = new Size(colW - 16, 28);

            lblBarTitle.Location = new Point(16, baseY + 36);

            _volBar.Location = new Point(16, baseY + 64);
            _volBar.Size = new Size(Math.Max(80, colW - 90), 24);

            _volLabel.Location = new Point(16 + Math.Max(80, colW - 90) + 4, baseY + 64);
            _volLabel.Size = new Size(70, 24);
        }

        private void OnVolumeChanged(float vol)
        {
            if (_completed) return;
            if (InvokeRequired) { Invoke(new Action(() => OnVolumeChanged(vol))); return; }
            UpdateBar(vol);
            SetStatus($"⏳  Volumen: {vol:F0}%  →  Meta: {_targetVolume:F0}%", AccentOrange);
            if (vol >= _targetVolume)
            {
                _completed = true;
                _timeout.Stop();
                AutoPass($"Volumen subido correctamente a {vol:F0}%");
            }
        }

        private void UpdateBar(float vol)
        {
            _volBar.Value = (int)Math.Max(0, Math.Min(100, vol));
            _volLabel.Text = $"{vol:F0}%";
            _volLabel.ForeColor = vol >= _targetVolume ? AccentGreen : AccentYellow;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _monitor?.Dispose(); _timeout?.Dispose(); }
            base.Dispose(disposing);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  6. VOLUME DOWN
    // ═══════════════════════════════════════════════════════════════════════════
    public class VolumeDownPanel : TestPanel
    {
        private VolumeMonitor _monitor;
        private System.Windows.Forms.Timer _timeout;
        private ProgressBar _volBar;
        private Label _volLabel;
        private Label _lblTarget;
        private float _startVolume;
        private float _targetVolume;
        private const float REQUIRED_DELTA = 20f;
        private bool _completed;

        public VolumeDownPanel() : base(7, "Bajar Volumen (−)", "🔉", withPlayer: true)
        {
            MakeStep(1, "El sistema registra el volumen inicial automáticamente.", 14);
            MakeStep(2, "Presione y mantenga el botón (−) del audífono.", 56);
            MakeStep(3, "Baje el volumen al menos 20 puntos por debajo del nivel inicial.", 98);
            MakeAnimatedGif("volumedown.gif");

            var lblFreq = new Label
            {
                Text = "♪  Frecuencias de prueba: Do 261 Hz · Mi 330 Hz · Sol 392 Hz",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = AccentCyan,
                AutoSize = false,
                Size = new Size(stepsPanel.Width - 32, 24),
                Location = new Point(16, 138),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            stepsPanel.Controls.Add(lblFreq);
            stepsPanel.Resize += (s, e) => lblFreq.Size = new Size(stepsPanel.Width - 32, 24);

            _lblTarget = new Label
            {
                Text = "Registrando nivel inicial...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentYellow,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var lblBarTitle = new Label
            {
                Text = "NIVEL ACTUAL:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(130, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _volBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _volLabel = new Label
            {
                Text = "50%",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AccentYellow,
                AutoSize = false,
                Size = new Size(70, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            card.Controls.AddRange(new Control[] { _lblTarget, lblBarTitle, _volBar, _volLabel });

            card.Resize += (s, e) => RelayoutVolumeControls(lblBarTitle);
            RelayoutVolumeControls(lblBarTitle);

            _monitor = new VolumeMonitor();
            _startVolume = _monitor.CurrentVolume;
            _targetVolume = Math.Max(_startVolume - REQUIRED_DELTA, 0f);
            _lblTarget.Text = $"Nivel inicial: {_startVolume:F0}%   →   Meta máxima: {_targetVolume:F0}%";
            UpdateBar(_startVolume);
            SetStatus($"⏳  Baje el volumen hasta {_targetVolume:F0}% o menos...", AccentYellow);

            _monitor.VolumeChanged += OnVolumeChanged;

            _timeout = new System.Windows.Forms.Timer { Interval = 25000 };
            _timeout.Tick += (s, e) =>
            {
                _timeout.Stop();
                if (!_completed) AutoFail("No se alcanzó el nivel de volumen requerido");
            };
            _timeout.Start();
        }

        private void RelayoutVolumeControls(Label lblBarTitle)
        {
            int colW = (int)(card.Width * 0.54) - 8;
            int baseY = 128 + 160 + 110 + 24;
            baseY = Math.Min(baseY, card.Height - 200);

            _lblTarget.Location = new Point(16, baseY);
            _lblTarget.Size = new Size(colW - 16, 28);

            lblBarTitle.Location = new Point(16, baseY + 36);

            _volBar.Location = new Point(16, baseY + 64);
            _volBar.Size = new Size(Math.Max(80, colW - 90), 24);

            _volLabel.Location = new Point(16 + Math.Max(80, colW - 90) + 4, baseY + 64);
            _volLabel.Size = new Size(70, 24);
        }

        private void OnVolumeChanged(float vol)
        {
            if (_completed) return;
            if (InvokeRequired) { Invoke(new Action(() => OnVolumeChanged(vol))); return; }
            UpdateBar(vol);
            SetStatus($"⏳  Volumen: {vol:F0}%  →  Meta: {_targetVolume:F0}%", AccentOrange);
            if (vol <= _targetVolume)
            {
                _completed = true;
                _timeout.Stop();
                AutoPass($"Volumen bajado correctamente a {vol:F0}%");
            }
        }

        private void UpdateBar(float vol)
        {
            _volBar.Value = (int)Math.Max(0, Math.Min(100, vol));
            _volLabel.Text = $"{vol:F0}%";
            _volLabel.ForeColor = vol <= _targetVolume ? AccentGreen : AccentYellow;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _monitor?.Dispose(); _timeout?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}