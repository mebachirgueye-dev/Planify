using System.Drawing.Drawing2D;
using Planify.Helpers;

namespace Planify.Controls;

/// <summary>Panneau "carte" : fond blanc, bordure fine, coins arrondis, ombre subtile.</summary>
public class CardPanel : Panel
{
    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Theme.Surface;
        Padding = new Padding(Theme.Spacing(16));
        Margin = new Padding(0, 0, Theme.Spacing(4), Theme.Spacing(4));
        DoubleBuffered = true; // Réduit le scintillement
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = Theme.Radius(Theme.RadiusCard);

        // Ombre portée subtile
        using (var shadowPath = GraphicsHelper.RoundedRectangle(
            new Rectangle(rect.X + Theme.Radius(2), rect.Y + Theme.Radius(2), rect.Width - Theme.Radius(4), rect.Height - Theme.Radius(4)), radius))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(25, Theme.ShadowColor)))
        {
            g.FillPath(shadowBrush, shadowPath);
        }

        // Carte principale
        using (var path = GraphicsHelper.RoundedRectangle(rect, radius))
        using (var fill = new SolidBrush(BackColor))
        using (var pen = new Pen(Theme.BorderLight))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }
    }
}
