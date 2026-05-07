using MicroTestCloud;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace MicroTestCloud
{
    public partial class Form1 : Form
    {
        // ══════════════════════════════════════════════════════════════
        //  MOTOR DE AUDIO
        // ══════════════════════════════════════════════════════════════

        private WaveInEvent waveIn;                         // Captura audio del micrófono
        private WaveOutEvent waveOut;                       // Reproduce audio en tiempo real (playback)
        private BufferedWaveProvider bufferedWaveProvider;  // Buffer intermedio para el playback en tiempo real
        private bool isListening = false;                   // Indica si se está grabando actualmente
        private float currentVolume = 0f;                   // Nivel de volumen actual (0.0 - 1.0)

        // ══════════════════════════════════════════════════════════════
        //  GRABACIÓN PARA REPRODUCCIÓN POSTERIOR
        // ══════════════════════════════════════════════════════════════

        private MemoryStream _recordedStream;   // Stream en memoria donde se guarda el audio grabado
        private WaveFileWriter _waveWriter;     // Escribe los datos PCM en formato WAV al stream
        private WaveOutEvent _playbackOut;      // Reproductor para escuchar el audio grabado después
        private bool _isPlayingBack = false;    // Indica si se está reproduciendo el audio grabado

        // ══════════════════════════════════════════════════════════════
        //  MODO BOCINA
        // ══════════════════════════════════════════════════════════════

        private bool _modoBocina = false;           // True = modo bocina (pista de audio), False = voz del operador
        private WaveOutEvent _speakerTone;          // Reproductor de la pista de audio en modo bocina
        private SignalGenerator _toneGenerator;     // Generador de señal (reservado para tonos sintéticos)

        // ══════════════════════════════════════════════════════════════
        //  LOG DE RESULTADOS
        // ══════════════════════════════════════════════════════════════

        private List<(DateTime Time, int Volume, string Level)> _logEntries = new(); // Historial de muestras tomadas durante el test
        private DateTime _testStartTime;   // Hora en que comenzó el test
        private string _deviceName = "";   // Nombre del micrófono seleccionado
        private int _secondCounter = 0;    // Contador de ticks para tomar muestras cada ~1 segundo
        private DateTime _lastLogTime;     // Marca de tiempo de la última muestra registrada

        // ══════════════════════════════════════════════════════════════
        //  RESULTADO DEL TEST
        // ══════════════════════════════════════════════════════════════

        private string _testResult = "No definido"; // Resultado final: "PASÓ" | "FALLÓ" | "No definido"
        private bool _reportGenerated = false;

        // ══════════════════════════════════════════════════════════════
        //  PALETA DE COLORES — LIGHT MODE
        // ══════════════════════════════════════════════════════════════

        private readonly Color BgDark = ColorTranslator.FromHtml("#F4F7FC");
        private readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
        private readonly Color AccentCyan = ColorTranslator.FromHtml("#0099BB");
        private readonly Color AccentYellow = ColorTranslator.FromHtml("#D4A000");
        private readonly Color AccentGreen = ColorTranslator.FromHtml("#00A85A");
        private readonly Color AccentRed = ColorTranslator.FromHtml("#CC2222");
        private readonly Color AccentOrange = ColorTranslator.FromHtml("#D46800");
        private readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");
        private readonly Color TextMuted = ColorTranslator.FromHtml("#5A6F90");
        private readonly Color BorderColor = ColorTranslator.FromHtml("#C8D4E8");

        // ══════════════════════════════════════════════════════════════
        //  CONTROLES DE LA INTERFAZ
        // ══════════════════════════════════════════════════════════════

        private ComboBox cmbDevices;
        private Button btnStart;
        private Button btnStop;
        private Button btnPlayback;
        private Button btnStopPlayback;
        private CustomProgressBar progressVolume;
        private Label lblVolumeText;
        private Label lblVolumePct;
        private Panel panelIndicator;
        private Label lblStatus;
        private Label lblStatusIcon;
        private CheckBox chkPlayback;
        private RadioButton rbVoz;
        private RadioButton rbBocina;
        private System.Windows.Forms.Timer timerUI;

        private AudioFileReader _audioFile;

        private ComboBox cmbOutputDevices;

        private Panel panelSummary;
        private Label lblSummary;
        private Button btnSaveReport;
        private Button btnCancelReport;

        private Button _btnPaso;
        private Button _btnFallo;

        // ══════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            BuildUI();
            LoadMicrophones();
            LoadOutputDevices();
        }

        private void Form1_Load(object sender, EventArgs e) { }

        // ══════════════════════════════════════════════════════════════
        //  CONSTRUCCIÓN DE LA INTERFAZ
        // ══════════════════════════════════════════════════════════════

        private void BuildUI()
        {
            this.Text = "MicroTest · Audio Diagnostics";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgDark;
            this.ForeColor = TextPrimary;
            this.AutoScroll = true;
            this.MinimumSize = new Size(1200, 760);

            int SW = Screen.PrimaryScreen.Bounds.Width;
            int SH = Screen.PrimaryScreen.Bounds.Height;
            int colW = Math.Min(940, SW - 96);
            int colX = (SW - colW) / 2;
            int titleH = 58;

            // ── Barra de título ────────────────────────────────────────
            var titleBar = new Panel
            {
                Size = new Size(SW, titleH),
                Location = new Point(0, 0),
                BackColor = BgCard
            };

            var dot = new Panel
            {
                Size = new Size(10, 10),
                Location = new Point(colX, 21),
                BackColor = AccentCyan
            };
            dot.Region = RoundRegion(10, 10, 5);

            var lblAppTitle = new Label
            {
                Text = "MICROTEST  ·  AUDIO DIAGNOSTICS",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = AccentCyan,
                Location = new Point(colX + 20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f),
                Size = new Size(52, titleH),
                Location = new Point(SW - 52, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextMuted,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = AccentRed;
            btnClose.Click += (s, e) => Application.Exit();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = TextMuted;

            var btnMin = new Button
            {
                Text = "─",
                Font = new Font("Segoe UI", 11f),
                Size = new Size(52, titleH),
                Location = new Point(SW - 104, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextMuted,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#DDE5F0");
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnMin.MouseEnter += (s, e) => btnMin.ForeColor = TextPrimary;
            btnMin.MouseLeave += (s, e) => btnMin.ForeColor = TextMuted;

            titleBar.Controls.AddRange(new Control[] { dot, lblAppTitle, btnMin, btnClose });
            titleBar.MouseDown += DragForm;
            lblAppTitle.MouseDown += DragForm;
            this.Controls.Add(titleBar);

            // ── Layout ────────────────────────────────────────────────
            int y = titleH + 16;
            int gap = 10;
            int cardPad = 28;

            // ── Tarjeta: Nivel de señal ────────────────────────────────
            int cardVolH = 120;
            var cardVolume = MakeCard(colX, y, colW, cardVolH);

            var lblVolLabel = MakeLabel("NIVEL DE SEÑAL", cardPad, 14, 0, TextMuted, true);
            lblVolLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

            int barW2 = colW - cardPad * 2;
            progressVolume = new CustomProgressBar
            {
                Location = new Point(cardPad, 38),
                Size = new Size(barW2, 22),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                BarColor = AccentCyan,
                BgColor = ColorTranslator.FromHtml("#DDE5F0"),
                CornerRadius = 11
            };

            string[] scaleMarks = { "0", "25", "50", "75", "100" };
            for (int i = 0; i < scaleMarks.Length; i++)
            {
                int xPos = cardPad + (int)(i * barW2 / 4.0) - (i == 4 ? 20 : 0);
                var lbl = MakeLabel(scaleMarks[i], xPos, 66, 0, TextMuted);
                lbl.Font = new Font("Segoe UI", 7.5f);
                cardVolume.Controls.Add(lbl);
            }

            lblVolumePct = MakeLabel("0%", cardPad, 86, 0, AccentCyan);
            lblVolumePct.Font = new Font("Segoe UI", 28, FontStyle.Bold);
            lblVolumePct.TextAlign = ContentAlignment.MiddleLeft;
            lblVolumePct.Size = new Size(160, 44);

            lblVolumeText = MakeLabel("sin señal", cardPad + 165, 100, 0, TextMuted);
            lblVolumeText.Font = new Font("Segoe UI", 10.5f, FontStyle.Italic);

            cardVolume.Controls.AddRange(new Control[] { lblVolLabel, progressVolume, lblVolumePct, lblVolumeText });
            this.Controls.Add(cardVolume);
            y += cardVolH + gap;

            // ── Tarjeta: Estado del test ───────────────────────────────
            int cardStatH = 120;
            var cardStatus = MakeCard(colX, y, colW, cardStatH);

            var lblStateLabel = MakeLabel("ESTADO DEL TEST", cardPad, 14, 0, TextMuted, true);
            lblStateLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

            panelIndicator = new Panel
            {
                Location = new Point(cardPad, 38),
                Size = new Size(48, 48),
                BackColor = Color.Transparent
            };
            panelIndicator.Paint += PanelIndicator_Paint;

            lblStatusIcon = MakeLabel("◉", cardPad, 38, 0, TextMuted);
            lblStatusIcon.Font = new Font("Segoe UI", 21f);
            lblStatusIcon.Size = new Size(48, 48);
            lblStatusIcon.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusIcon.BackColor = Color.Transparent;

            lblStatus = MakeLabel("Inactivo — selecciona un micrófono y presiona Iniciar", cardPad + 62, 42, 0, TextMuted);
            lblStatus.Font = new Font("Segoe UI", 10.5f);
            lblStatus.Size = new Size(colW - cardPad - 70, 48);

            cardStatus.Controls.AddRange(new Control[] { lblStateLabel, panelIndicator, lblStatusIcon, lblStatus });
            this.Controls.Add(cardStatus);
            y += cardStatH + gap;

            // ── Botones: Iniciar / Detener ─────────────────────────────
            int btnH = 54;
            int btnW = (colW - gap) / 2;

            btnStart = MakeButton("INICIAR TEST", colX, y, btnW, btnH, AccentCyan, BgDark);
            btnStart.Click += BtnStart_Click;

            btnStop = MakeButton("DETENER", colX + btnW + gap, y, btnW, btnH, AccentRed, BgDark);
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;

            this.Controls.AddRange(new Control[] { btnStart, btnStop });
            y += btnH + (gap + 20);

            // ── Sección: Revisar Prueba ────────────────────────────────
            int cardRevisarH = 46;
            var cardRevisar = MakeCard(colX, y, colW, cardRevisarH);

            var lblRevisarTitle = new Label
            {
                Text = "🔍  REVISAR PRUEBA",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = AccentCyan,
                Location = new Point(cardPad, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            cardRevisar.Controls.Add(lblRevisarTitle);
            this.Controls.Add(cardRevisar);
            y += cardRevisarH + gap;

            // ── Botones: Reproducir / Parar ────────────────────────────
            int btn2H = 48;

            btnPlayback = MakeButton("REPRODUCIR AUDIO", colX, y, btnW, btn2H, AccentGreen, BgDark);
            btnPlayback.Enabled = false;
            btnPlayback.Click += BtnPlayback_Click;

            btnStopPlayback = MakeButton("PARAR", colX + btnW + gap, y, btnW, btn2H, AccentOrange, BgDark);
            btnStopPlayback.Enabled = false;
            btnStopPlayback.Click += BtnStopPlayback_Click;

            this.Controls.AddRange(new Control[] { btnPlayback, btnStopPlayback });

            this.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };

            timerUI = new System.Windows.Forms.Timer { Interval = 40 };
            timerUI.Tick += TimerUI_Tick;

            y += btn2H + gap;

            // ── Botón: Siguiente prueba ────────────────────────────────
            var btnSiguiente = MakeButton("PASAR A LA SIGUIENTE PRUEBA", colX, y, colW, 54, AccentCyan, BgDark);
            btnSiguiente.Click += (s, e) => Application.Exit();
            this.Controls.Add(btnSiguiente);
            y += 48 + (gap + 20);

            // ── Sección: Configuración ─────────────────────────────────
            int cardConfigH = 46;
            var cardConfig = MakeCard(colX, y, colW, cardConfigH);

            var lblConfigTitle = new Label
            {
                Text = "⚙  CONFIGURACIÓN",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = AccentCyan,
                Location = new Point(cardPad, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            cardConfig.Controls.Add(lblConfigTitle);
            this.Controls.Add(cardConfig);
            y += cardConfigH + gap;

            // ── Tarjeta: Dispositivos ──────────────────────────────────
            int cardDevH = 160;
            var cardDevice = MakeCard(colX, y, colW, cardDevH);

            var lblDevLabel = MakeLabel("DISPOSITIVO DE ENTRADA", cardPad, 14, 0, TextMuted, true);
            lblDevLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

            cmbDevices = new ComboBox
            {
                Location = new Point(cardPad, 36),
                Size = new Size(colW - cardPad * 2, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5f),
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 26
            };
            cmbDevices.DrawItem += CmbDevices_DrawItem;

            var lblOutLabel = MakeLabel("DISPOSITIVO DE SALIDA", cardPad, 80, 0, TextMuted, true);
            lblOutLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

            cmbOutputDevices = new ComboBox
            {
                Location = new Point(cardPad, 102),
                Size = new Size(colW - cardPad * 2, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5f),
                FlatStyle = FlatStyle.Flat
            };

            cardDevice.Controls.AddRange(new Control[] { lblDevLabel, cmbDevices, lblOutLabel, cmbOutputDevices });
            this.Controls.Add(cardDevice);
            y += cardDevH + gap;

            // ── Tarjeta: Modo de prueba ────────────────────────────────
            int cardModoH = 56;
            var cardModo = MakeCard(colX, y, colW, cardModoH);

            var lblModoLabel = MakeLabel("MODO DE PRUEBA", cardPad, 14, 0, TextMuted, true);
            lblModoLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

            rbVoz = new RadioButton
            {
                Text = "  Voz del operador",
                Font = new Font("Segoe UI", 10.5f),
                Location = new Point(cardPad, 36),
                Size = new Size(220, 26),
                ForeColor = AccentCyan,
                BackColor = Color.Transparent,
                Checked = true,
                Cursor = Cursors.Hand
            };

            rbBocina = new RadioButton
            {
                Text = "  Bocina  (Pista)",
                Font = new Font("Segoe UI", 10.5f),
                Location = new Point(cardPad + 240, 36),
                Size = new Size(240, 26),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                Checked = false,
                Cursor = Cursors.Hand
            };

            rbVoz.CheckedChanged += (s, e) =>
            {
                rbVoz.ForeColor = rbVoz.Checked ? AccentCyan : TextPrimary;
                rbBocina.ForeColor = rbBocina.Checked ? AccentCyan : TextMuted;
                if (rbVoz.Checked) _modoBocina = false;
            };

            rbBocina.CheckedChanged += (s, e) =>
            {
                rbBocina.ForeColor = rbBocina.Checked ? AccentCyan : TextMuted;
                rbVoz.ForeColor = rbVoz.Checked ? AccentCyan : TextPrimary;
                if (rbBocina.Checked) _modoBocina = true;
            };

            cardModo.Controls.AddRange(new Control[] { lblModoLabel, rbVoz, rbBocina });
            this.Controls.Add(cardModo);
            y += cardModoH + gap;

            // ── Tarjeta: Playback en tiempo real ──────────────────────
            int cardChkH = 58;
            var cardPlaybackCheck = MakeCard(colX, y, colW, cardChkH);

            chkPlayback = new CheckBox
            {
                Text = "  Escuchar en tiempo real  (playback)",
                Font = new Font("Segoe UI", 10.5f),
                Location = new Point(cardPad, 16),
                Size = new Size(380, 28),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            chkPlayback.CheckedChanged += (s, e) =>
                chkPlayback.ForeColor = chkPlayback.Checked ? AccentCyan : TextMuted;

            cardPlaybackCheck.Controls.Add(chkPlayback);
            this.Controls.Add(cardPlaybackCheck);
            y += cardChkH + (gap + 80);
        }

        // ══════════════════════════════════════════════════════════════
        //  HELPERS DE INTERFAZ
        // ══════════════════════════════════════════════════════════════

        private void DragForm(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private Panel MakeCard(int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = BgCard
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            return card;
        }

        private Label MakeLabel(string text, int x, int y, int pad, Color color, bool upper = false)
        {
            return new Label
            {
                Text = upper ? text.ToUpper() : text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = color,
                BackColor = Color.Transparent,
                Padding = new Padding(pad, 0, 0, 0)
            };
        }

        private Button MakeButton(string text, int x, int y, int w, int h, Color accent, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = BgCard,
                ForeColor = accent,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btn.FlatAppearance.BorderColor = accent;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, accent);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, accent);
            return btn;
        }

        private Region RoundRegion(int w, int h, int r)
        {
            var path = new GraphicsPath();
            path.AddEllipse(0, 0, w, h);
            return new Region(path);
        }

        private void PanelIndicator_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var color = (panelIndicator.Tag is Color c) ? c : TextMuted;

            using var glowBrush = new SolidBrush(Color.FromArgb(40, color));
            e.Graphics.FillEllipse(glowBrush, 2, 2, 44, 44);

            using var brush = new SolidBrush(color);
            e.Graphics.FillEllipse(brush, 8, 8, 32, 32);

            using var highlight = new SolidBrush(Color.FromArgb(80, Color.White));
            e.Graphics.FillEllipse(highlight, 13, 11, 12, 10);
        }

        private void CmbDevices_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.Graphics.FillRectangle(new SolidBrush(
                (e.State & DrawItemState.Selected) != 0
                    ? ColorTranslator.FromHtml("#D0DFF5")
                    : Color.White),
                e.Bounds);

            e.Graphics.DrawString(
                cmbDevices.Items[e.Index].ToString(),
                new Font("Segoe UI", 10f),
                new SolidBrush(TextPrimary),
                e.Bounds.X + 8, e.Bounds.Y + 3);
        }

        // ══════════════════════════════════════════════════════════════
        //  CARGA DE DISPOSITIVOS
        // ══════════════════════════════════════════════════════════════

        private void LoadMicrophones()
        {
            cmbDevices.Items.Clear();
            int count = WaveIn.DeviceCount;

            if (count == 0)
            {
                cmbDevices.Items.Add("No se encontraron micrófonos");
                cmbDevices.SelectedIndex = 0;
                btnStart.Enabled = false;
                return;
            }

            for (int i = 0; i < count; i++)
                cmbDevices.Items.Add(WaveIn.GetCapabilities(i).ProductName);

            cmbDevices.SelectedIndex = 0;
        }

        private void LoadOutputDevices()
        {
            cmbOutputDevices.Items.Clear();
            int count = WaveOut.DeviceCount;

            if (count == 0)
            {
                cmbOutputDevices.Items.Add("No hay dispositivos de salida");
                cmbOutputDevices.SelectedIndex = 0;
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                cmbOutputDevices.Items.Add(caps.ProductName);
            }

            cmbOutputDevices.SelectedIndex = 0;
        }

        // ══════════════════════════════════════════════════════════════
        //  CONTROL DEL TEST
        // ══════════════════════════════════════════════════════════════

        private void BtnStart_Click(object sender, EventArgs e)
        {
            _logEntries.Clear();
            _secondCounter = 0;
            _testStartTime = DateTime.Now;
            _lastLogTime = DateTime.Now;
            _deviceName = cmbDevices.SelectedItem?.ToString() ?? "Desconocido";

            _recordedStream?.Dispose();
            _recordedStream = new MemoryStream();
            _waveWriter = null;

            btnPlayback.Enabled = false;
            btnStopPlayback.Enabled = false;

            waveIn = new WaveInEvent
            {
                DeviceNumber = cmbDevices.SelectedIndex,
                WaveFormat = new WaveFormat(44100, 1),
                BufferMilliseconds = 40
            };
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;

            _waveWriter = new WaveFileWriter(new IgnoreDisposeStream(_recordedStream), waveIn.WaveFormat);

            if (chkPlayback.Checked)
            {
                bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat)
                { DiscardOnBufferOverflow = true };
                waveOut = new WaveOutEvent();
                waveOut.Init(bufferedWaveProvider);
                waveOut.Play();
            }

            waveIn.StartRecording();
            isListening = true;
            timerUI.Start();

            // ── Modo bocina ────────────────────────────────────────────
            if (_modoBocina)
            {
                string rutaBase = AppDomain.CurrentDomain.BaseDirectory;
                string rutaAudio = Path.Combine(rutaBase, "PistaAudio", "Prueba.mp3");

                try
                {
                    if (File.Exists(rutaAudio))
                    {
                        _speakerTone?.Stop();
                        _speakerTone?.Dispose();
                        _audioFile?.Dispose();

                        _audioFile = new AudioFileReader(rutaAudio) { Volume = 0.5f };

                        _speakerTone = new WaveOutEvent
                        {
                            DeviceNumber = cmbOutputDevices.SelectedIndex
                        };
                        _speakerTone.Init(_audioFile);
                        _speakerTone.Play();

                        SetStatus("▶ Reproduciendo audio de prueba — grabando micrófono...", AccentCyan);
                    }
                    else
                    {
                        SetStatus($"⚠ No se encontró el archivo:\n{rutaAudio}", Color.Red);
                    }
                }
                catch (Exception ex)
                {
                    SetStatus($"❌ Error al reproducir audio:\n{ex.Message}", Color.Red);
                }
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            cmbDevices.Enabled = false;
            chkPlayback.Enabled = false;
            rbVoz.Enabled = false;
            rbBocina.Enabled = false;

            panelIndicator.Tag = AccentYellow;
            panelIndicator.Invalidate();
            lblStatusIcon.ForeColor = AccentYellow;

            if (_modoBocina)
                SetStatus("▶  Reproduciendo pista — grabando micrófono...", AccentCyan);
            else
                SetStatus("Grabando...  habla cerca del micrófono.", AccentYellow);
        }

        private void BtnStop_Click(object sender, EventArgs e) => waveIn?.StopRecording();

        // ══════════════════════════════════════════════════════════════
        //  EVENTOS DE AUDIO
        // ══════════════════════════════════════════════════════════════

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            float sum = 0;
            int count = e.BytesRecorded / 2;

            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                float norm = sample / 32768f;
                sum += norm * norm;
            }

            currentVolume = (float)Math.Sqrt(sum / count);

            // Captura local para evitar condición de carrera al hacer Dispose
            var writer = _waveWriter;
            writer?.Write(e.Buffer, 0, e.BytesRecorded);

            if (chkPlayback.Checked && bufferedWaveProvider != null)
                bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            this.Invoke((Action)(() =>
            {
                timerUI.Stop();

                waveOut?.Stop();
                waveOut?.Dispose();
                waveOut = null;

                _speakerTone?.Stop();
                _speakerTone?.Dispose();
                _speakerTone = null;
                _toneGenerator = null;

                _waveWriter?.Flush();
                _waveWriter?.Dispose();
                _waveWriter = null;

                waveIn?.Dispose();
                waveIn = null;
                isListening = false;

                if (_recordedStream != null && _recordedStream.Length > 0)
                    btnPlayback.Enabled = true;

                progressVolume.Value = 0;
                progressVolume.BarColor = AccentCyan;
                lblVolumePct.Text = "0%";
                lblVolumePct.ForeColor = AccentCyan;
                lblVolumeText.Text = "sin señal";

                panelIndicator.Tag = TextMuted;
                panelIndicator.Invalidate();
                lblStatusIcon.ForeColor = TextMuted;

                btnStart.Enabled = true;
                btnStop.Enabled = false;
                cmbDevices.Enabled = true;
                chkPlayback.Enabled = true;
                rbVoz.Enabled = true;
                rbBocina.Enabled = true;

                SetStatus("Test detenido — puedes reproducir el audio grabado.", AccentGreen);
            }));
        }

        // ══════════════════════════════════════════════════════════════
        //  TIMER DE UI
        // ══════════════════════════════════════════════════════════════

        private void TimerUI_Tick(object sender, EventArgs e)
        {
            // Factor 300 para amplificar el RMS (señales de voz típicamente bajas)
            const float VolumeScale = 300f;
            int vol = (int)Math.Min(currentVolume * VolumeScale, 100);
            progressVolume.Value = vol;
            lblVolumePct.Text = $"{vol}%";

            Color barColor, indicatorColor;
            string statusText, levelText;

            if (vol == 0)
            {
                barColor = AccentRed; indicatorColor = AccentRed;
                statusText = "Sin señal detectada."; levelText = "sin señal";
            }
            else if (vol < 15)
            {
                barColor = AccentOrange; indicatorColor = AccentOrange;
                statusText = "Señal muy baja — acércate más al micrófono."; levelText = "muy baja";
            }
            else if (vol < 40)
            {
                barColor = AccentYellow; indicatorColor = AccentYellow;
                statusText = "Señal baja — intenta hablar más fuerte."; levelText = "baja";
            }
            else if (vol < 85)
            {
                barColor = AccentGreen; indicatorColor = AccentGreen;
                statusText = "✓  Señal óptima."; levelText = "óptima";
            }
            else
            {
                barColor = AccentRed; indicatorColor = AccentRed;
                statusText = "⚠  Saturación — aleja el micrófono."; levelText = "saturada";
            }

            progressVolume.BarColor = barColor;
            progressVolume.Invalidate();
            lblVolumePct.ForeColor = barColor;
            lblVolumeText.Text = levelText;
            lblVolumeText.ForeColor = barColor;
            panelIndicator.Tag = indicatorColor;
            panelIndicator.Invalidate();
            lblStatusIcon.ForeColor = indicatorColor;

            if (!_modoBocina)
                SetStatus(statusText, indicatorColor);

            // Registrar muestra cada segundo usando tiempo real (más preciso que contar ticks)
            if ((DateTime.Now - _lastLogTime).TotalSeconds >= 1)
            {
                _lastLogTime = DateTime.Now;
                _logEntries.Add((DateTime.Now, vol, levelText));
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  REPRODUCCIÓN DEL AUDIO GRABADO
        // ══════════════════════════════════════════════════════════════

        private void BtnPlayback_Click(object sender, EventArgs e)
        {
            if (_recordedStream == null || _recordedStream.Length == 0) return;

            try
            {
                _recordedStream.Position = 0;
                var reader = new WaveFileReader(_recordedStream);

                _playbackOut = new WaveOutEvent
                {
                    DeviceNumber = cmbOutputDevices.SelectedIndex
                };
                _playbackOut.Init(reader);

                _playbackOut.PlaybackStopped += (s, ev) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        _isPlayingBack = false;
                        btnPlayback.Enabled = true;
                        btnStopPlayback.Enabled = false;
                        btnStart.Enabled = true;
                        SetStatus("Reproducción finalizada.", AccentGreen);
                        MostrarFormResultado();
                    }));
                };

                _playbackOut.Play();
                _isPlayingBack = true;
                btnPlayback.Enabled = false;
                btnStopPlayback.Enabled = true;
                btnStart.Enabled = false;
                SetStatus("▶  Reproduciendo audio grabado...", AccentGreen);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al reproducir:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStopPlayback_Click(object sender, EventArgs e)
        {
            _playbackOut?.Stop();
            _playbackOut?.Dispose();
            _playbackOut = null;
            _isPlayingBack = false;
            btnPlayback.Enabled = true;
            btnStopPlayback.Enabled = false;
            btnStart.Enabled = true;
            SetStatus("Reproducción detenida.", TextMuted);
            MostrarFormResultado();
        }

        private void MostrarFormResultado()
        {
            var formResultado = new FormResultado(
                _deviceName,
                _testStartTime,
                _logEntries,
                _modoBocina,
                _recordedStream);   // ← se pasa el stream para guardar el WAV

            formResultado.ShowDialog(this);
            _testResult = formResultado.TestResult;
        }

        // ══════════════════════════════════════════════════════════════
        //  UTILIDADES
        // ══════════════════════════════════════════════════════════════

        private void SetStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        }

        /// <summary>
        /// Intenta obtener el número de serie del micrófono via WMI.
        /// </summary>
        private string GetMicrophoneSerial(string productName)
        {
            try
            {
                var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'AudioEndpoint' OR PNPClass = 'Media'");

                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    if (name.IndexOf(productName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string deviceId = obj["DeviceID"]?.ToString() ?? "";
                        var parts = deviceId.Split('\\');
                        if (parts.Length >= 3)
                            return parts[2];
                    }
                }
            }
            catch { }

            return "No disponible";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            WriteFallbackReportIfMissing();
            if (isListening) waveIn?.StopRecording();
            _speakerTone?.Stop();
            _speakerTone?.Dispose();
            _audioFile?.Dispose();      // ← liberado correctamente al cerrar
            _playbackOut?.Stop();
            _playbackOut?.Dispose();
            _recordedStream?.Dispose();
            base.OnFormClosing(e);
        }

        private void WriteFallbackReportIfMissing()
        {
            try
            {
                if (_reportGenerated)
                    return;

                bool hasActivity = _logEntries != null && _logEntries.Count > 0;
                if (!hasActivity && string.IsNullOrWhiteSpace(_deviceName))
                    return;

                if (Directory.GetFiles(Environment.CurrentDirectory, "MicroTest_*.txt").Length > 0)
                {
                    _reportGenerated = true;
                    return;
                }

                string baseName = $"MicroTest_{DateTime.Now:yyyyMMdd_HHmmss}";
                string txtPath = Path.Combine(Environment.CurrentDirectory, baseName + ".txt");
                string result = _testResult == "PASS" || _testResult == "FAIL" ? _testResult : "FAIL";

                using (var sw = new StreamWriter(txtPath, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("MICROTEST · REPORTE DE RESCATE");
                    sw.WriteLine($"Resultado : {result}");
                    sw.WriteLine($"Fecha y hora : {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    sw.WriteLine($"Micrófono : {_deviceName}");
                    sw.WriteLine($"Muestras : {_logEntries?.Count ?? 0}");
                }

                _reportGenerated = true;
            }
            catch
            {
                // Best-effort fallback, never block close.
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  BARRA DE PROGRESO PERSONALIZADA
    // ══════════════════════════════════════════════════════════════════

    public class CustomProgressBar : Control
    {
        private int _minimum = 0, _maximum = 100, _value = 0;
        private Color _barColor = ColorTranslator.FromHtml("#00D4FF");
        private Color _bgColor = ColorTranslator.FromHtml("#DDE5F0");
        private int _cornerRadius = 11;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Minimum { get => _minimum; set { _minimum = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Maximum { get => _maximum; set { _maximum = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value { get => _value; set { _value = Math.Max(_minimum, Math.Min(_maximum, value)); Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BarColor { get => _barColor; set { _barColor = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BgColor { get => _bgColor; set { _bgColor = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get => _cornerRadius; set { _cornerRadius = value; Invalidate(); } }

        public CustomProgressBar() { DoubleBuffered = true; }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int r = CornerRadius;

            using var bgBrush = new SolidBrush(BgColor);
            FillRoundRect(e.Graphics, bgBrush, 0, 0, Width, Height, r);

            float pct = (float)(Value - Minimum) / (Maximum - Minimum);
            int barW = (int)(Width * pct);

            if (barW > r * 2)
            {
                using var barBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, barW, Height),
                    Color.FromArgb(180, BarColor), BarColor,
                    LinearGradientMode.Horizontal);
                FillRoundRect(e.Graphics, barBrush, 0, 0, barW, Height, r);

                using var glowBrush = new SolidBrush(Color.FromArgb(60, Color.White));
                e.Graphics.FillRectangle(glowBrush, r, 2, barW - r * 2, Height / 3);
            }
        }

        private void FillRoundRect(Graphics g, Brush brush, int x, int y, int w, int h, int r)
        {
            if (w <= 0 || h <= 0) return;
            var path = new GraphicsPath();
            path.AddArc(x, y, r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  IGNOREDISPOSESTREAM
    // ══════════════════════════════════════════════════════════════════

    internal class IgnoreDisposeStream : Stream
    {
        private readonly Stream _inner;
        public IgnoreDisposeStream(Stream inner) => _inner = inner;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
        protected override void Dispose(bool disposing) { /* No cerrar el stream interno */ }
    }

    // ══════════════════════════════════════════════════════════════════
    //  NATIVEMETHODS
    // ══════════════════════════════════════════════════════════════════

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}

// ══════════════════════════════════════════════════════════════════════
//  FORMRESULTADO
// ══════════════════════════════════════════════════════════════════════

/// <summary>
/// Ventana modal que permite al usuario marcar el test como PASÓ/FALLÓ
/// y guardar el reporte de texto + el archivo WAV grabado.
/// </summary>
public class FormResultado : Form
{
    public string TestResult { get; private set; } = "No definido";

    // ── Paleta ────────────────────────────────────────────────────────
    private readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
    private readonly Color AccentCyan = ColorTranslator.FromHtml("#0099BB");
    private readonly Color AccentGreen = ColorTranslator.FromHtml("#00A85A");
    private readonly Color AccentRed = ColorTranslator.FromHtml("#CC2222");
    private readonly Color AccentBlack = ColorTranslator.FromHtml("#1A1A1A");
    private readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");
    private readonly Color TextMuted = ColorTranslator.FromHtml("#5A6F90");
    private readonly Color BorderColor = ColorTranslator.FromHtml("#F0F2F5");

    // ── Botones ───────────────────────────────────────────────────────
    private Button _btnPaso;
    private Button _btnFallo;
    private Button _btnGuardar;
    private Button _btnCancelar;

    // ── Datos recibidos ───────────────────────────────────────────────
    private readonly string _deviceName;
    private readonly DateTime _testStartTime;
    private readonly List<(DateTime Time, int Volume, string Level)> _logEntries;
    private readonly bool _modoBocina;
    private readonly MemoryStream _audioStream;     // ← stream de audio grabado

    // ══════════════════════════════════════════════════════════════════
    //  CONSTRUCTOR
    // ══════════════════════════════════════════════════════════════════

    public FormResultado(
        string deviceName,
        DateTime testStartTime,
        List<(DateTime, int, string)> logEntries,
        bool modoBocina,
        MemoryStream audioStream)           // ← nuevo parámetro
    {
        _deviceName = deviceName;
        _testStartTime = testStartTime;
        _logEntries = logEntries;
        _modoBocina = modoBocina;
        _audioStream = audioStream;         // ← guardado para usar en SaveReport

        this.Text = "Resultado del Test";
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Size = new Size(620, 280);
        this.BackColor = BgCard;
        this.DoubleBuffered = true;

        BuildUI();
    }

    // ══════════════════════════════════════════════════════════════════
    //  CONSTRUCCIÓN DE INTERFAZ
    // ══════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        int pad = 24;

        // ── Barra de título ────────────────────────────────────────────
        var titleBar = new Panel
        {
            Size = new Size(this.Width, 46),
            Location = new Point(0, 0),
            BackColor = ColorTranslator.FromHtml("#0099BB")
        };

        var lblTitle = new Label
        {
            Text = "📋  RESULTADO DEL TEST",
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            ForeColor = AccentBlack,
            Location = new Point(pad, 13),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 10),
            Size = new Size(46, 46),
            Location = new Point(this.Width - 46, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = TextMuted,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = AccentRed;
        btnClose.Click += (s, e) => { TestResult = "No definido"; this.Close(); };
        btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
        btnClose.MouseLeave += (s, e) => btnClose.ForeColor = TextMuted;

        titleBar.Controls.AddRange(new Control[] { lblTitle, btnClose });
        titleBar.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                MicroTestCloud.NativeMethods.ReleaseCapture();
                MicroTestCloud.NativeMethods.SendMessage(Handle, 0xA1, 0x2, 0);
            }
        };
        this.Controls.Add(titleBar);

        // ── Pregunta ───────────────────────────────────────────────────
        var lblQuestion = new Label
        {
            Text = "¿Cuál es el resultado de la prueba?",
            Location = new Point(pad, 70),
            Size = new Size(560, 24),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = TextMuted,
            BackColor = Color.Transparent
        };
        this.Controls.Add(lblQuestion);

        // ── Botones PASS / FAIL ────────────────────────────────────────
        _btnPaso = MakeBtn("✓  PASS", pad, 104, 170, 48, AccentGreen);
        _btnFallo = MakeBtn("✗  FAIL", pad + 190, 104, 170, 48, AccentRed);

        _btnPaso.Click += (s, e) =>
        {
            TestResult = "PASS";
            _btnPaso.BackColor = Color.FromArgb(60, AccentGreen);
            _btnFallo.BackColor = BgCard;
        };

        _btnFallo.Click += (s, e) =>
        {
            TestResult = "FAIL";
            _btnFallo.BackColor = Color.FromArgb(60, AccentRed);
            _btnPaso.BackColor = BgCard;
        };

        this.Controls.AddRange(new Control[] { _btnPaso, _btnFallo });

        // ── Botones Guardar / Cancelar ─────────────────────────────────
        _btnGuardar = MakeBtn("💾  Guardar reporte", pad, 178, 220, 50, AccentGreen);
        _btnCancelar = MakeBtn("✖  Cancelar", pad + 340, 178, 180, 50, AccentRed);

        _btnGuardar.Click += (s, e) =>
        {
            if (TestResult == "No definido")
            {
                MessageBox.Show("Selecciona PASS o FAIL antes de guardar.",
                    "Resultado requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SaveReport();
            this.Close();
        };

        _btnCancelar.Click += (s, e) =>
        {
            TestResult = "No definido";
            this.Close();
        };

        this.Controls.AddRange(new Control[] { _btnGuardar, _btnCancelar });

        // ── Borde exterior ─────────────────────────────────────────────
        this.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderColor, 2);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  HELPER DE BOTÓN
    // ══════════════════════════════════════════════════════════════════

    private Button MakeBtn(string text, int x, int y, int w, int h, Color accent)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = BgCard,
            ForeColor = accent,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderColor = accent;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, accent);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, accent);
        return btn;
    }

    // ══════════════════════════════════════════════════════════════════
    //  GUARDAR REPORTE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guarda dos archivos con el mismo timestamp:
    ///   · MicroTest_YYYYMMDD_HHMMSS.txt  — reporte estadístico de texto
    ///   · MicroTest_YYYYMMDD_HHMMSS.wav  — audio grabado durante el test
    /// </summary>
    private void SaveReport()
    {
        try
        {
            string baseName = $"MicroTest_{_testStartTime:yyyyMMdd_HHmmss}";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string txtPath = Path.Combine(baseDir, baseName + ".txt");
            string wavPath = Path.Combine(baseDir, baseName + ".wav");
            string modoStr = _modoBocina ? "Bocina (Pista)" : "Voz del operador";

            // ── Calcular estadísticas ──────────────────────────────────
            int volMax = 0, volMin = 100, volSum = 0;
            var levelCounts = new Dictionary<string, int>();

            foreach (var entry in _logEntries)
            {
                volMax = Math.Max(volMax, entry.Volume);
                volMin = Math.Min(volMin, entry.Volume);
                volSum += entry.Volume;
                if (!levelCounts.ContainsKey(entry.Level)) levelCounts[entry.Level] = 0;
                levelCounts[entry.Level]++;
            }

            double volAvg = _logEntries.Count > 0 ? (double)volSum / _logEntries.Count : 0;
            TimeSpan dur = DateTime.Now - _testStartTime;

            // ── Escribir reporte .txt ──────────────────────────────────
            using (var sw = new StreamWriter(txtPath, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("╔══════════════════════════════════════════════════╗");
                sw.WriteLine("║           MICROTEST · REPORTE DE AUDIO           ║");
                sw.WriteLine("╚══════════════════════════════════════════════════╝");
                sw.WriteLine();
                sw.WriteLine($"  Resultado       : {TestResult}");
                sw.WriteLine($"  Fecha y hora    : {_testStartTime:dd/MM/yyyy  HH:mm:ss}");
                sw.WriteLine($"  Micrófono       : {_deviceName}");
                sw.WriteLine($"  Modo de prueba  : {modoStr}");
                sw.WriteLine($"  Duración        : {(int)dur.TotalSeconds} segundos");
                sw.WriteLine($"  Muestras        : {_logEntries.Count}");
                sw.WriteLine($"  Audio grabado   : {baseName}.wav");
                sw.WriteLine();
                sw.WriteLine("──────────────────────────────────────────────────");
                sw.WriteLine("  RESUMEN ESTADÍSTICO");
                sw.WriteLine("──────────────────────────────────────────────────");
                sw.WriteLine($"  Volumen máximo  : {volMax}%");
                sw.WriteLine($"  Volumen mínimo  : {volMin}%");
                sw.WriteLine($"  Volumen promedio: {volAvg:F1}%");
                sw.WriteLine();
                sw.WriteLine("  Distribución de estados:");
                foreach (var kv in levelCounts)
                    sw.WriteLine($"    · {kv.Key,-14} : {kv.Value} seg");
                sw.WriteLine();
                sw.WriteLine("──────────────────────────────────────────────────");
                sw.WriteLine("  REGISTRO DETALLADO");
                sw.WriteLine("──────────────────────────────────────────────────");
                sw.WriteLine($"  {"Hora",-10} {"Volumen",8}   {"Estado",-16}  Barra");
                sw.WriteLine();

                foreach (var entry in _logEntries)
                {
                    int bars = entry.Volume / 5;
                    string bar = "[" + new string('█', bars) + new string('░', 20 - bars) + "]";
                    sw.WriteLine($"  {entry.Time:HH:mm:ss}     {entry.Volume,3}%   {entry.Level,-16}  {bar}");
                }

                sw.WriteLine();
                sw.WriteLine($"  Reporte generado por MicroTest · {DateTime.Now:dd/MM/yyyy HH:mm}");
                sw.WriteLine("──────────────────────────────────────────────────");
            }   // ← StreamWriter se cierra aquí

            // ── Guardar audio .wav ─────────────────────────────────────
            if (_audioStream != null && _audioStream.Length > 0)
            {
                _audioStream.Position = 0;
                using var fs = new FileStream(wavPath, FileMode.Create, FileAccess.Write);
                _audioStream.CopyTo(fs);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo guardar el reporte:\n{ex.Message}", "Error al guardar",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}