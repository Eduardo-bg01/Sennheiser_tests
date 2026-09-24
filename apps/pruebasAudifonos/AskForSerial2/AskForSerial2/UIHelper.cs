using System;
using System.Drawing;
using System.Windows.Forms;

/// <summary>
/// Centralized UI helper methods for consistent styling across all test applications.
/// Reduces code duplication and improves maintainability.
/// </summary>
public static class UIHelper
{
    public static void StylePrimaryButton(Button btn) => StyleButton(btn, SharedTheme.Accent, true);
    public static void StyleSecondaryButton(Button btn) =>
        StyleButton(btn, SharedTheme.BgCard, false, SharedTheme.Border);
    public static void StyleSuccessButton(Button btn) => StyleButton(btn, SharedTheme.Success, true);
    public static void StyleDangerButton(Button btn) => StyleButton(btn, SharedTheme.Danger, true);

    private static void StyleButton(Button btn, Color back, bool bold, Color? border = null)
    {
        btn.BackColor = back;
        btn.ForeColor = back == SharedTheme.BgCard ? SharedTheme.TextPrimary : Color.White;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = border.HasValue ? 1 : 0;
        if (border.HasValue) btn.FlatAppearance.BorderColor = border.Value;
        btn.Font = new Font("Segoe UI", 11F, bold ? FontStyle.Bold : FontStyle.Regular);
        btn.Cursor = Cursors.Hand;
    }

    public static void ApplyThemeToControl(Control control)
    {
        if (control is Button btn)
        {
            if (btn.Tag?.ToString() == "primary") StylePrimaryButton(btn);
            else if (btn.Tag?.ToString() == "success") StyleSuccessButton(btn);
            else if (btn.Tag?.ToString() == "danger") StyleDangerButton(btn);
            else StyleSecondaryButton(btn);
        }
        else if (control is Label lbl)
        {
            lbl.ForeColor = SharedTheme.TextPrimary;
            lbl.BackColor = Color.Transparent;
        }
        else if (control is TextBox tb)
        {
            tb.BackColor = SharedTheme.BgCard;
            tb.ForeColor = SharedTheme.TextPrimary;
            tb.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is ComboBox cb)
        {
            cb.BackColor = SharedTheme.BgCard;
            cb.ForeColor = SharedTheme.TextPrimary;
        }
        else if (control is ListBox lb)
        {
            lb.BackColor = SharedTheme.BgCard;
            lb.ForeColor = SharedTheme.TextPrimary;
        }

        foreach (Control child in control.Controls)
            ApplyThemeToControl(child);
    }

    public static void StyleFormBackground(Form form)
    {
        form.BackColor = SharedTheme.BgApp;
    }
}
