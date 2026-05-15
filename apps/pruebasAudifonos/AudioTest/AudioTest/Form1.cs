using NAudio.Wave;

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

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (step == 1)
            {
                headphonesOutputIndex = comboHeadphones.SelectedIndex;
                speakerOutputIndex = comboSpeakers.SelectedIndex;
                microphoneInputIndex = comboMicrophones.SelectedIndex;
                content1.Visible = false;
                content2.Visible = true;
            }
            if (step == 2)
            {
                content2.Visible = false;
                content3.Visible = true;

                btnNext.Enabled = false;
                btnCancel.Enabled = false;

                StartRecording();
                PlayAudio("karmaPolice.wav",headphonesOutputIndex);

                timer = new System.Windows.Forms.Timer();
                timer.Interval = 1000;
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            if (step == 3)
            {
                File.WriteAllText("hearingPassResults.txt", passed.ToString());
                Application.Exit();
            }
            step++;
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
                timer.Stop();
                StopRecording();
                StopAudio();
                PlayAudio(recordedAudioPath, speakerOutputIndex,true);
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            LoadDevices();
            ApplyProfessionalLayout();

            try
            {
                pictureBox1.Image = Image.FromFile("miniDSP.jpg");
            }
            catch
            {
                // miniDSP.jpg not found - this is optional, continue without it
                pictureBox1.BackColor = Color.LightGray;
            }
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
    bool loop = false
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

            output = new WaveOutEvent();
            output.DeviceNumber = outputDeviceIndex;

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
            if (File.Exists(recordedAudioPath))
            {
                File.Delete(recordedAudioPath);
            }

            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = microphoneInputIndex;

            waveIn.WaveFormat = new WaveFormat(44100, 1);

            writer = new WaveFileWriter(
                recordedAudioPath,
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
}
