using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HeadPhoneTest2
{
    public partial class Form1 : Form
    {
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
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (step == 0)
            {
                //pasar al paso de colocar audifonos sobre cabeza
                content1.Visible = false;
                content2.Visible = true;
                inputIndex = comboBoxIn.SelectedIndex;
                outputIndex = comboBoxOut.SelectedIndex;
            }
            if (step == 1)
            {
                //hacer play a la cancion
                btnNext.BackColor = Color.FromArgb(80, SystemColors.InactiveCaption);
                btnFail.BackColor = Color.FromArgb(80, Color.DarkSalmon);
                btnPass.BackColor = Color.FromArgb(80, Color.PaleGreen);

                content2.Visible = false;
                content3.Visible = true;
                inputIndex = comboBoxIn.SelectedIndex;
                outputIndex = comboBoxOut.SelectedIndex;
                btnNext.Enabled = false;

                outputDevice.DeviceNumber = outputIndex;
                waveIn.DeviceNumber = inputIndex;

                playAudio("karmaPolice.wav");

                timer = new System.Windows.Forms.Timer();
                timer.Interval = 1000;
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            if (step == 2)
            {
                //colocar audifonos sobre orejas
                stopAudio();
                File.WriteAllText("hearingPassResults.txt", this.hearing_pass.ToString());
                content3.Visible = false;
                content4.Visible = true;
            }
            if (step == 3)
            {
                //play al audioSweep
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = 0.5f;

                currentTimer = 2;
                btnNext.BackColor = Color.FromArgb(80, SystemColors.InactiveCaption);
                btnNext.Enabled = false;
                btnCancel.BackColor = Color.FromArgb(80, SystemColors.InactiveCaption);
                btnCancel.Enabled = false;
                timer.Start();
                outputDevice.PlaybackStopped -= stopActions;
                outputDevice.PlaybackStopped += stopActions;
                playAudio("audioSweep.wav");
                startRecording();
                content4.Visible = false;
                content5.Visible = true;
            }
            if (step == 4)
            {
                Application.Exit();
            }
            step++;
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
                    btnFail.BackColor = Color.FromArgb(255, Color.DarkSalmon);
                    btnPass.BackColor = Color.FromArgb(255, Color.PaleGreen);
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
            lblStatus.ForeColor = Color.Green;
            activateButtons();
        }

        private void activateButtons()
        {
            btnNext.BackColor = Color.FromArgb(255, SystemColors.InactiveCaption);
            btnNext.Enabled = true;
            btnCancel.BackColor = Color.FromArgb(255, SystemColors.InactiveCaption);
            btnCancel.Enabled = true;
        }

        private void btnFail_Click(object sender, EventArgs e)
        {
            hearing_pass = false;
            lblStatus.Text = "FAIL";
            lblStatus.ForeColor = Color.Red;
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

            if(audioTitle=="audioSweep.wav")
            outputDevice.PlaybackStopped += stopActions;

            audioFile = new AudioFileReader("audio/" + audioTitle);
            outputDevice.Init(audioFile);
            outputDevice.Play();
        }

        void stopAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                audioFile.Dispose();
            }
        }

        void stopActions(object sender, StoppedEventArgs e)
        {
            stopRecording();

            try
            {
                string scriptOutput = RunPythonScript("recorded.wav");

                EvaluateResults("results.json");

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
            string jsonFile = "results.json";
            string pngFile = "resultado.png";
            string args = $"--input \"{wavPath}\" --json-out \"{jsonFile}\" --png-out \"{pngFile}\"";

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "python";
            psi.Arguments = $"{script} {args}";
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

                // detener audio y grabación por seguridad
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

                btnNext.BackColor = Color.FromArgb(255, SystemColors.InactiveCaption);
                btnCancel.BackColor = Color.FromArgb(255, SystemColors.InactiveCaption);
                btnFail.BackColor = Color.FromArgb(80, Color.DarkSalmon);
                btnPass.BackColor = Color.FromArgb(80, Color.PaleGreen);

                // labels
                lblStatus.Text = "";
                lblCountdown.Text = "";
                lblPlay.Text = "";

                // imágenes de resultados
                balanceImg.Image = null;
                levelImg.Image = null;
                clippingImg.Image = null;
            
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