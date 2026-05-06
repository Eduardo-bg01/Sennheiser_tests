namespace AskForSerial2
{
    public partial class Form1 : Form
    {
        private static readonly Color BgApp = ColorTranslator.FromHtml("#F4F7FC");
        private static readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color Border = ColorTranslator.FromHtml("#D7E1F0");
        private static readonly Color Accent = ColorTranslator.FromHtml("#0099BB");
        private static readonly Color AccentMuted = ColorTranslator.FromHtml("#EAF1FA");
        private static readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");

        public string serial;

        public Form1()
        {
            InitializeComponent();
            ApplyCohesiveTheme();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            CenterContainer();
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
            MinimumSize = new Size(1024, 640);
            tableLayoutPanel1.BackColor = BgApp;
            tableLayoutPanel1.Padding = new Padding(28, 20, 28, 20);
            tableLayoutPanel1.RowStyles[0] = new RowStyle(SizeType.Absolute, 40F);
            tableLayoutPanel1.RowStyles[1] = new RowStyle(SizeType.Percent, 100F);
            tableLayoutPanel1.RowStyles[2] = new RowStyle(SizeType.Absolute, 96F);

            panel1.BackColor = BgCard;
            panel1.Padding = new Padding(40, 24, 40, 24);
            panel1.Paint += PaintCardBorder;
            panel1.Resize += (_, _) => CenterContainer();

            container.BackColor = Color.Transparent;
            container.MaximumSize = new Size(980, 320);
            container.Width = Math.Min(panel1.Width - 80, 980);
            container.Height = 220;

            label1.ForeColor = TextPrimary;
            label1.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            label1.Text = "Ingrese el numero de serie del producto";

            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.BackColor = Color.White;
            textBox1.ForeColor = TextPrimary;
            textBox1.Font = new Font("Segoe UI", 20F, FontStyle.Regular);

            tableLayoutPanel2.BackColor = BgApp;
            panel2.BackColor = BgApp;
            panel3.BackColor = BgApp;

            StylePrimaryButton(button1, "Siguiente");
            StyleSecondaryButton(btnCancel, "Cancelar");
        }

        private void CenterContainer()
        {
            container.Top = (panel1.Height - container.Height) / 2;
            container.Left = (panel1.Width - container.Width) / 2;
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
