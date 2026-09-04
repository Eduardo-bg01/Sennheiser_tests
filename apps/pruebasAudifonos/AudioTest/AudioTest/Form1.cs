using NAudio.Wave;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AudioTest
{
    public partial class Form1 : Form
    {
        private static readonly Color BgApp = ColorTranslator.FromHtml("#F4F7FC");
        private static readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color Border = ColorTranslator.FromHtml("#D7E1F0");
        private static readonly Color Accent = ColorTranslator.FromHtml("#0099BB");
        private static readonly Color AccentMuted = ColorTranslator.FromHtml("#EAF1FA");
        private static readonly Color Success = ColorTranslator.FromHtml("#00A85A");
        private static readonly Color Danger = ColorTranslator.FromHtml("#CC2222");
        private static readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#5A6F90");

        public int step = 1;

        public int headphonesOutputIndex;
        public int speakerOutputIndex;
        public int microphoneInputIndex;

        WaveOutEvent? output;
        AudioFileReader? reader;

        LoopStream? loopStream;

        WaveInEvent? waveIn;
        WaveFileWriter? writer;

        string recordedAudioPath = "ear_microphone_capture.wav";

        System.Windows.Forms.Timer? timer;
        public int seconds = 15;

        public bool passed;

        private readonly bool quickVariant =
            Environment.GetEnvironmentVariable("QUICK_AUDIO") == "1";

        // Ciclo de repeticiones de la prueba auditiva, una por cada tipo de conexion fisica.
        // - RS 255 y RS 275: 3 conexiones (USB, Optico, Analogico).
        // - RS 195: 2 conexiones (Optico, Analogico) - no tiene entrada USB.
        // - El resto de los modelos: flujo de una sola prueba, sin cambios.
        private static readonly string[] FullConnectionTypes = { "1. USB", "2. �ptico", "3. Anal�gico 3.5" };
        private static readonly string[] FullConnectionFileSuffixes = { "USB", "Optico", "Analogico" };
        private static readonly string[] NoUsbConnectionTypes = { "1. �ptico", "2. Anal�gico 3.5" };
        private static readonly string[] NoUsbConnectionFileSuffixes = { "Optico", "Analogico" };

        private static readonly string[] FullCycleModels = { "rs255", "rs275" };
        private static readonly string[] NoUsbCycleModels = { "rs195" };

        private int connectionIndex = 0;
        private readonly bool isRSModel;
        private readonly string[] ConnectionTypes;
        private readonly string[] ConnectionFileSuffixes;
        private Label? connectionInfoLabel;
        private readonly List<HearingRunSummary> connectionResults = new List<HearingRunSummary>();

        // Nombre de archivo separado por conexion (solo modelos con ciclo); el resto de los
        // modelos usa el nombre de siempre.
        private string RecordedAudioPath() => isRSModel
            ? $"ear_microphone_capture_{ConnectionFileSuffixes[connectionIndex]}.wav"
            : recordedAudioPath;

        public Form1()
        {
            if (quickVariant)
            {
                seconds = 7;
            }

            string device = Environment.GetEnvironmentVariable("DEVICE_NAME") ?? "";
            string norm = new string(device.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

            if (FullCycleModels.Any(m => norm.Contains(m)))
            {
                isRSModel = true;
                ConnectionTypes = FullConnectionTypes;
                ConnectionFileSuffixes = FullConnectionFileSuffixes;
            }
            else if (NoUsbCycleModels.Any(m => norm.Contains(m)))
            {
                isRSModel = true;
                ConnectionTypes = NoUsbConnectionTypes;
                ConnectionFileSuffixes = NoUsbConnectionFileSuffixes;
            }
            else
            {
                isRSModel = false;
                ConnectionTypes = Array.Empty<string>();
                ConnectionFileSuffixes = Array.Empty<string>();
            }

            InitializeComponent();
            ApplyCohesiveTheme();
            CreateConnectionInfoLabel();
            Resize += (_, _) => ApplyProfessionalLayout();
            FormClosing += (s, e) => { try { if (!File.Exists("hearingPassResults.txt")) File.WriteAllText("hearingPassResults.txt", "False"); } catch { } };
        }

        private void CreateConnectionInfoLabel()
        {
            // Se agrega a la fila 0 de tableLayoutPanel1 (siempre visible, sin importar
            // que "content" este activo) para recordar en todo momento la conexion actual.
            // Solo se muestra para modelos RS, que son los unicos con base multi-entrada.
            connectionInfoLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Accent,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Visible = isRSModel,
            };
            tableLayoutPanel1.Controls.Add(connectionInfoLabel, 0, 0);
            UpdateConnectionInfoLabel();
        }

        private void UpdateConnectionInfoLabel()
        {
            if (connectionInfoLabel == null)
                return;

            connectionInfoLabel.Text = "Tipo de conexi�n: " + ConnectionTypes[connectionIndex] +
                "   (prueba " + (connectionIndex + 1) + " de " + ConnectionTypes.Length + ")";
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // ponytail: write FAIL so batch/run.bat can continue with operator FAIL instead of 5 retries -> exit 2
            try { if (!File.Exists("hearingPassResults.txt")) File.WriteAllText("hearingPassResults.txt", "False"); } catch { }
            Application.Exit();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (step == 1)
            {
                headphonesOutputIndex = comboHeadphones.SelectedIndex;
                speakerOutputIndex = comboSpeakers.SelectedIndex;
                microphoneInputIndex = comboMicrophones.SelectedIndex;

                if (isRSModel && connectionIndex == 0)
                {
                    MessageBox.Show(
                        "Se probar� la siguiente entrada:\r\n\r\n" +
                        ConnectionTypes[0] +
                        "\r\n\r\nAseg�rese de que los aud�fonos est�n conectados de esta forma antes de continuar.",
                        "Iniciar prueba de conexi�n",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                content1.Visible = false;
                content3.Visible = true;

                btnNext.Enabled = false;
                btnCancel.Enabled = false;

                // ponytail: clamp indices, guard no-device crash that leaves hearingPassResults missing -> batch retries auto-fail
                if (headphonesOutputIndex < 0) headphonesOutputIndex = 0;
                if (speakerOutputIndex < 0) speakerOutputIndex = 0;
                if (microphoneInputIndex < 0) microphoneInputIndex = 0;
                if (WaveOut.DeviceCount == 0 || WaveIn.DeviceCount == 0 || comboHeadphones.Items.Count == 0 || comboMicrophones.Items.Count == 0)
                {
                    MessageBox.Show("No se detectaron dispositivos de audio. Verifique que los audifonos/microfono esten conectados.", "AudioTest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    try { File.WriteAllText("hearingPassResults.txt", "False"); } catch { }
                    Application.Exit();
                    return;
                }

                SetPlaybackVolume(100);
                try
                {
                    StartRecording();
                    // ponytail: clamp 30s offset if file shorter
                    TimeSpan? offset = quickVariant ? TimeSpan.FromSeconds(30) : null;
                    PlayAudio("karmaPolice.wav", headphonesOutputIndex, startOffset: offset);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al iniciar audio/grabacion:\n" + ex.Message, "AudioTest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    try { File.WriteAllText("hearingPassResults.txt", "False"); } catch { }
                    try { StopRecording(); } catch { }
                    try { StopAudio(); } catch { }
                    Application.Exit();
                    return;
                }

                timer = new System.Windows.Forms.Timer();
                timer.Interval = 1000;
                timer.Tick += Timer_Tick;
                timer.Start();
                step = 3;
                return;
            }
            if (step == 3)
            {
                if (!isRSModel)
                {
                    File.WriteAllText("hearingPassResults.txt", passed.ToString());
                    Application.Exit();
                    return;
                }

                // Guarda el resultado de esta conexion para el resumen final del ciclo.
                connectionResults.Add(new HearingRunSummary
                {
                    connection = ConnectionTypes[connectionIndex],
                    passed = passed,
                });

                if (connectionIndex < ConnectionTypes.Length - 1)
                {
                    connectionIndex++;
                    MessageBox.Show(
                        "Cambie el tipo de conexi�n de los aud�fonos a:\r\n\r\n" +
                        ConnectionTypes[connectionIndex] +
                        "\r\n\r\nUna vez conectado, presione OK para continuar con la siguiente prueba.",
                        "Cambiar tipo de conexi�n",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    UpdateConnectionInfoLabel();
                    ResetForNextConnection();
                    return;
                }
                else
                {
                    SaveConnectionSummary();
                    MessageBox.Show(
                        "Se completaron las pruebas para las 3 conexiones (USB, �ptico y Anal�gico 3.5).\r\n\r\n" +
                        "Resumen guardado en tests_conexiones.json",
                        "Ciclo de pruebas completo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    File.WriteAllText("hearingPassResults.txt", passed.ToString());
                    Application.Exit();
                    return;
                }
            }
        }

        private void ResetForNextConnection()
        {
            step = 1;
            passed = false;
            seconds = quickVariant ? 7 : 15;

            if (timer != null)
            {
                timer.Stop();
            }

            StopAudio();
            try { StopRecording(); } catch { }

            content3.Visible = false;
            content1.Visible = true;

            btnNext.Enabled = true;
            btnCancel.Enabled = true;
            btnPass.Enabled = false;
            btnFail.Enabled = false;
            btnPass.Visible = false;
            btnFail.Visible = false;

            label2.Text = "";
            label4.Text = "";
        }

        private void SaveConnectionSummary()
        {
            try
            {
                var payload = new
                {
                    date = DateTime.Now.ToString("yyyy-MM-dd"),
                    time = DateTime.Now.ToString("HH:mm:ss"),
                    results = connectionResults
                };
                File.WriteAllText("tests_conexiones.json",
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

                var sb = new StringBuilder();
                sb.AppendLine("Resumen de prueba auditiva (AudioTest) - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();
                foreach (var r in connectionResults)
                {
                    sb.AppendLine("Conexi�n: " + r.connection);
                    sb.AppendLine("  Resultado: " + (r.passed ? "PASS" : "FAIL"));
                    sb.AppendLine();
                }
                File.WriteAllText("tests_conexiones.txt", sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error guardando el resumen: " + ex.Message, "Resumen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            seconds--;
            label2.Text = "Preste atencion al audio (" + seconds + ")";

            if (seconds <= 0)
            {
                label2.Text = "Escucha el audio de forma clara y sin distorsion?";
                btnPass.Enabled = true;
                btnFail.Enabled = true;
                btnFail.Visible = true;
                btnPass.Visible = true;
                try { timer.Stop(); } catch { }
                try { StopRecording(); } catch { }
                try { StopAudio(); } catch { }
                try { PlayAudio(RecordedAudioPath(), speakerOutputIndex, true); }
                catch (Exception ex)
                {
                    // ponytail: don't leave hearingPass missing -> batch auto-retry loop
                    label2.Text = "Error al reproducir grabaci\u00f3n: " + ex.Message;
                    // still allow Si/No so operator can decide
                }
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            LoadDevices();
            ApplyProfessionalLayout();
            pictureBox1.Image = null;
            pictureBox1.Visible = false;
        }

        //START-MOD FONG
        private void LoadDevices()
        {
            comboHeadphones.Items.Clear();
            comboSpeakers.Items.Clear();
            comboMicrophones.Items.Clear();

            // OUTPUTS
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var info = WaveOut.GetCapabilities(i);

                comboHeadphones.Items.Add(info.ProductName);
                comboSpeakers.Items.Add(info.ProductName);
            }

            // INPUTS
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var info = WaveIn.GetCapabilities(i);

                comboMicrophones.Items.Add(info.ProductName);
            }

            if (comboHeadphones.Items.Count > 0)
                comboHeadphones.SelectedIndex = 0;

            if (comboSpeakers.Items.Count > 0)
                comboSpeakers.SelectedIndex = 0;

            if (comboMicrophones.Items.Count > 0)
                comboMicrophones.SelectedIndex = 0;
        }
        //END-MOD FONG

        private void StopAudio()
        {
            if (output != null)
            {
                output.Stop();
                output.Dispose();
                output = null;
            }
            if (loopStream != null)
            {
                loopStream.Dispose();
                loopStream = null;
            }
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
        }

        private void PlayAudio(
    string path,
    int outputDeviceIndex,
    bool loop = false,
    TimeSpan? startOffset = null
)
        {
            StopAudio();

            string fullPath = path;

            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    path
                );
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"Audio file not found: {path}"
                );
            }

            reader = new AudioFileReader(fullPath);
            if (startOffset.HasValue)
            {
                // ponytail: clamp offset inside file duration (e.g. short test file)
                var dur = reader.TotalTime;
                var off = startOffset.Value;
                if (off < TimeSpan.Zero) off = TimeSpan.Zero;
                if (off >= dur) off = TimeSpan.Zero; // fallback to start if beyond
                else if (dur - off < TimeSpan.FromSeconds(2)) off = dur - TimeSpan.FromSeconds(2);
                if (off < TimeSpan.Zero) off = TimeSpan.Zero;
                reader.CurrentTime = off;
            }

            output = new WaveOutEvent();
            output.DeviceNumber = outputDeviceIndex;
            output.Volume = 1.0f;

            if (loop)
            {
                loopStream = new LoopStream(reader);
                output.Init(loopStream);
            }
            else
            {
                output.Init(reader);
            }

            output.Play();
        }

        private void StartRecording()
        {
            string audioPath = RecordedAudioPath();
            if (File.Exists(audioPath))
            {
                File.Delete(audioPath);
            }

            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = microphoneInputIndex;

            waveIn.WaveFormat = new WaveFormat(44100, 1);

            writer = new WaveFileWriter(
                audioPath,
                waveIn.WaveFormat
            );

            waveIn.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                writer.Flush();
            };

            waveIn.StartRecording();
        }

        private void StopRecording()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }

            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        private void btnFail_Click(object sender, EventArgs e)
        {
            passed = false;
            label4.Text = "FAIL";
            label4.ForeColor = Danger;
            btnCancel.Enabled = true;
            btnNext.Enabled = true;
        }

        private void btnPass_Click(object sender, EventArgs e)
        {
            passed = true;
            label4.Text = "PASS";
            label4.ForeColor = Success;
            btnCancel.Enabled = true;
            btnNext.Enabled = true;
        }

        private void ApplyCohesiveTheme()
        {
            BackColor = BgApp;
            MinimumSize = new Size(1080, 700);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            tableLayoutPanel1.BackColor = BgApp;
            tableLayoutPanel1.Padding = new Padding(24, 18, 24, 18);
            tableLayoutPanel1.RowStyles[0] = new RowStyle(SizeType.Absolute, 36F);
            tableLayoutPanel1.RowStyles[1] = new RowStyle(SizeType.Percent, 100F);
            tableLayoutPanel1.RowStyles[2] = new RowStyle(SizeType.Absolute, 96F);

            panelContainer.BackColor = BgApp;
            tableLayoutPanel2.BackColor = BgApp;
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Padding = new Padding(0);

            ApplyThemeToControlTree(this);
            StylePrimaryButton(btnNext, "Siguiente", Accent, Color.White);
            StyleSecondaryButton(btnCancel, "Cancelar");
            StylePrimaryButton(btnPass, "Si", Success, Color.White);
            StylePrimaryButton(btnFail, "No", Danger, Color.White);
            StyleHeadline(label1);
            StyleHeadline(label2);
            StyleHeadline(label3);
            label4.Font = new Font("Segoe UI", 22F, FontStyle.Bold);

            foreach (var combo in new[]
            {
                comboHeadphones,
                comboSpeakers,
                comboMicrophones
            })
            {
                combo.BackColor = Color.White;
                combo.ForeColor = TextPrimary;
                combo.Font = new Font("Segoe UI", 13F);
                combo.FlatStyle = FlatStyle.Flat;
            }

            ApplyProfessionalLayout();
        }

        private void ApplyThemeToControlTree(Control root)
        {
            foreach (Control child in root.Controls)
            {
                switch (child)
                {
                    case Panel panel:
                        if (panel == panel2 || panel == panel3)
                        {
                            panel.BackColor = BgApp;
                        }
                        else if (panel == panelContainer)
                        {
                            panel.BackColor = BgApp;
                        }
                        else
                        {
                            panel.BackColor = BgCard;
                        }
                        break;
                    case Label label:
                        label.ForeColor = TextPrimary;
                        if (label == label4)
                        {
                            label.ForeColor = TextMuted;
                        }
                        break;
                }

                if (child.HasChildren)
                {
                    ApplyThemeToControlTree(child);
                }
            }
        }

        private static void StyleButton(Button button, Color bgColor, Color foreColor)
        {
            button.BackColor = bgColor;
            button.ForeColor = foreColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            button.Height = 56;
            button.Margin = new Padding(8);
        }

        private static void StylePrimaryButton(Button button, string text, Color bgColor, Color foreColor)
        {
            button.Text = text;
            StyleButton(button, bgColor, foreColor);
        }

        private static void StyleSecondaryButton(Button button, string text)
        {
            button.Text = text;
            button.BackColor = AccentMuted;
            button.ForeColor = TextPrimary;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            button.Height = 56;
            button.Margin = new Padding(8);
        }

        private static void StyleHeadline(Label label)
        {
            label.ForeColor = TextPrimary;
            label.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        }



        private void ApplyProfessionalLayout()
        {
            int pad = 40;
            int maxW = Math.Min(960, panelContainer.Width - (pad * 2));

            foreach (var panel in new[] { content1, content2, content3 })
            {
                panel.Padding = new Padding(pad, 28, pad, 24);
            }

            label1.Height = 66;
            label2.Height = 66;
            label3.Height = 66;
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label2.TextAlign = ContentAlignment.MiddleCenter;
            label3.TextAlign = ContentAlignment.MiddleCenter;

            //START-MOD FONG
            int startX = (content1.Width - maxW) / 2;

            labelHeadphones.Location = new Point(startX, 100);
            comboHeadphones.Location = new Point(startX, 145);
            comboHeadphones.Width = maxW;

            labelSpeakers.Location = new Point(startX, 225);
            comboSpeakers.Location = new Point(startX, 270);
            comboSpeakers.Width = maxW;

            labelMicrophones.Location = new Point(startX, 350);
            comboMicrophones.Location = new Point(startX, 395);
            comboMicrophones.Width = maxW;
            //END-MOD FONG

            tableLayoutPanel3.Height = 130;
            btnFail.Dock = DockStyle.Fill;
            btnPass.Dock = DockStyle.Fill;
            panel1.Padding = new Padding(24, 10, 24, 10);
            panel4.Padding = new Padding(24, 10, 24, 10);
        }

        private static void SetPlaybackVolume(int percent)
        {
            string helperPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VolumeHelper.exe");
            if (!File.Exists(helperPath))
            {
                return;
            }

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = helperPath,
                    Arguments = percent.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                process?.WaitForExit(5000);
            }
            catch
            {
            }
        }
    }

    public class LoopStream : WaveStream
    {
        private readonly WaveStream sourceStream;

        public LoopStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
        }

        public override WaveFormat WaveFormat => sourceStream.WaveFormat;

        public override long Length => sourceStream.Length;

        public override long Position
        {
            get => sourceStream.Position;
            set => sourceStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = sourceStream.Read(
                    buffer,
                    offset + totalBytesRead,
                    count - totalBytesRead
                );

                if (bytesRead == 0)
                {
                    sourceStream.Position = 0;
                }
                else
                {
                    totalBytesRead += bytesRead;
                }
            }

            return totalBytesRead;
        }
    }

    public class HearingRunSummary
    {
        public string connection { get; set; }
        public bool passed { get; set; }
    }
}