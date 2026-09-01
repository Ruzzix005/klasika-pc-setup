using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace KlasikaPCSetup;

internal static class AppTheme
{
    internal static readonly Color Accent = Color.FromArgb(80, 175, 176);
    internal static readonly Color AccentBright = Color.FromArgb(101, 197, 198);
    internal static readonly Color Charcoal = Color.FromArgb(9, 12, 15);
    internal static readonly Color Background = Color.FromArgb(13, 17, 21);
    internal static readonly Color Surface = Color.FromArgb(21, 27, 32);
    internal static readonly Color SurfaceRaised = Color.FromArgb(26, 34, 40);
    internal static readonly Color Muted = Color.FromArgb(152, 165, 174);
    internal static readonly Color Border = Color.FromArgb(41, 52, 60);

    internal static void PrimaryButton(Button button)
    {
        if (button is ForgeButton forge) forge.IsPrimary = true;
        StyleButton(button, Accent, Color.White, 0);
    }

    internal static void SecondaryButton(Button button)
    {
        if (button is ForgeButton forge) forge.IsPrimary = false;
        StyleButton(button, Surface, Color.WhiteSmoke, 1);
    }

    private static void StyleButton(Button button, Color back, Color fore, int border)
    {
        button.BackColor = back; button.ForeColor = fore; button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Accent; button.FlatAppearance.BorderSize = border;
        button.FlatAppearance.MouseOverBackColor = border == 0 ? AccentBright : SurfaceRaised;
        button.Cursor = Cursors.Hand; button.Font = new Font("Segoe UI Semibold", button.Font.Size);
    }

    internal static void ApplyDarkTitleBar(Form form)
    {
        if (!OperatingSystem.IsWindows()) return;
        var enabled = 1;
        DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 19, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}

internal sealed class ForgeButton : Button
{
    private bool hovered;
    internal bool IsPrimary { get; set; }

    internal ForgeButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = RoundedRectangle(rect, 9);
        var background = !Enabled ? AppTheme.SurfaceRaised : IsPrimary ? (hovered ? AppTheme.AccentBright : AppTheme.Accent) : (hovered ? AppTheme.SurfaceRaised : AppTheme.Surface);
        using var brush = new SolidBrush(background);
        e.Graphics.FillPath(brush, path);
        if (!IsPrimary)
        {
            using var border = new Pen(Enabled ? AppTheme.Accent : AppTheme.Border);
            e.Graphics.DrawPath(border, path);
        }
        var textColor = Enabled ? Color.White : Color.FromArgb(105, 116, 124);
        TextRenderer.DrawText(e.Graphics, Text, Font, rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private static GraphicsPath RoundedRectangle(Rectangle r, int radius)
    {
        var d = radius * 2; var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90); path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }
}

internal sealed class ForgeCard : Panel
{
    internal ForgeCard()
    {
        BackColor = AppTheme.Surface;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = RoundedRectangle(ClientRectangle, 14);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRectangle(new Rectangle(1, 1, Width - 3, Height - 3), 14);
        using var pen = new Pen(AppTheme.Border);
        e.Graphics.DrawPath(pen, path);
        base.OnPaint(e);
    }

    private static GraphicsPath RoundedRectangle(Rectangle r, int radius)
    {
        var d = radius * 2; var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90); path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }
}

internal sealed class ForgeCheckBox : CheckBox
{
    internal ForgeCheckBox()
    {
        AutoSize = false; Height = 38; ForeColor = Color.WhiteSmoke; BackColor = Color.Transparent;
        Font = new Font("Segoe UI", 10.5f); Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? AppTheme.Surface);
        var box = new Rectangle(2, 8, 22, 22);
        using var background = new SolidBrush(Checked ? AppTheme.Accent : AppTheme.SurfaceRaised);
        using var border = new Pen(Checked ? AppTheme.AccentBright : AppTheme.Border);
        e.Graphics.FillRectangle(background, box); e.Graphics.DrawRectangle(border, box);
        if (Checked)
        {
            using var checkPen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.DrawLines(checkPen, new Point[] { new(7, 19), new(12, 24), new(20, 13) });
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(37, 0, Width - 39, Height), Enabled ? ForeColor : AppTheme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}
