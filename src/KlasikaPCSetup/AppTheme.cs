namespace KlasikaPCSetup;

internal static class AppTheme
{
    internal static readonly Color Accent = Color.FromArgb(232, 75, 42);
    internal static readonly Color AccentDark = Color.FromArgb(196, 53, 27);
    internal static readonly Color Charcoal = Color.FromArgb(35, 35, 35);
    internal static readonly Color Background = Color.FromArgb(244, 245, 246);
    internal static readonly Color Surface = Color.White;
    internal static readonly Color Muted = Color.FromArgb(100, 105, 110);
    internal static readonly Color Border = Color.FromArgb(218, 220, 223);

    internal static void PrimaryButton(Button button)
    {
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI Semibold", button.Font.Size);
    }

    internal static void SecondaryButton(Button button)
    {
        button.BackColor = Surface;
        button.ForeColor = Charcoal;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI Semibold", button.Font.Size);
    }
}
