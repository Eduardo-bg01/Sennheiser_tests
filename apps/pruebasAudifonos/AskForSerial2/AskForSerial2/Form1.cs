namespace AskForSerial2
{
    public partial class Form1 : Form
    {
        private const string SERIAL_FILE = ".\\serial.txt";
        private const int MIN_WINDOW_WIDTH = 1024;
        private const int MIN_WINDOW_HEIGHT = 640;
        private const int CONTENT_WIDTH = 980;
        private const int CONTENT_HEIGHT = 260;

        public string serial;

        public Form1()
        {
            InitializeComponent();

            AcceptButton = button1;

            ApplyCohesiveTheme();
            Resize += (_, _) => ApplyProfessionalLayout();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            ApplyProfessionalLayout();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Trim antes de validar. Se guarda como texto: nunca se convierte a
            // numero, para no perder los ceros a la izquierda (0364031120).
            serial = textBox1.Text.Trim();

            if (serial.Length == 0)
            {
                MessageBox.Show("Ingrese el numero de serie del producto.",
                    "Serial requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // Equivale a ^\d{10}$|^\d{12}$: 10 o 12 digitos, nada mas.
            if (serial.Length is not (10 or 12) || !serial.All(char.IsDigit))
            {
                MessageBox.Show(
                    "El serial debe tener 10 o 12 digitos, solo numeros.\n\n" +
                    "No use espacios, guiones ni otros caracteres.",
                    "Serial invalido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                textBox1.SelectAll();
                return;
            }

            File.WriteAllText(SERIAL_FILE, serial);
            Application.Exit();
        }

        private void ApplyCohesiveTheme()
        {
            UIHelper.StyleFormBackground(this);
            BackColor = SharedTheme.BgApp;
            MinimumSize = new Size(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            
            tableLayoutPanel1.BackColor = SharedTheme.BgApp;
            tableLayoutPanel1.Padding = new Padding(28, 20, 28, 20);
            tableLayoutPanel1.RowStyles[0] = new RowStyle(SizeType.Absolute, 40F);
            tableLayoutPanel1.RowStyles[1] = new RowStyle(SizeType.Percent, 100F);
            tableLayoutPanel1.RowStyles[2] = new RowStyle(SizeType.Absolute, 96F);

            panel1.BackColor = SharedTheme.BgCard;
            panel1.Padding = new Padding(40, 24, 40, 24);
            panel1.Paint += PaintCardBorder;
            panel1.Resize += (_, _) => ApplyProfessionalLayout();

            container.BackColor = Color.Transparent;
            container.MaximumSize = new Size(CONTENT_WIDTH, 320);
            container.Width = Math.Min(panel1.Width - 80, CONTENT_WIDTH);
            container.Height = 220;

            label1.ForeColor = SharedTheme.TextPrimary;
            label1.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            label1.Text = "Ingrese el numero de serie del producto";

            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.BackColor = Color.White;
            textBox1.ForeColor = SharedTheme.TextPrimary;
            textBox1.Font = new Font("Segoe UI", 18F, FontStyle.Regular);

            tableLayoutPanel2.BackColor = SharedTheme.BgApp;
            panel2.BackColor = SharedTheme.BgApp;
            panel3.BackColor = SharedTheme.BgApp;

            UIHelper.StylePrimaryButton(button1);
            UIHelper.StyleSecondaryButton(btnCancel);
            button1.Text = "Siguiente";
            btnCancel.Text = "Cancelar";
            button1.Height = 56;
            btnCancel.Height = 56;
            button1.Margin = new Padding(8);
            btnCancel.Margin = new Padding(8);
            button1.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            btnCancel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            
            ApplyProfessionalLayout();
        }

        private void ApplyProfessionalLayout()
        {
            int contentWidth = Math.Min(CONTENT_WIDTH - 40, panel1.Width - 80);
            int contentHeight = CONTENT_HEIGHT;
            container.Size = new Size(contentWidth, contentHeight);
            container.Top = (panel1.Height - container.Height) / 2;
            container.Left = (panel1.Width - container.Width) / 2;

            label1.Dock = DockStyle.Top;
            label1.Height = 74;
            label1.TextAlign = ContentAlignment.MiddleCenter;

            textBox1.Dock = DockStyle.Top;
            textBox1.Height = 54;
            textBox1.Margin = new Padding(0, 14, 0, 0);

            panel2.Padding = new Padding(12, 16, 12, 16);
            panel3.Padding = new Padding(12, 16, 12, 16);
            btnCancel.Dock = DockStyle.Fill;
            button1.Dock = DockStyle.Fill;
        }

        private void PaintCardBorder(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(SharedTheme.Border, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, panel1.Width - 1, panel1.Height - 1);
        }
    }
}
