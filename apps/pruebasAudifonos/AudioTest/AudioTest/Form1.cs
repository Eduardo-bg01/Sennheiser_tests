using NAudio.Wave;

namespace AudioTest
{
    public partial class Form1 : Form
    {
        private static readonly Color BgApp = ColorTranslator.FromHtml("#F4F7FC");
        private static readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color Border = ColorTranslator.FromHtml("#D7E1F0");
        private static readonly Color Accent = ColorTranslator.FromHtml("#0099BB");
        private static readonly Color Success = ColorTranslator.FromHtml("#00A85A");
        private static readonly Color Danger = ColorTranslator.FromHtml("#CC2222");
        private static readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#5A6F90");

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
            ApplyProfessionalLayout();
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
            StyleButton(btnNext, Accent, Color.White);
            StyleButton(btnCancel, Color.FromArgb(232, 238, 248), TextPrimary);
            StyleButton(btnPass, Success, Color.White);
            StyleButton(btnFail, Danger, Color.White);
            StyleHeadline(label1);
            StyleHeadline(label2);
            StyleHeadline(label3);
            label4.Font = new Font("Segoe UI", 24F, FontStyle.Bold);

            comboBox1.BackColor = Color.White;
            comboBox1.ForeColor = TextPrimary;
            comboBox1.FlatStyle = FlatStyle.Flat;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.Font = new Font("Segoe UI", 16F, FontStyle.Regular);
            comboBox1.Height = 46;
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
                            panel.Paint += PaintCardBorder;
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
            button.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            button.Height = 56;
        }

        private static void StyleHeadline(Label label)
        {
            label.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        }

        private void PaintCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel) return;
            using var pen = new Pen(Border, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        }

        private void ApplyProfessionalLayout()
        {
            int pad = 40;
            int maxW = Math.Min(960, panelContainer.Width - (pad * 2));

            foreach (var panel in new[] { content1, content2, content3 })
            {
                panel.Padding = new Padding(pad, 30, pad, 24);
            }

            label1.Height = 70;
            label2.Height = 70;
            label3.Height = 70;
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label2.TextAlign = ContentAlignment.MiddleCenter;
            label3.TextAlign = ContentAlignment.MiddleCenter;

            comboBox1.Dock = DockStyle.None;
            comboBox1.Width = maxW;
            comboBox1.Location = new Point((content1.Width - comboBox1.Width) / 2, 110);

            tableLayoutPanel3.Height = 130;
            btnFail.Dock = DockStyle.Fill;
            btnPass.Dock = DockStyle.Fill;
            panel1.Padding = new Padding(26, 10, 26, 10);
            panel4.Padding = new Padding(26, 10, 26, 10);
        }
    }
}
