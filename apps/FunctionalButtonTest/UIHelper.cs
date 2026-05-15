using System;
using System.Drawing;
using System.Windows.Forms;

public static class UIHelper
{
    public static void StylePrimaryButton(Button btn)
    {
        btn.BackColor = SharedTheme.Accent;
        btn.ForeColor = Color.White;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleSecondaryButton(Button btn)
    {
        btn.BackColor = SharedTheme.BgCard;
        btn.ForeColor = SharedTheme.TextPrimary;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = SharedTheme.Border;
        btn.Font = new Font("Segoe UI", 11F);
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleSuccessButton(Button btn)
    {
        btn.BackColor = SharedTheme.Success;
        btn.ForeColor = Color.White;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleDangerButton(Button btn)
    {
        btn.BackColor = SharedTheme.Danger;
        btn.ForeColor = Color.White;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
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
