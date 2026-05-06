namespace AskForSerial2
{
    public partial class Form1 : Form
    {
        private static readonly Color BgApp = ColorTranslator.FromHtml("#F4F7FC");
        private static readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color Border = ColorTranslator.FromHtml("#D7E1F0");
        private static readonly Color Accent = ColorTranslator.FromHtml("#0099BB");
        private static readonly Color AccentMuted = ColorTranslator.FromHtml("#EAF1FA");
        private static readonly Color Danger = ColorTranslator.FromHtml("#CC2222");
        private static readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#5A6F90");

        public string serial;

        public Form1()
        {
            InitializeComponent();
            ApplyCohesiveTheme();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            container.Top = (panel1.Height - container.Height) / 2;
            container.Left = (panel1.Width - container.Width) / 2;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            serial = textBox1.Text;
            if (serial.Trim() == "")
            {
                MessageBox.Show("Ingrese un serial valido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                File.WriteAllText(".\\serial.txt", serial.Trim());
                Application.Exit();
            }
            
        }

        private void ApplyCohesiveTheme()
        {
            BackColor = BgApp;
            tableLayoutPanel1.BackColor = BgApp;
            panel1.BackColor = BgCard;
            panel1.Padding = new Padding(24);
            panel1.Paint += PaintCardBorder;

            container.BackColor = Color.Transparent;

            label1.ForeColor = TextPrimary;
            label1.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
            label1.Text = "Ingrese el numero de serie";

            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.BackColor = Color.White;
            textBox1.ForeColor = TextPrimary;
            textBox1.Font = new Font("Segoe UI", 22F, FontStyle.Regular);

            tableLayoutPanel2.BackColor = BgApp;
            panel2.BackColor = BgApp;
            panel3.BackColor = BgApp;

            StylePrimaryButton(button1, "Siguiente");
            StyleSecondaryButton(btnCancel, "Cancelar");
        }

        private static void StylePrimaryButton(Button button, string text)
        {
            button.Text = text;
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            button.Height = 56;
            button.Margin = new Padding(8);
        }

        private static void StyleSecondaryButton(Button button, string text)
        {
            button.Text = text;
            button.BackColor = AccentMuted;
            button.ForeColor = TextPrimary;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            button.Height = 56;
            button.Margin = new Padding(8);
        }

        private void PaintCardBorder(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Border, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, panel1.Width - 1, panel1.Height - 1);
        }
    }
}
