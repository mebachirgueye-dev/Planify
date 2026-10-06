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
    Danger,
    /// <summary>Action fantôme : sans fond, texte coloré.</summary>
    Ghost
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
        Height = Theme.Px(40);
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
        var radius = Theme.Radius(Theme.RadiusSmall);

        switch (_kind)
        {
            case ButtonKind.Primary:
                if (_pressed) fill = Theme.PrimaryPressed;
                else if (_hover) fill = Theme.PrimaryHover;
                else fill = Theme.Primary;
                border = fill;
                text = Color.White;
                break;
            case ButtonKind.Danger:
                if (_pressed) fill = Theme.DangerLight;
                else if (_hover) fill = Color.FromArgb(255, 245, 245);
                else fill = Theme.Surface;
                border = _hover || _pressed ? Theme.Danger : Theme.BorderLight;
                text = Theme.Danger;
                break;
            case ButtonKind.Ghost:
                if (_pressed) fill = Theme.HoverBackground;
                else if (_hover) fill = Theme.PrimarySoft;
                else fill = Color.Transparent;
                border = Color.Transparent;
                text = _hover || _pressed ? Theme.Primary : Theme.TextSecondary;
                break;
            default: // Secondary
                if (_pressed) fill = Theme.Background;
                else if (_hover) fill = Theme.SurfaceHover;
                else fill = Theme.Surface;
                border = _hover ? Theme.PrimaryLight : Theme.BorderLight;
                text = Theme.Navy;
                break;
        }

        if (!Enabled)
        {
            fill = _kind == ButtonKind.Primary ? Theme.PrimaryLight : Theme.Background;
            border = Theme.BorderLight;
            text = Theme.TextMuted;
        }

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = GraphicsHelper.RoundedRectangle(rect, radius))
        {
            // Fond
            using (var brush = new SolidBrush(fill))
                g.FillPath(brush, path);

            // Bordure
            if (border != Color.Transparent)
                using (var pen = new Pen(border, _kind == ButtonKind.Ghost ? 0 : 1))
                    g.DrawPath(pen, path);
        }

        // Texte
        var textRect = ClientRectangle;
        if (_pressed) textRect.Offset(1, 1); // léger décalage au clic
        TextRenderer.DrawText(g, Text, Font, textRect, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}
