using System.Drawing.Drawing2D;
using Planify.Helpers;

namespace Planify.Controls;

/// <summary>Carte statistique du Dashboard : titre, grande valeur, légende et barre d'accent latérale.</summary>
public class StatCard : Control
{
    private const TextFormatFlags TextFlags =
        TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.SingleLine |
        TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

    private string _title = string.Empty;
    private string _value = "—";
    private string _caption = string.Empty;
    private Color _accentColor = Theme.Primary;

    public StatCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(Theme.Px(240), Theme.Px(124));
        Margin = new Padding(0, 0, Theme.Spacing(8), Theme.Spacing(8));
    }

    public string Title { get => _title; set { _title = value; Invalidate(); } }
    public string Value { get => _value; set { _value = value; Invalidate(); } }
    public string Caption { get => _caption; set { _caption = value; Invalidate(); } }
    public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = Theme.Radius(Theme.RadiusCard);
        var accentWidth = Theme.Px(5);

        // Ombre
        using (var shadowPath = GraphicsHelper.RoundedRectangle(
            new Rectangle(rect.X + Theme.Radius(2), rect.Y + Theme.Radius(2), rect.Width - Theme.Radius(4), rect.Height - Theme.Radius(4)), radius))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(20, Theme.ShadowColor)))
        {
            g.FillPath(shadowBrush, shadowPath);
        }

        // Fond carte
        using (var path = GraphicsHelper.RoundedRectangle(rect, radius))
        using (var fill = new SolidBrush(Theme.Surface))
        using (var pen = new Pen(Theme.BorderLight))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        // Barre d'accent verticale à gauche
        using (var accentPath = GraphicsHelper.RoundedRectangle(
            new Rectangle(0, 0, accentWidth, Height), radius))
        using (var accentBrush = new SolidBrush(_accentColor))
        {
            g.FillPath(accentBrush, accentPath);
        }

        int pad = Theme.Spacing(20);
        int textWidth = Width - pad - accentWidth - Theme.Spacing(12);

        TextRenderer.DrawText(g, _title, Theme.Small, new Rectangle(pad + accentWidth + Theme.Spacing(12), Theme.Px(14), textWidth, Theme.Px(20)),
            Theme.TextMuted, TextFlags);
        TextRenderer.DrawText(g, _value, Theme.BigNumber, new Rectangle(pad + accentWidth + Theme.Spacing(12), Theme.Px(34), textWidth, Theme.Px(50)),
            Theme.Navy, TextFlags);
        TextRenderer.DrawText(g, _caption, Theme.Small, new Rectangle(pad + accentWidth + Theme.Spacing(12), Height - Theme.Px(36), textWidth, Theme.Px(20)),
            Theme.TextMuted, TextFlags);
    }
}
