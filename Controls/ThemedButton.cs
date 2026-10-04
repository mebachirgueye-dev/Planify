using System.Drawing.Drawing2D;
using Planify.Helpers;

namespace Planify.Controls;

public enum ButtonKind
{
    /// <summary>Action principale : fond bleu.</summary>
    Primary,
    /// <summary>Action secondaire : fond blanc, bordure grise.</summary>
    Secondary,
    /// <summary>Action destructrice : texte rouge.</summary>
    Danger
}

/// <summary>Bouton plat aux coins arrondis, aux couleurs de Planify.</summary>
public class ThemedButton : Button
{
    private ButtonKind _kind = ButtonKind.Secondary;
    private bool _hover;
    private bool _pressed;

    public ThemedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = Theme.BodyBold;
        Height = Theme.Px(38);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public ButtonKind Kind
    {
        get => _kind;
        set { _kind = value; Invalidate(); }
    }

    protected override bool ShowFocusCues => false;

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs mevent) { _pressed = true; Invalidate(); base.OnMouseDown(mevent); }
    protected override void OnMouseUp(MouseEventArgs mevent) { _pressed = false; Invalidate(); base.OnMouseUp(mevent); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color fill, border, text;
        switch (_kind)
        {
            case ButtonKind.Primary:
                fill = _hover || _pressed ? Theme.PrimaryHover : Theme.Primary;
                border = fill;
                text = Color.White;
                break;
            case ButtonKind.Danger:
                fill = _hover || _pressed ? Color.FromArgb(254, 242, 242) : Theme.Surface;
                border = Color.FromArgb(252, 165, 165);
                text = Theme.Danger;
                break;
            default:
                fill = _hover || _pressed ? Theme.HoverBackground : Theme.Surface;
                border = Theme.Border;
                text = Theme.Navy;
                break;
        }

        if (!Enabled)
        {
            fill = _kind == ButtonKind.Primary ? Theme.PrimaryLight : Theme.Background;
            border = Theme.Border;
            text = Theme.TextMuted;
        }

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = GraphicsHelper.RoundedRectangle(rect, Theme.Px(8)))
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}
