using System.Drawing.Drawing2D;

namespace Planify.Helpers;

public static class GraphicsHelper
{
    /// <summary>Crée un rectangle aux coins arrondis (rayon en pixels).</summary>
    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(1, radius * 2);
        d = Math.Min(d, Math.Min(bounds.Width, bounds.Height));

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
