using System.Drawing.Drawing2D;
using System.Reflection;

namespace Planify.Helpers;

/// <summary>
/// Chargement des ressources intégrées à l'exécutable (dossier Resources/).
/// Les noms sont ceux des fichiers : "logo.png", "logo_mark.png", "logo_wordmark.png", "planify.ico".
/// </summary>
public static class AppResources
{
    private static readonly Assembly Asm = typeof(AppResources).Assembly;

    private static Stream Open(string name) =>
        Asm.GetManifestResourceStream(name)
        ?? throw new FileNotFoundException($"Ressource introuvable : {name}");

    public static Icon LoadIcon(string name)
    {
        using var stream = Open(name);
        return new Icon(stream);
    }

    /// <summary>
    /// Charge une image et la redimensionne en haute qualité à la hauteur demandée
    /// (évite l'aspect crénelé d'un PictureBox qui réduit une grande image).
    /// </summary>
    public static Bitmap LoadImage(string name, int targetHeight)
    {
        using var stream = Open(name);
        using var original = new Bitmap(stream);

        int height = targetHeight;
        int width = (int)Math.Round(original.Width * (double)height / original.Height);

        var result = new Bitmap(width, height);
        using var g = Graphics.FromImage(result);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(original, 0, 0, width, height);
        return result;
    }
}
