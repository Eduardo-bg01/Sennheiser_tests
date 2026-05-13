namespace AudioTest
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            tableLayoutPanel1 = new TableLayoutPanel();
            panelContainer = new Panel();
            content3 = new Panel();
            label4 = new Label();
            pictureBox2 = new PictureBox();
            label2 = new Label();
            tableLayoutPanel3 = new TableLayoutPanel();
            panel4 = new Panel();
            btnPass = new Button();
            panel1 = new Panel();
            btnFail = new Button();
            content2 = new Panel();
            pictureBox1 = new PictureBox();
            label3 = new Label();
            content1 = new Panel();
            //START-MOD FONG
            labelHeadphones = new Label();
            labelSpeakers = new Label();
            labelMicrophones = new Label();

            comboHeadphones = new ComboBox();
            comboSpeakers = new ComboBox();
            comboMicrophones = new ComboBox();
            //END-MOD FONG
            label1 = new Label();
            tableLayoutPanel2 = new TableLayoutPanel();
            panel3 = new Panel();
            btnNext = new Button();
            panel2 = new Panel();
            btnCancel = new Button();
            tableLayoutPanel1.SuspendLayout();
            panelContainer.SuspendLayout();
            content3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            tableLayoutPanel3.SuspendLayout();
            panel4.SuspendLayout();
            panel1.SuspendLayout();
            content2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            content1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            panel3.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.BackColor = Color.FromArgb(224, 224, 224);
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(panelContainer, 0, 1);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 80F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            tableLayoutPanel1.Size = new Size(1169, 657);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // panelContainer
            // 
            panelContainer.Controls.Add(content3);
            panelContainer.Controls.Add(content2);
            panelContainer.Controls.Add(content1);
            panelContainer.Dock = DockStyle.Fill;
            panelContainer.Location = new Point(3, 68);
            panelContainer.Name = "panelContainer";
            panelContainer.Size = new Size(1163, 519);
            panelContainer.TabIndex = 2;
            // 
            // content3
            // 
            content3.BackColor = Color.White;
            content3.Controls.Add(label4);
            content3.Controls.Add(pictureBox2);
            content3.Controls.Add(label2);
            content3.Controls.Add(tableLayoutPanel3);
            content3.Dock = DockStyle.Fill;
            content3.Location = new Point(0, 0);
            content3.Name = "content3";
            content3.Padding = new Padding(10);
            content3.Size = new Size(1163, 519);
            content3.TabIndex = 2;
            content3.Visible = false;
            // 
            // label4
            // 
            label4.Dock = DockStyle.Bottom;
            label4.Font = new Font("Segoe UI", 22.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.Location = new Point(10, 332);
            label4.Name = "label4";
            label4.Size = new Size(1143, 52);
            label4.TabIndex = 3;
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBox2
            // 
            pictureBox2.Dock = DockStyle.Fill;
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(10, 82);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(1143, 302);
            pictureBox2.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBox2.TabIndex = 2;
            pictureBox2.TabStop = false;
            // 
            // label2
            // 
            label2.Dock = DockStyle.Top;
            label2.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(10, 10);
            label2.Name = "label2";
            label2.Size = new Size(1143, 72);
            label2.TabIndex = 1;
            label2.Text = "Preste atencion al audio (7)";
            label2.TextAlign = ContentAlignment.TopCenter;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Controls.Add(panel4, 1, 0);
            tableLayoutPanel3.Controls.Add(panel1, 0, 0);
            tableLayoutPanel3.Dock = DockStyle.Bottom;
            tableLayoutPanel3.Location = new Point(10, 384);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Size = new Size(1143, 125);
            tableLayoutPanel3.TabIndex = 2;
            // 
            // panel4
            // 
            panel4.Controls.Add(btnPass);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(574, 3);
            panel4.Name = "panel4";
            panel4.Padding = new Padding(10);
            panel4.Size = new Size(566, 119);
            panel4.TabIndex = 1;
            // 
            // btnPass
            // 
            btnPass.BackColor = Color.PaleGreen;
            btnPass.Dock = DockStyle.Left;
            btnPass.Enabled = false;
            btnPass.FlatAppearance.BorderSize = 0;
            btnPass.FlatStyle = FlatStyle.Flat;
            btnPass.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnPass.ForeColor = Color.FromArgb(0, 64, 0);
            btnPass.Location = new Point(10, 10);
            btnPass.Name = "btnPass";
            btnPass.Size = new Size(268, 99);
            btnPass.TabIndex = 2;
            btnPass.Text = "Si";
            btnPass.UseVisualStyleBackColor = false;
            btnPass.Visible = false;
            btnPass.Click += btnPass_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnFail);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 3);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(10);
            panel1.Size = new Size(565, 119);
            panel1.TabIndex = 0;
            // 
            // btnFail
            // 
            btnFail.BackColor = Color.DarkSalmon;
            btnFail.Dock = DockStyle.Right;
            btnFail.Enabled = false;
            btnFail.FlatAppearance.BorderSize = 0;
            btnFail.FlatStyle = FlatStyle.Flat;
            btnFail.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnFail.ForeColor = Color.Red;
            btnFail.Location = new Point(287, 10);
            btnFail.Name = "btnFail";
            btnFail.Size = new Size(268, 99);
            btnFail.TabIndex = 3;
            btnFail.Text = "No";
            btnFail.UseVisualStyleBackColor = false;
            btnFail.Visible = false;
            btnFail.Click += btnFail_Click;
            // 
            // content2
            // 
            content2.BackColor = Color.White;
            content2.Controls.Add(pictureBox1);
            content2.Controls.Add(label3);
            content2.Dock = DockStyle.Fill;
            content2.Location = new Point(0, 0);
            content2.Name = "content2";
            content2.Padding = new Padding(10);
            content2.Size = new Size(1163, 519);
            content2.TabIndex = 2;
            content2.Visible = false;
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(10, 82);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(1143, 427);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // label3
            // 
            label3.Dock = DockStyle.Top;
            label3.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(10, 10);
            label3.Name = "label3";
            label3.Size = new Size(1143, 72);
            label3.TabIndex = 1;
            label3.Text = "Coloque los audifonos sobre el dispositivo DSP";
            label3.TextAlign = ContentAlignment.TopCenter;
            // 
            // content1
            // 
            content1.BackColor = Color.White;
            //START-MOD FONG
            content1.Controls.Add(comboMicrophones);
            content1.Controls.Add(labelMicrophones);

            content1.Controls.Add(comboSpeakers);
            content1.Controls.Add(labelSpeakers);

            content1.Controls.Add(comboHeadphones);
            content1.Controls.Add(labelHeadphones);
            
            content1.Controls.Add(label1);
            //END-MOD FONG
            content1.Dock = DockStyle.Fill;
            content1.Location = new Point(0, 0);
            content1.Name = "content1";
            content1.Padding = new Padding(10);
            content1.Size = new Size(1163, 519);
            content1.TabIndex = 0;
            // 
            // label1
            // 
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(10, 10);
            label1.Name = "label1";
            label1.Size = new Size(1143, 72);
            label1.TabIndex = 0;
            label1.Text = "Seleccione su dispositivo de salida";
            label1.TextAlign = ContentAlignment.TopCenter;
            //START-MOD FONG
            //
            // labelHeadphones
            //
            labelHeadphones.Text = "Seleccione dispositivo de audífonos";
            labelHeadphones.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            labelHeadphones.AutoSize = false;
            labelHeadphones.Height = 40;
            labelHeadphones.Width = 900;

            //
            // labelSpeakers
            //
            labelSpeakers.Text = "Seleccione bocinas externas";
            labelSpeakers.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            labelSpeakers.AutoSize = false;
            labelSpeakers.Height = 40;
            labelSpeakers.Width = 900;

            //
            // labelMicrophones
            //
            labelMicrophones.Text = "Seleccione micrófono de orejas";
            labelMicrophones.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            labelMicrophones.AutoSize = false;
            labelMicrophones.Height = 40;
            labelMicrophones.Width = 900;

            //
            // comboHeadphones
            //
            comboHeadphones.DropDownStyle = ComboBoxStyle.DropDownList;
            comboHeadphones.Font = new Font("Segoe UI", 13F);
            comboHeadphones.Width = 900;

            //
            // comboSpeakers
            //
            comboSpeakers.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSpeakers.Font = new Font("Segoe UI", 13F);
            comboSpeakers.Width = 900;

            //
            // comboMicrophones
            //
            comboMicrophones.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMicrophones.Font = new Font("Segoe UI", 13F);
            comboMicrophones.Width = 900;
            //END-MOD FONG
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Controls.Add(panel3, 1, 0);
            tableLayoutPanel2.Controls.Add(panel2, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 593);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(1163, 61);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // panel3
            // 
            panel3.Controls.Add(btnNext);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(584, 3);
            panel3.Name = "panel3";
            panel3.Padding = new Padding(10);
            panel3.Size = new Size(576, 55);
            panel3.TabIndex = 1;
            // 
            // btnNext
            // 
            btnNext.BackColor = SystemColors.InactiveCaption;
            btnNext.Dock = DockStyle.Left;
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnNext.Location = new Point(10, 10);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(268, 35);
            btnNext.TabIndex = 2;
            btnNext.Text = "Siguiente";
            btnNext.UseVisualStyleBackColor = false;
            btnNext.Click += btnNext_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(btnCancel);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(3, 3);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(10);
            panel2.Size = new Size(575, 55);
            panel2.TabIndex = 0;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = SystemColors.InactiveCaption;
            btnCancel.Dock = DockStyle.Right;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.Location = new Point(297, 10);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(268, 35);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1169, 657);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "Form1";
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            Shown += Form1_Shown;
            tableLayoutPanel1.ResumeLayout(false);
            panelContainer.ResumeLayout(false);
            content3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            tableLayoutPanel3.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel1.ResumeLayout(false);
            content2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            content1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private Panel content1;
        private Label label1;
        private TableLayoutPanel tableLayoutPanel2;
        private Panel panel3;
        private Button btnNext;
        private Panel panel2;
        private Button btnCancel;
        private Panel panelContainer;
        private Panel content3;
        private Label label2;
        private Panel content2;
        private PictureBox pictureBox1;
        private Label label3;
        private PictureBox pictureBox2;
        private TableLayoutPanel tableLayoutPanel3;
        private Panel panel4;
        private Button btnPass;
        private Panel panel1;
        private Button btnFail;
        private Label label4;
        //
        //AGREGADO POR FONG
        //
        private Label labelHeadphones;
        private Label labelSpeakers;
        private Label labelMicrophones;

        private ComboBox comboHeadphones;
        private ComboBox comboSpeakers;
        private ComboBox comboMicrophones;
    }
}
