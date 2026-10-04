using System.Drawing.Drawing2D;
using Planify.Helpers;

namespace Planify.Controls;

/// <summary>Entrée du menu latéral. L'entrée active est surlignée en bleu clair avec une barre d'accent.</summary>
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
        Height = Theme.Px(42);
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

        var area = new Rectangle(Theme.Px(12), Theme.Px(2), Width - Theme.Px(24), Height - Theme.Px(4));

        if (_isActive || _hover)
        {
            using var path = GraphicsHelper.RoundedRectangle(area, Theme.Px(8));
            using var brush = new SolidBrush(_isActive ? Theme.PrimarySoft : Theme.HoverBackground);
            g.FillPath(brush, path);
        }

        if (_isActive)
        {
            var bar = new Rectangle(area.X + Theme.Px(4), area.Y + (area.Height - Theme.Px(20)) / 2, Theme.Px(4), Theme.Px(20));
            using var barPath = GraphicsHelper.RoundedRectangle(bar, Theme.Px(2));
            using var barBrush = new SolidBrush(Theme.Primary);
            g.FillPath(barBrush, barPath);
        }

        var textArea = new Rectangle(area.X + Theme.Px(20), area.Y, area.Width - Theme.Px(28), area.Height);
        TextRenderer.DrawText(g, Text, _isActive ? Theme.BodyBold : Theme.Body, textArea,
            _isActive ? Theme.Primary : Theme.Navy,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}
