using NAudio.Wave;

namespace AudioTest
{
    public partial class Form1 : Form
    {
        public int step = 1;

        public int inputIndex;

        WaveOutEvent output;
        AudioFileReader reader;

        System.Windows.Forms.Timer timer;
        public int seconds = 7;

        public bool passed;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (step == 1)
            {
                inputIndex = comboBox1.SelectedIndex;
                content1.Visible = false;
                content2.Visible = true;
            }
            if (step == 2)
            {
                content2.Visible = false;
                content3.Visible = true;

                btnNext.Enabled = false;
                btnCancel.Enabled = false;

                PlayAudio("karmaPolice.wav");

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
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            loadOutputDevices();
        }

        private void loadOutputDevices()
        {
            comboBox1.Items.Clear();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var info = WaveOut.GetCapabilities(i);
                comboBox1.Items.Add(info.ProductName);
            }
            if (WaveOut.DeviceCount > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        private void StopAudio()
        {
            if (output != null)
            {
                output.Stop();
                output.Dispose();
                output = null;
            }

            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
        }

        private void PlayAudio(string path)
        {
            StopAudio();

            string fullPath = path;
            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            }
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Audio file not found: {path}");
            }

            reader = new AudioFileReader(fullPath);
            output = new WaveOutEvent();
            output.DeviceNumber = inputIndex;

            output.Init(reader);
            output.Play();
        }

        private void btnFail_Click(object sender, EventArgs e)
        {
            passed = false;
            label4.Text = "FAIL";
            label4.ForeColor = Color.Red;
            btnCancel.Enabled = true;
            btnNext.Enabled = true;
        }

        private void btnPass_Click(object sender, EventArgs e)
        {
            passed = true;
            label4.Text = "PASS";
            label4.ForeColor = Color.Green;
            btnCancel.Enabled = true;
            btnNext.Enabled = true;
        }
    }
}
