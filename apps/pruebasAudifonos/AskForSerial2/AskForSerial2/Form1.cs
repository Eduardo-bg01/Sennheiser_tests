namespace AskForSerial2
{
    public partial class Form1 : Form
    {
        public string serial;

        public Form1()
        {
            InitializeComponent();
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
    }
}
