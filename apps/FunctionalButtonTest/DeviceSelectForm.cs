using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    public class DeviceSelectForm : Form
    {
        public BluetoothDeviceInfo SelectedDevice { get; private set; }

        private static readonly Color BgDark = ColorTranslator.FromHtml("#F4F7FC");
        private static readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color AccentCyan = ColorTranslator.FromHtml("#0099BB");
        private static readonly Color AccentGreen = ColorTranslator.FromHtml("#00A85A");
        private static readonly Color AccentYellow = ColorTranslator.FromHtml("#D4A000");
        private static readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#5A6F90");
        private static readonly Color BorderColor = ColorTranslator.FromHtml("#C8D4E8");

        private ListBox listDevices;
        private Button btnRefresh;
        private Button btnStart;
        private Label lblStatus;
        private Label lblInstruction;
        private List<BluetoothDeviceInfo> _devices = new List<BluetoothDeviceInfo>();

        public DeviceSelectForm()
        {
            // Pantalla completa sin bordes — igual que MainForm
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.MinimumSize = new Size(600, 480);
            // Sin MaximumSize fijo

            InitUI();
            this.Load += (s, e) => LoadDevices();
            this.Resize += (s, e) => RelayoutControls();
        }

        private void InitUI()
        {
            Text = "Seleccionar Dispositivo Bluetooth";
            BackColor = BgDark;
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 10f);
            MaximizeBox = false;

            // ── Header (Dock=Top, altura fija) ──────────────────────
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = ColorTranslator.FromHtml("#E8EEF8")
            };

            var lblTitle = new Label
            {
                Text = "🔵  SELECCIÓN DE DISPOSITIVO",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = AccentCyan,
                AutoSize = false,
                Dock = DockStyle.None,
                Size = new Size(600, 40),
                Location = new Point(20, 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelHeader.Resize += (s, e) =>
                lblTitle.Size = new Size(panelHeader.Width - 40, 40);

            var lblSub = new Label
            {
                Text = "Seleccione el audífono a probar antes de iniciar la secuencia.",
                Font = new Font("Segoe UI", 11f),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(600, 28),
                Location = new Point(20, 52),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelHeader.Resize += (s, e) =>
                lblSub.Size = new Size(panelHeader.Width - 40, 28);

            panelHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ── Instrucción ─────────────────────────────────────────
            lblInstruction = new Label
            {
                Text = "Dispositivos Bluetooth pareados en este equipo:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = false,
                Size = new Size(520, 28),
                TextAlign = ContentAlignment.MiddleLeft
                // Location se asigna en RelayoutControls()
            };

            // ── Lista de dispositivos ────────────────────────────────
            listDevices = new ListBox
            {
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                ItemHeight = 36,
                DrawMode = DrawMode.OwnerDrawFixed
                // Size y Location en RelayoutControls()
            };
            listDevices.DrawItem += ListDevices_DrawItem;
            listDevices.DoubleClick += (s, e) => TryStart();
            listDevices.SelectedIndexChanged += (s, e) => UpdateButtons();

            // ── Status ──────────────────────────────────────────────
            lblStatus = new Label
            {
                Text = "🔄  Buscando dispositivos...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = AccentYellow,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
                // Location en RelayoutControls()
            };

            // ── Botones ─────────────────────────────────────────────
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

            // ── Ensamblar ────────────────────────────────────────────
            Controls.AddRange(new Control[] {
                panelHeader, lblInstruction, listDevices,
                lblStatus, btnRefresh, btnStart });
        }

        /// <summary>
        /// Reposiciona todos los controles de contenido de forma relativa
        /// al tamaño actual de la ventana. Se llama en Load y en Resize.
        /// </summary>
        private void RelayoutControls()
        {
            // Ancho de contenido: centrado, máximo 600 px, con márgen de 40 px mínimo
            int contentW = Math.Min(600, ClientSize.Width - 80);
            int startX = (ClientSize.Width - contentW) / 2;

            // El header tiene Dock=Top (90 px) — el área libre empieza en Y=90
            int y = 90 + 16;

            lblInstruction.Location = new Point(startX, y);
            lblInstruction.Size = new Size(contentW, 28);
            y += 34;

            // Lista: altura restante hasta los botones (reservar ~140 px abajo)
            int listH = Math.Max(120, ClientSize.Height - y - 140);
            listDevices.Location = new Point(startX, y);
            listDevices.Size = new Size(contentW, listH);
            y += listH + 12;

            lblStatus.Location = new Point(startX, y);
            lblStatus.Size = new Size(contentW, 28);
            y += 38;

            // Botones: Actualizar a la izquierda, Iniciar a la derecha del bloque
            btnRefresh.Location = new Point(startX, y);
            btnStart.Location = new Point(startX + contentW - 200, y);
        }

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
                        listDevices.Items.Add("(No se encontraron dispositivos pareados)");
                        lblStatus.Text = "⚠  No hay dispositivos BT pareados. Parée el audífono primero.";
                        lblStatus.ForeColor = ColorTranslator.FromHtml("#CC2222");
                    }
                    else
                    {
                        foreach (var d in devices)
                            listDevices.Items.Add(d);

                        int connected = 0;
                        foreach (var d in devices) if (d.IsConnected) connected++;

                        lblStatus.Text = connected > 0
                            ? $"✔  {devices.Count} dispositivo(s) encontrado(s), {connected} conectado(s)."
                            : $"ℹ  {devices.Count} dispositivo(s) pareado(s). Ninguno conectado aún.";
                        lblStatus.ForeColor = connected > 0 ? AccentGreen : AccentYellow;
                    }

                    UpdateButtons();
                }));
            });
        }

        private void UpdateButtons()
        {
            bool ok = listDevices.SelectedIndex >= 0
                   && listDevices.SelectedItem is BluetoothDeviceInfo;
            btnStart.Enabled = ok;
            btnStart.BackColor = ok
                ? ColorTranslator.FromHtml("#00A85A")
                : ColorTranslator.FromHtml("#C8D4E8");
        }

        private void TryStart()
        {
            if (listDevices.SelectedItem is BluetoothDeviceInfo dev)
            {
                SelectedDevice = dev;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

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
                using var dotBrush = new SolidBrush(dev.IsConnected ? AccentGreen : BorderColor);
                e.Graphics.FillEllipse(dotBrush,
                    e.Bounds.Left + 12, e.Bounds.Top + 12, 12, 12);

                using var nameBrush = new SolidBrush(TextPrimary);
                e.Graphics.DrawString(dev.Name,
                    new Font("Segoe UI", 11f, FontStyle.Bold), nameBrush,
                    e.Bounds.Left + 34, e.Bounds.Top + 4);

                string sub = dev.IsConnected
                    ? $"{dev.Address}  •  Conectado"
                    : $"{dev.Address}  •  No conectado";
                using var subBrush = new SolidBrush(dev.IsConnected ? AccentGreen : TextMuted);
                e.Graphics.DrawString(sub,
                    new Font("Segoe UI", 8.5f), subBrush,
                    e.Bounds.Left + 34, e.Bounds.Top + 18);
            }
            else
            {
                using var b = new SolidBrush(TextMuted);
                e.Graphics.DrawString(item.ToString(),
                    new Font("Segoe UI", 10f, FontStyle.Italic), b,
                    e.Bounds.Left + 12, e.Bounds.Top + 8);
            }

            e.DrawFocusRectangle();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}