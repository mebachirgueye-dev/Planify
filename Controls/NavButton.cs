using System.Drawing.Drawing2D;
using Planify.Helpers;

namespace Planify.Controls;

/// <summary>Entrée du menu latéral. L'entrée active est surlignée avec barre d'accent à gauche.</summary>
public class NavButton : Button
{
    private bool _hover;
    private bool _isActive;

    public NavButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = Theme.Body;
        Height = Theme.Px(44);
        TextAlign = ContentAlignment.MiddleLeft;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Invalidate(); }
    }

    protected override bool ShowFocusCues => false;

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var padding = Theme.Px(16);
        var area = new Rectangle(padding, Theme.Px(4), Width - 2 * padding, Height - Theme.Px(8));
        var radius = Theme.Radius(Theme.RadiusSmall);

        // Fond au survol/actif
        if (_isActive || _hover)
        {
            using var path = GraphicsHelper.RoundedRectangle(area, radius);
            using var brush = new SolidBrush(_isActive ? Theme.PrimarySoft : Theme.HoverBackground);
            g.FillPath(brush, path);
        }

        // Barre d'accent à gauche quand actif
        if (_isActive)
        {
            var barWidth = Theme.Px(4);
            var barHeight = Theme.Px(28);
            var bar = new Rectangle(
                area.X,
                area.Y + (area.Height - barHeight) / 2,
                barWidth, barHeight);
            using var barPath = GraphicsHelper.RoundedRectangle(bar, Theme.Px(2));
            using var barBrush = new SolidBrush(Theme.Primary);
            g.FillPath(barBrush, barPath);
        }

        // Texte
        var textX = area.X + (_isActive ? Theme.Px(16) : Theme.Px(12)) + Theme.Px(4);
        var textArea = new Rectangle(textX, area.Y, area.Width - Theme.Px(20), area.Height);
        TextRenderer.DrawText(g, Text, _isActive ? Theme.BodyBold : Theme.Body, textArea,
            _isActive ? Theme.Primary : Theme.Navy,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}
