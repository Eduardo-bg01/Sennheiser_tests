using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HeadPhoneTest2
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

        public int inputIndex;
        public int outputIndex;
        public int step = 0;

        public bool hearing_pass = false;

        private System.Windows.Forms.Timer timer;
        private int seconds = 5;
        private int seconds2 = 40;
        private int currentTimer = 1;

        WaveOutEvent outputDevice = new WaveOutEvent();
        WaveInEvent waveIn = new WaveInEvent();
        AudioFileReader audioFile;
        WaveFileWriter writer;

        public double peak_right;
        public double peak_left;
        public double level_right;
        public double level_left;
        public double level_diff;
        public double level_avg;
        public double peak;

        public Form1()
        {
            InitializeComponent();
            ApplyCohesiveTheme();
            Resize += (_, _) => ApplyProfessionalLayout();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void LoadInputDevices()
        {
            comboBoxIn.Items.Clear();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var deviceInfo = WaveIn.GetCapabilities(i);
                comboBoxIn.Items.Add(deviceInfo.ProductName);
            }

            if (comboBoxIn.Items.Count > 0)
                comboBoxIn.SelectedIndex = 0;
        }

        private void LoadOutputDevices()
        {
            comboBoxOut.Items.Clear();

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var deviceInfo = WaveOut.GetCapabilities(i);
                comboBoxOut.Items.Add(deviceInfo.ProductName);
            }

            if (comboBoxOut.Items.Count > 0)
                comboBoxOut.SelectedIndex = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadInputDevices();
            LoadOutputDevices();
            ApplyProfessionalLayout();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (step == 0)
            {
                // Seleccion de dispositivos lista; pasar directo a pantalla previa de prueba automatica.
                content1.Visible = false;
                content2.Visible = true;
                inputIndex = comboBoxIn.SelectedIndex;
                outputIndex = comboBoxOut.SelectedIndex;
            }
            if (step == 1)
            {
                // Ejecutar solo la parte automatica (sin confirmacion manual de escucha).
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = 0.5f;

                currentTimer = 2;
                seconds2 = 40;
                btnNext.BackColor = Color.FromArgb(80, SystemColors.InactiveCaption);
                btnNext.Enabled = false;
                btnCancel.BackColor = Color.FromArgb(80, SystemColors.InactiveCaption);
                btnCancel.Enabled = false;

                EnsureTimer();
                timer.Start();

                outputDevice.PlaybackStopped -= stopActions;
                outputDevice.PlaybackStopped += stopActions;
                playAudio("audioSweep");
                startRecording();
                content2.Visible = false;
                content3.Visible = false;
                content4.Visible = false;
                content5.Visible = true;
            }
            if (step == 2)
            {
                Application.Exit();
            }
            step++;
        }

        private void EnsureTimer()
        {
            if (timer != null)
            {
                return;
            }

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (currentTimer == 1)
            {
                seconds--;

                lblCountdown.Text = "Ponga atencion al audio (" + seconds + ")";

                if (seconds <= 0)
                {
                    timer.Stop();
                    btnFail.Enabled = true;
                    btnPass.Enabled = true;
                    btnFail.BackColor = Danger;
                    btnPass.BackColor = Success;
                    lblCountdown.Text = "Escucha el audio de forma clara y sin distorsion?";
                }
            }
            if (currentTimer == 2)
            {
                seconds2--;
                lblPlay.Text = "Realizando prueba de audio\r\nNO RETIRE NI DESCONECTE LOS AUDIFONOS (" + seconds2 + ")";
                if (seconds2 <= 0)
                {
                    timer.Stop();
                    activateButtons();
                }
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnPass_Click(object sender, EventArgs e)
        {
            hearing_pass = true;
            lblStatus.Text = "PASS";
            lblStatus.ForeColor = Success;
            activateButtons();
        }

        private void activateButtons()
        {
            btnNext.BackColor = Accent;
            btnNext.Enabled = true;
            btnCancel.BackColor = Color.FromArgb(232, 238, 248);
            btnCancel.Enabled = true;
        }

        private void btnFail_Click(object sender, EventArgs e)
        {
            hearing_pass = false;
            lblStatus.Text = "FAIL";
            lblStatus.ForeColor = Danger;
            activateButtons();
        }

        void startRecording()
        {
            waveIn.WaveFormat = new WaveFormat(44100, 16, 2);
            writer = new WaveFileWriter("recorded.wav", waveIn.WaveFormat);

            waveIn.DataAvailable -= WaveIn_DataAvailable;
            waveIn.DataAvailable += WaveIn_DataAvailable;

            waveIn.StartRecording();
        }

        void WaveIn_DataAvailable(object sender, WaveInEventArgs a)
        {
            writer?.Write(a.Buffer, 0, a.BytesRecorded);
        }

        void stopRecording()
        {
            waveIn.StopRecording();

            writer?.Dispose();
            writer = null;
        }

        void playAudio(string audioTitle)
        {
            outputDevice.Stop();
            outputDevice.Dispose();

            outputDevice = new WaveOutEvent();
            outputDevice.DeviceNumber = outputIndex;

            if (audioTitle.StartsWith("audioSweep", StringComparison.OrdinalIgnoreCase))
                outputDevice.PlaybackStopped += stopActions;

            string audioPath = ResolveAudioPath(audioTitle);

            audioFile = new AudioFileReader(audioPath);
            outputDevice.Init(audioFile);
            outputDevice.Play();
        }

        private string ResolveAudioPath(string audioTitle)
        {
            var candidates = new List<string>();

            if (Path.HasExtension(audioTitle))
            {
                candidates.Add(audioTitle);

                string alternate = Path.ChangeExtension(audioTitle,
                    Path.GetExtension(audioTitle).Equals(".wav", StringComparison.OrdinalIgnoreCase) ? ".mp3" : ".wav");
                if (!string.Equals(alternate, audioTitle, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(alternate);
                }
            }
            else
            {
                candidates.Add(audioTitle + ".wav");
                candidates.Add(audioTitle + ".mp3");
            }

            foreach (string fileName in candidates)
            {
                string localPath = Path.Combine("audio", fileName);
                if (File.Exists(localPath))
                    return localPath;

                string baseDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", fileName);
                if (File.Exists(baseDirPath))
                    return baseDirPath;
            }

            throw new FileNotFoundException($"Audio file not found for '{audioTitle}'. Tried: {string.Join(", ", candidates)}");
        }

        void stopAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                audioFile?.Dispose();
            }
        }

        void stopActions(object sender, StoppedEventArgs e)
        {
            stopRecording();

            try
            {
                string scriptOutput = RunPythonScript("recorded.wav");

                EvaluateResults("results.json");
                File.WriteAllText("hearingPassResults.txt", "True");

                this.Invoke(() =>
                {
                    levelDetails.Text = "I: " + Math.Round(level_left, 2) + " db | D: " + Math.Round(level_right, 2) + " db";
                    balanceDetails.Text = "Diferencia: " + Math.Round(level_diff, 2) + " db";
                    clippingDetails.Text = "Peak: " + Math.Round(peak, 2) + " db";
                    content5.Visible = false;
                    content6.Visible = true;

                    btnRepeat.Left = (panel12.Width - btnRepeat.Width) / 2;
                    btnRepeat.Top = (panel12.Height - btnRepeat.Height) / 2;

                    if (level_diff > 2)
                    {
                        balanceImg.Image = Properties.Resources.x;
                    }
                    else
                    {
                        balanceImg.Image = Properties.Resources.check;
                    }

                    if (level_left<-30 || level_left>-10 || level_right<-30 || level_right>-10)
                    {
                        levelImg.Image = Properties.Resources.x;
                    }
                    else
                    {
                        levelImg.Image = Properties.Resources.check;
                    }

                    if (peak > 0)
                    {
                        clippingImg.Image = Properties.Resources.x;
                    }
                    else
                    {
                        clippingImg.Image = Properties.Resources.check;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error ejecutando el script: " + ex.Message);
            }


        }

        private string RunPythonScript(string wavPath)
        {
            string script = "db_chart.py";
            string scriptPath = ResolvePythonScriptPath(script);
            string jsonFile = "results.json";
            string pngFile = "resultado.png";
            string args = $"--input \"{wavPath}\" --json-out \"{jsonFile}\" --png-out \"{pngFile}\"";

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "python";
            psi.Arguments = $"\"{scriptPath}\" {args}";
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            StringBuilder output = new StringBuilder();

            using (Process process = Process.Start(psi))
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    output.AppendLine(line);
                }
                process.WaitForExit();
            }

            return output.ToString();
        }

        private string ResolvePythonScriptPath(string scriptName)
        {
            var candidates = new List<string>
            {
                scriptName,
                Path.Combine(Directory.GetCurrentDirectory(), scriptName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", scriptName),
                Path.Combine(Directory.GetCurrentDirectory(), "apps", "pruebasAudifonos", scriptName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "apps", "pruebasAudifonos", scriptName),
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                    return Path.GetFullPath(path);
            }

            return Path.GetFullPath(scriptName);
        }

        private void EvaluateResults(string jsonFile)
        {
            if (!File.Exists(jsonFile))
            {
                MessageBox.Show("No existe el archivo de resultados.json", "Error de preuba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            string jsonText = File.ReadAllText(jsonFile);
            MeasurementsResult result = JsonSerializer.Deserialize<MeasurementsResult>(jsonText);

            var left = result.measurements.FirstOrDefault(m => m.channel == "Left");
            var right = result.measurements.FirstOrDefault(m => m.channel == "Right");

            if (left == null || right == null)
            {
                MessageBox.Show("No existe el archivo de resultados.json", "Error de preuba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            level_left = left.dbfs;
            level_right = right.dbfs;
            level_diff = Math.Abs(level_left - level_right);
            peak_left = left.peak_dbfs;
            peak_right = right.peak_dbfs;
            level_avg = (level_left + level_right) / 2;
            peak = Math.Max(peak_left, peak_right);
        }

        private void btnRepeat_Click(object sender, EventArgs e)
        {
            Reset();
        }

            void Reset()
            {
                // estado del flujo
                step = 0;
                hearing_pass = false;
                currentTimer = 1;

                // timers
                seconds = 5;
                seconds2 = 40;

                if (timer != null)
                {
                    timer.Stop();
                }

                // detener audio y grabaci�n por seguridad
                stopAudio();
                try { stopRecording(); } catch { }

                // visibilidad de contenidos
                content1.Visible = true;
                content2.Visible = false;
                content3.Visible = false;
                content4.Visible = false;
                content5.Visible = false;
                content6.Visible = false;

                // botones
                btnNext.Enabled = true;
                btnCancel.Enabled = true;
                btnFail.Enabled = false;
                btnPass.Enabled = false;

                btnNext.BackColor = Accent;
                btnCancel.BackColor = Color.FromArgb(232, 238, 248);
                btnFail.BackColor = Color.FromArgb(130, Danger);
                btnPass.BackColor = Color.FromArgb(130, Success);

                // labels
                lblStatus.Text = "";
                lblCountdown.Text = "";
                lblPlay.Text = "";

                // im�genes de resultados
                balanceImg.Image = null;
                levelImg.Image = null;
                clippingImg.Image = null;
            
        }

        private void ApplyCohesiveTheme()
        {
            BackColor = BgApp;
            MinimumSize = new Size(1120, 740);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            tableLayoutPanel1.BackColor = BgApp;
            tableLayoutPanel1.Padding = new Padding(24, 18, 24, 18);
            tableLayoutPanel1.RowStyles[0] = new RowStyle(SizeType.Absolute, 36F);
            tableLayoutPanel1.RowStyles[1] = new RowStyle(SizeType.Percent, 100F);
            tableLayoutPanel1.RowStyles[2] = new RowStyle(SizeType.Absolute, 96F);

            panelContainer.BackColor = BgApp;
            buttonContainer.BackColor = BgApp;
            buttonContainer.Margin = new Padding(0);
            buttonContainer.Padding = new Padding(0);

            ApplyThemeToControlTree(this);
            StylePrimaryButton(btnNext, "Siguiente", Accent, Color.White);
            StyleSecondaryButton(btnCancel, "Cancelar");
            StylePrimaryButton(btnPass, "Si", Success, Color.White);
            StylePrimaryButton(btnFail, "No", Danger, Color.White);
            StylePrimaryButton(btnRepeat, "Repetir pruebas", Accent, Color.White);

            comboBoxIn.BackColor = Color.White;
            comboBoxOut.BackColor = Color.White;
            comboBoxIn.ForeColor = TextPrimary;
            comboBoxOut.ForeColor = TextPrimary;
            comboBoxIn.FlatStyle = FlatStyle.Flat;
            comboBoxOut.FlatStyle = FlatStyle.Flat;
            comboBoxIn.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxOut.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxIn.Font = new Font("Segoe UI", 15F, FontStyle.Regular);
            comboBoxOut.Font = new Font("Segoe UI", 15F, FontStyle.Regular);
            comboBoxIn.Height = 48;
            comboBoxOut.Height = 48;

            lblStatus.ForeColor = TextMuted;
            lblCountdown.ForeColor = TextPrimary;
            lblPlay.ForeColor = TextPrimary;
            StyleHeadline(label1);
            StyleHeadline(label4);
            StyleHeadline(label5);
            StyleHeadline(label6);
            lblCountdown.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblPlay.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            label7.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label8.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label9.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            balanceDetails.Font = new Font("Segoe UI", 16F, FontStyle.Regular);
            levelDetails.Font = new Font("Segoe UI", 16F, FontStyle.Regular);
            clippingDetails.Font = new Font("Segoe UI", 16F, FontStyle.Regular);
            ApplyProfessionalLayout();
        }

        private void ApplyThemeToControlTree(Control root)
        {
            foreach (Control child in root.Controls)
            {
                switch (child)
                {
                    case Label label:
                        if (label != lblStatus)
                        {
                            label.ForeColor = TextPrimary;
                        }
                        break;
                    case Panel panel:
                        if (panel == panelContainer || panel == panel2 || panel == panel3)
                        {
                            panel.BackColor = BgApp;
                        }
                        else
                        {
                            panel.BackColor = BgCard;
                            panel.Paint += PaintCardBorder;
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

        private void PaintCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel) return;
            using var pen = new Pen(Border, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        }

        private void ApplyProfessionalLayout()
        {
            foreach (var panel in new[] { content1, content2, content3, content4, content5, content6 })
            {
                panel.Padding = new Padding(34, 26, 34, 24);
            }

            tableLayoutPanel3.Height = 260;
            comboBoxIn.Dock = DockStyle.Top;
            comboBoxOut.Dock = DockStyle.Top;

            buttonContainer.Height = 92;
            panel2.Padding = new Padding(20, 16, 20, 16);
            panel3.Padding = new Padding(20, 16, 20, 16);
            btnCancel.Dock = DockStyle.Fill;
            btnNext.Dock = DockStyle.Fill;
        }
    }
}

public class Measurement
{
    public string channel { get; set; }
    public double rms { get; set; }
    public double dbfs { get; set; }
    public double? dbspl { get; set; }
    public double peak_dbfs { get; set; }
    public double duration_sec { get; set; }
}

public class MeasurementsResult
{
    public List<Measurement> measurements { get; set; }
}