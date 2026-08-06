using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BluetoothHeadphoneTest
{
    public class SummaryPanel : Panel
    {
        public event Action<BluetoothDeviceInfo> OnRestart;

        private static readonly Color BgDark = ColorTranslator.FromHtml("#F4F7FC");
        private static readonly Color BgCard = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color BgRow = ColorTranslator.FromHtml("#F0F4FB");
        private static readonly Color BgRowAlt = ColorTranslator.FromHtml("#E8EEF8");
        private static readonly Color AccentCyan = ColorTranslator.FromHtml("#0099BB");
        private static readonly Color AccentGreen = ColorTranslator.FromHtml("#00A85A");
        private static readonly Color AccentRed = ColorTranslator.FromHtml("#CC2222");
        private static readonly Color AccentYellow = ColorTranslator.FromHtml("#D4A000");
        private static readonly Color TextPrimary = ColorTranslator.FromHtml("#1A2640");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#5A6F90");
        private static readonly Color BorderColor = ColorTranslator.FromHtml("#C8D4E8");

        public SummaryPanel(TestSession session)
        {
            BackColor = BgDark;
            Dock = DockStyle.Fill;

            bool passed = session.AllPassed;
            int passCount = 0;
            foreach (var r in session.Records) if (r.Result == TestResult.Pass) passCount++;
            var duration = DateTime.Now - session.StartTime;

            // Guardar reporte automáticamente en el escritorio
            AutoSaveTxtReport(session, passed, passCount, duration);

            // ── Scrollable card ──────────────────────────────────────────────
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BgDark,
                Padding = new Padding(20)
            };

            var card = new Panel
            {
                BackColor = BgCard,
                Width = 860,
                Height = 520,
                Location = new Point(20, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            scroll.Controls.Add(card);
            scroll.Resize += (s, e) =>
            {
                card.Width = Math.Max(600, scroll.ClientSize.Width - 40);
                RelayoutCard(card);
            };

            Controls.Add(scroll);
            BuildCard(card, session, passed, passCount, duration);
        }

        private void BuildCard(Panel card, TestSession session,
                               bool passed, int passCount, TimeSpan duration)
        {
            card.Controls.Clear();
            int w = card.Width;
            int y = 16;

            // ── Dispositivo y MAC ────────────────────────────────────────────
            var lblDev = new Label
            {
                Text = $"Dispositivo: {session.SelectedDevice?.Name ?? "—"}",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = false,
                Size = new Size(w - 40, 28),
                Location = new Point(20, y),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lblDev);
            y += 30;

            var lblMac = new Label
            {
                Text = $"MAC: {session.SelectedDevice?.Address ?? "—"}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(w - 40, 24),
                Location = new Point(20, y),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lblMac);
            y += 32;

            // ── Separator ────────────────────────────────────────────────────
            var sep = new Panel
            {
                BackColor = BorderColor,
                Location = new Point(20, y),
                Size = new Size(w - 40, 1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(sep);
            y += 12;

            // ── Column headers ───────────────────────────────────────────────
            var hdrPanel = new Panel
            {
                BackColor = ColorTranslator.FromHtml("#E8EEF8"),
                Location = new Point(20, y),
                Size = new Size(w - 40, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            AddRowLabel(hdrPanel, "PRUEBA", 0, w - 280, new Font("Segoe UI", 9f, FontStyle.Bold), AccentCyan);
            AddRowLabel(hdrPanel, "RESULTADO", w - 280, 120, new Font("Segoe UI", 9f, FontStyle.Bold), AccentCyan, ContentAlignment.MiddleCenter);
            AddRowLabel(hdrPanel, "HORA", w - 160, 140, new Font("Segoe UI", 9f, FontStyle.Bold), AccentCyan, ContentAlignment.MiddleRight);
            card.Controls.Add(hdrPanel);
            y += 34;

            // ── Result rows ──────────────────────────────────────────────────
            int i = 0;
            foreach (var rec in session.Records)
            {
                bool ok = rec.Result == TestResult.Pass;
                bool na = rec.Result == TestResult.NotApplicable;
                bool fail = rec.Result == TestResult.Fail;

                var row = new Panel
                {
                    BackColor = i % 2 == 0 ? BgRow : BgRowAlt,
                    Location = new Point(20, y),
                    Size = new Size(w - 40, 42),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                // Barra de color lateral
                Color accentColor = ok ? AccentGreen : (na ? BorderColor : (fail ? AccentRed : TextMuted));
                var accent = new Panel
                {
                    BackColor = accentColor,
                    Location = new Point(0, 0),
                    Size = new Size(4, 42)
                };
                row.Controls.Add(accent);

                // Nombre de la prueba (atenuado si N/A)
                AddRowLabel(row, rec.Name, 14, w - 300,
                    new Font("Segoe UI", 10.5f, na ? FontStyle.Italic : FontStyle.Regular),
                    na ? TextMuted : TextPrimary);

                // Resultado
                string resText = ok ? "✔  PASS" : (na ? "—  N/A" : (fail ? "✘  FAIL" : "·  Pend."));
                Color resColor = ok ? AccentGreen : (na ? TextMuted : (fail ? AccentRed : TextMuted));
                AddRowLabel(row, resText, w - 280, 120,
                    new Font("Segoe UI", 10f, FontStyle.Bold), resColor,
                    ContentAlignment.MiddleCenter);

                // Hora
                string timeText = na ? "—" : (rec.Timestamp.HasValue
                    ? rec.Timestamp.Value.ToString("HH:mm:ss") : "--:--:--");
                AddRowLabel(row, timeText, w - 160, 140,
                    new Font("Segoe UI", 9.5f), TextMuted,
                    ContentAlignment.MiddleRight);

                card.Controls.Add(row);
                y += 46;
                i++;
            }

            y += 16;

            // ── Buttons ──────────────────────────────────────────────────────
            var btnNew = MakeButton("↺  NUEVA PRUEBA", ColorTranslator.FromHtml("#0099BB"), 20, y, 190, 44);
            var btnClose = MakeButton("SIGUIENTE PRUEBA", ColorTranslator.FromHtml("#CC2222"), 224, y, 160, 44);
            btnNew.Click += (s, e) =>
            {
                var btn = (Control)s;
                btn.FindForm()?.BeginInvoke(new Action(() =>
                {
                    using var selectForm = new DeviceSelectForm();
                    if (selectForm.ShowDialog() == DialogResult.OK)
                        OnRestart?.Invoke(selectForm.SelectedDevice);
                }));
            };
            btnClose.Click += (s, e) => FindForm()?.Close();
            card.Controls.AddRange(new Control[] { btnNew, btnClose });

            card.Height = y + 44 + 20;
        }

        private void RelayoutCard(Panel card)
        {
            // Rebuild card when width changes
            if (card.Tag is TestSession session)
                BuildCard(card, session,
                    session.AllPassed,
                    CountPassed(session),
                    DateTime.Now - session.StartTime);
        }

        private int CountPassed(TestSession s)
        {
            int c = 0;
            foreach (var r in s.Records) if (r.Result == TestResult.Pass) c++;
            return c;
        }

        private void AddRowLabel(Panel parent, string text, int x, int width,
            Font font, Color color,
            ContentAlignment align = ContentAlignment.MiddleLeft)
        {
            var lbl = new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                AutoSize = false,
                Size = new Size(width, parent.Height),
                Location = new Point(x, 0),
                TextAlign = align,
                Padding = new Padding(align == ContentAlignment.MiddleLeft ? 8 : 0, 0, 4, 0)
            };
            parent.Controls.Add(lbl);
        }

        private Button MakeButton(string text, Color bg, int x, int y, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(w, h),
                Location = new Point(x, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ── Auto-guardado en escritorio ──────────────────────────────────────
        private void AutoSaveTxtReport(TestSession session, bool passed,
                                       int passCount, TimeSpan duration)
        {
            try
            {
                string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
                string currentFolder = Directory.GetCurrentDirectory();
                string fileName = $"Prueba_{session.SelectedDevice?.Name ?? "BT"}_{session.StartTime:yyyyMMdd_HHmm}.txt";
                string baseFilePath = Path.Combine(baseFolder, fileName);
                string currentFilePath = Path.Combine(currentFolder, fileName);

                var sb = new System.Text.StringBuilder();
                string sep = new string('─', 54);

                var selected = session.SelectedDevice;
                string displayName = selected != null && selected.IsWired && !string.IsNullOrWhiteSpace(selected.SelectedJackModel)
                    ? selected.SelectedJackModel
                    : selected?.Name ?? "—";
                sb.AppendLine($"Dispositivo : {displayName}");
                sb.AppendLine($"MAC         : {session.SelectedDevice?.Address ?? "—"}");
                sb.AppendLine($"Fecha       : {session.StartTime:dd/MM/yyyy  HH:mm}");
                sb.AppendLine();
                sb.AppendLine(sep);
                sb.AppendLine($"  {"PRUEBA",-32} {"RESULTADO",-10} {"HORA"}");
                sb.AppendLine(sep);

                foreach (var rec in session.Records)
                {
                    string res = rec.Result == TestResult.Pass ? "PASS" :
                                 rec.Result == TestResult.Fail ? "FAIL" :
                                 rec.Result == TestResult.NotApplicable ? "N/A" : "PEND";
                    string time = rec.Result == TestResult.NotApplicable ? "—"
                        : (rec.Timestamp.HasValue
                            ? rec.Timestamp.Value.ToString("HH:mm:ss") : "--:--:--");
                    sb.AppendLine($"  {rec.Name,-32} {res,-10} {time}");
                }

                sb.AppendLine(sep);
                int totalApplicable = 0, naCount = 0;
                foreach (var r in session.Records)
                {
                    if (r.Result == TestResult.NotApplicable) naCount++;
                    else totalApplicable++;
                }

                sb.AppendLine(sep);
                sb.AppendLine($"  Resultado final: {(passed ? "APROBADO" : "FALLIDO")}  ({passCount}/{totalApplicable})  •  N/A: {naCount}");

                File.WriteAllText(baseFilePath, sb.ToString(), System.Text.Encoding.UTF8);

                if (!string.Equals(baseFilePath, currentFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllText(currentFilePath, sb.ToString(), System.Text.Encoding.UTF8);
                }
            }
            catch { /* Si falla el guardado, continuar sin interrumpir */ }
        }
    }
}