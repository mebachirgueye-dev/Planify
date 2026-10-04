using System.Drawing.Drawing2D;
using Planify.Helpers;

namespace Planify.Controls;

/// <summary>Carte statistique du Dashboard : titre, grande valeur, légende et pastille de couleur.</summary>
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
        using (var path = GraphicsHelper.RoundedRectangle(rect, Theme.Px(12)))
        using (var fill = new SolidBrush(Theme.Surface))
        using (var pen = new Pen(Theme.Border))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        int pad = Theme.Px(20);
        int textWidth = Width - 2 * pad;

        // Pastille de couleur en haut à droite
        int dot = Theme.Px(10);
        using (var dotBrush = new SolidBrush(_accentColor))
            g.FillEllipse(dotBrush, Width - pad - dot, pad, dot, dot);

        TextRenderer.DrawText(g, _title, Theme.Small, new Rectangle(pad, Theme.Px(16), textWidth - dot - Theme.Px(8), Theme.Px(20)),
            Theme.TextMuted, TextFlags);
        TextRenderer.DrawText(g, _value, Theme.BigNumber, new Rectangle(pad, Theme.Px(38), textWidth, Theme.Px(48)),
            Theme.Navy, TextFlags);
        TextRenderer.DrawText(g, _caption, Theme.Small, new Rectangle(pad, Height - Theme.Px(34), textWidth, Theme.Px(20)),
            Theme.TextMuted, TextFlags);
    }
}
