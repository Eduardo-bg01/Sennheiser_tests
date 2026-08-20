using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    public class DeviceSelectForm : Form
    {
        public BluetoothDeviceInfo SelectedDevice { get; private set; }

        private static readonly Color BgDark = AppColors.BgDark;
        private static readonly Color BgCard = AppColors.BgCard;
        private static readonly Color AccentCyan = AppColors.AccentCyan;
        private static readonly Color AccentGreen = AppColors.AccentGreen;
        private static readonly Color AccentYellow = AppColors.AccentYellow;
        private static readonly Color AccentBlue = ColorTranslator.FromHtml("#3B6EC8");
        private static readonly Color TextPrimary = AppColors.TextPrimary;
        private static readonly Color TextMuted = AppColors.TextMuted;
        private static readonly Color BorderColor = AppColors.BorderColor;

        private ListBox listDevices;
        private ComboBox comboJackModel;
        private Label lblJackModel;
        private Panel panelJackModel;
        private Button btnRefresh;
        private Button btnStart;
        private Label lblStatus;
        private Label lblInstruction;

        private List<BluetoothDeviceInfo> _devices = new();

        // Modelos jack: obtenidos del registry
        private List<string> _jackModels = new();

        public DeviceSelectForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            Bounds = Screen.PrimaryScreen.Bounds;
            MinimumSize = new Size(600, 480);

            LoadJackModels();
            InitUI();
            Load += (s, e) => { RelayoutControls(); LoadDevices(); };
            Resize += (s, e) => RelayoutControls();
        }

        // ── Carga los modelos jack registrados ─────────────────────────────────
        private void LoadJackModels()
        {
            _jackModels.Clear();
            foreach (var p in DeviceProfileRegistry.GetWiredProfiles())
                _jackModels.Add(p.ModelName);
        }

        // ── Construcción de UI ─────────────────────────────────────────────────
        private void InitUI()
        {
            Text = "Seleccionar Dispositivo";
            BackColor = BgDark;
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 10f);

            // ── Header ──────────────────────────────────────────────────────
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = ColorTranslator.FromHtml("#E8EEF8")
            };

            var lblTitle = new Label
            {
                Text = "🎧  SELECCIÓN DE DISPOSITIVO",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = AccentCyan,
                AutoSize = false,
                Size = new Size(600, 40),
                Location = new Point(20, 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelHeader.Resize += (s, e) => lblTitle.Size = new Size(panelHeader.Width - 40, 40);

            var lblSub = new Label
            {
                Text = "Seleccione el audífono a probar (Bluetooth o Jack 3.5 mm).",
                Font = new Font("Segoe UI", 11f),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(600, 28),
                Location = new Point(20, 52),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelHeader.Resize += (s, e) => lblSub.Size = new Size(panelHeader.Width - 40, 28);
            panelHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ── Instrucción ──────────────────────────────────────────────────
            lblInstruction = new Label
            {
                Text = "Dispositivos disponibles en este equipo:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = false,
                Size = new Size(520, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ── Lista de dispositivos ────────────────────────────────────────
            listDevices = new ListBox
            {
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                ItemHeight = 40,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            listDevices.DrawItem += ListDevices_DrawItem;
            listDevices.DoubleClick += (s, e) => TryStart();
            listDevices.SelectedIndexChanged += OnDeviceSelectionChanged;

            // ── Panel selección de modelo jack ──────────────────────────────
            panelJackModel = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#EEF4FF"),
                Visible = false,
                Height = 60,
                Padding = new Padding(0, 6, 0, 0)
            };
            panelJackModel.Paint += (s, e) =>
            {
                using var pen = new Pen(ColorTranslator.FromHtml("#B0C4E8"), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, panelJackModel.Width - 1, panelJackModel.Height - 1);
            };

            lblJackModel = new Label
            {
                Text = "Modelo del audífono (Jack):",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = AccentBlue,
                AutoSize = true,
                Location = new Point(8, 8)
            };

            comboJackModel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                ForeColor = TextPrimary,
                Size = new Size(340, 28),
                Location = new Point(8, 28)
            };
            foreach (var m in _jackModels)
                comboJackModel.Items.Add(m);
            if (comboJackModel.Items.Count > 0)
                comboJackModel.SelectedIndex = 0;
            comboJackModel.SelectedIndexChanged += (s, e) => UpdateButtons();

            panelJackModel.Controls.AddRange(new Control[] { lblJackModel, comboJackModel });

            // ── Status ───────────────────────────────────────────────────────
            lblStatus = new Label
            {
                Text = "🔄  Buscando dispositivos...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = AccentYellow,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ── Botones ──────────────────────────────────────────────────────
            btnRefresh = new Button
            {
                Text = "↺  Actualizar",
                Size = new Size(160, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#E8EEF8"),
                ForeColor = ColorTranslator.FromHtml("#1A2640"),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadDevices();

            btnStart = new Button
            {
                Text = "▶  Iniciar Pruebas",
                Size = new Size(200, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#C8D4E8"),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += (s, e) => TryStart();

            // ── Ensamblar ────────────────────────────────────────────────────
            Controls.AddRange(new Control[] {
                panelHeader, lblInstruction, listDevices,
                panelJackModel, lblStatus, btnRefresh, btnStart });
        }

        // ── Layout dinámico ────────────────────────────────────────────────────
        private void RelayoutControls()
        {
            int contentW = Math.Min(600, ClientSize.Width - 80);
            int startX = (ClientSize.Width - contentW) / 2;
            int y = 90 + 16;

            lblInstruction.Location = new Point(startX, y);
            lblInstruction.Size = new Size(contentW, 28);
            y += 34;

            // Lista: reservar espacio para panel jack (60) + status (38) + botones (70) + márgenes
            bool jackVisible = panelJackModel.Visible;
            int reserved = 38 + 70 + (jackVisible ? 70 : 10);
            int listH = Math.Max(120, ClientSize.Height - y - reserved);

            listDevices.Location = new Point(startX, y);
            listDevices.Size = new Size(contentW, listH);
            y += listH + 8;

            // Panel jack
            if (jackVisible)
            {
                panelJackModel.Location = new Point(startX, y);
                panelJackModel.Size = new Size(contentW, 62);
                comboJackModel.Size = new Size(contentW - 16, 28);
                y += 70;
            }

            lblStatus.Location = new Point(startX, y);
            lblStatus.Size = new Size(contentW, 28);
            y += 38;

            btnRefresh.Location = new Point(startX, y);
            btnStart.Location = new Point(startX + contentW - 200, y);
        }

        // ── Carga de dispositivos ──────────────────────────────────────────────
        private void LoadDevices()
        {
            lblStatus.Text = "🔄  Actualizando lista...";
            lblStatus.ForeColor = AccentYellow;

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var devices = BluetoothDetector.GetPairedDevices();
                if (!IsHandleCreated || IsDisposed) return;
                Invoke(new Action(() =>
                {
                    _devices = devices;
                    listDevices.Items.Clear();

                    if (devices.Count == 0)
                    {
                        listDevices.Items.Add("(No se encontraron dispositivos)");
                        lblStatus.Text = "⚠  No hay dispositivos BT pareados ni jack detectado.";
                        lblStatus.ForeColor = ColorTranslator.FromHtml("#CC2222");
                    }
                    else
                    {
                        foreach (var d in devices) listDevices.Items.Add(d);

                        int btConnected = 0, btTotal = 0, jackCount = 0;
                        foreach (var d in devices)
                        {
                            if (d.IsWired) jackCount++;
                            else { btTotal++; if (d.IsConnected) btConnected++; }
                        }

                        var parts = new List<string>();
                        if (btTotal > 0)
                            parts.Add($"{btTotal} BT ({btConnected} conectado(s))");
                        if (jackCount > 0)
                            parts.Add($"{jackCount} Jack 3.5 mm detectado(s)");

                        lblStatus.Text = $"✔  {string.Join("   •   ", parts)}";
                        lblStatus.ForeColor = AccentGreen;
                    }
                    UpdateButtons();
                }));
            });
        }

        // ── Evento: cambio de selección ────────────────────────────────────────
        private void OnDeviceSelectionChanged(object sender, EventArgs e)
        {
            bool isJack = listDevices.SelectedItem is BluetoothDeviceInfo d && d.IsWired;

            panelJackModel.Visible = isJack && _jackModels.Count > 0;

            // Si no hay modelos jack registrados, avisa
            if (isJack && _jackModels.Count == 0)
            {
                lblStatus.Text = "⚠  Audífono jack detectado pero no hay modelos registrados en DeviceProfileRegistry.";
                lblStatus.ForeColor = AccentYellow;
            }

            RelayoutControls();
            UpdateButtons();
        }

        // ── Habilitación del botón Iniciar ─────────────────────────────────────
        private void UpdateButtons()
        {
            bool deviceOk = listDevices.SelectedItem is BluetoothDeviceInfo;
            bool isJack = listDevices.SelectedItem is BluetoothDeviceInfo dv && dv.IsWired;

            // Para jack: también necesita modelo elegido
            bool jackOk = !isJack || (comboJackModel.SelectedItem != null);
            bool ok = deviceOk && jackOk;

            btnStart.Enabled = ok;
            btnStart.BackColor = ok
                ? ColorTranslator.FromHtml("#00A85A")
                : ColorTranslator.FromHtml("#C8D4E8");
        }

        // ── Iniciar ────────────────────────────────────────────────────────────
        private void TryStart()
        {
            if (listDevices.SelectedItem is not BluetoothDeviceInfo dev) return;

            if (dev.IsWired)
            {
                // Asignar el modelo jack elegido por el operador
                if (comboJackModel.SelectedItem is string modelName)
                    dev.SelectedJackModel = modelName;
                else
                {
                    MessageBox.Show("Selecciona el modelo del audífono jack antes de continuar.",
                        "Modelo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            SelectedDevice = dev;
            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Renderizado de filas ───────────────────────────────────────────────
        private void ListDevices_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            var item = listDevices.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;

            var bgColor = selected
                ? ColorTranslator.FromHtml("#D0EAF5")
                : (e.Index % 2 == 0 ? BgCard : ColorTranslator.FromHtml("#F0F4FB"));

            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            if (item is BluetoothDeviceInfo dev)
            {
                bool isWired = dev.IsWired;

                // Ícono de conexión
                string icon = isWired ? "🔌" : (dev.IsConnected ? "🔵" : "⚪");
                using var iconFont = new Font("Segoe UI Emoji", 12f);
                using var iconBrush = new SolidBrush(TextPrimary);
                e.Graphics.DrawString(icon, iconFont, iconBrush,
                    e.Bounds.Left + 8, e.Bounds.Top + 10);

                // Nombre principal
                using var nameBrush = new SolidBrush(TextPrimary);
                e.Graphics.DrawString(dev.Name,
                    new Font("Segoe UI", 11f, FontStyle.Bold), nameBrush,
                    e.Bounds.Left + 40, e.Bounds.Top + 4);

                // Subtítulo
                string sub;
                Color subColor;
                if (isWired)
                {
                    sub = "Jack 3.5 mm  •  Selecciona el modelo abajo";
                    subColor = AccentBlue;
                }
                else
                {
                    sub = dev.IsConnected ? $"{dev.Address}  •  Conectado" : $"{dev.Address}  •  No conectado";
                    subColor = dev.IsConnected ? AccentGreen : TextMuted;
                }

                using var subBrush = new SolidBrush(subColor);
                e.Graphics.DrawString(sub,
                    new Font("Segoe UI", 8.5f), subBrush,
                    e.Bounds.Left + 40, e.Bounds.Top + 22);
            }
            else
            {
                using var b = new SolidBrush(TextMuted);
                e.Graphics.DrawString(item.ToString(),
                    new Font("Segoe UI", 10f, FontStyle.Italic), b,
                    e.Bounds.Left + 12, e.Bounds.Top + 10);
            }

            e.DrawFocusRectangle();
        }

        protected override void Dispose(bool disposing) => base.Dispose(disposing);
    }
}