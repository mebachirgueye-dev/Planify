namespace Planify.Helpers;

/// <summary>
/// Identité graphique de Planify, tirée du logo : bleu calendrier, bleu clair des cases, marine du texte.
/// Toutes les couleurs, polices et tailles communes sont ici : modifier ce fichier change tout le logiciel.
/// </summary>
public static class Theme
{
    // --- Couleurs issues du logo ---
    public static readonly Color Primary = Color.FromArgb(8, 96, 196);        // bleu du calendrier
    public static readonly Color PrimaryHover = Color.FromArgb(6, 78, 160);
    public static readonly Color PrimaryPressed = Color.FromArgb(4, 62, 128);
    public static readonly Color Accent = Color.FromArgb(11, 110, 230);       // case mise en évidence
    public static readonly Color PrimarySoft = Color.FromArgb(232, 240, 251); // fond bleu très clair
    public static readonly Color PrimaryLight = Color.FromArgb(182, 205, 234);// cases du calendrier
    public static readonly Color Navy = Color.FromArgb(11, 31, 59);           // texte "Planify"

    // --- Neutres ---
    public static readonly Color Background = Color.FromArgb(241, 245, 249);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceHover = Color.FromArgb(248, 249, 250);
    public static readonly Color Border = Color.FromArgb(218, 224, 232);
    public static readonly Color BorderLight = Color.FromArgb(230, 235, 242);
    public static readonly Color HoverBackground = Color.FromArgb(235, 241, 249);
    public static readonly Color TextMuted = Color.FromArgb(115, 125, 140);
    public static readonly Color TextSecondary = Color.FromArgb(70, 80, 95);

    // --- États ---
    public static readonly Color Success = Color.FromArgb(22, 163, 74);
    public static readonly Color SuccessLight = Color.FromArgb(220, 250, 230);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color DangerLight = Color.FromArgb(254, 230, 230);
    public static readonly Color Warning = Color.FromArgb(217, 148, 0);
    public static readonly Color WarningLight = Color.FromArgb(255, 248, 220);
    public static readonly Color Info = Color.FromArgb(8, 96, 196);
    public static readonly Color InfoLight = Color.FromArgb(225, 238, 255);

    // --- Ombres / Profondeur ---
    public static readonly Color ShadowColor = Color.FromArgb(30, 11, 31, 59);
    public static readonly int ShadowBlur = 8;
    public static readonly int ShadowOffset = 2;

    // --- Rayons ---
    public static readonly int RadiusSmall = 6;
    public static readonly int RadiusMedium = 10;
    public static readonly int RadiusLarge = 14;
    public static readonly int RadiusCard = 12;

    // --- Espacements ---
    public static readonly int SpacingXS = 4;
    public static readonly int SpacingSM = 8;
    public static readonly int SpacingMD = 16;
    public static readonly int SpacingLG = 24;
    public static readonly int SpacingXL = 32;

    // --- Polices ---
    public static readonly Font Body = new("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font BodyBold = new("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Small = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font SmallBold = new("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Title = new("Segoe UI Semibold", 18F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font TitleLarge = new("Segoe UI Semibold", 22F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font BigNumber = new("Segoe UI Semibold", 28F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Caption = new("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);

    // --- Mise à l'échelle DPI ---
    // L'application est en mode "SystemAware" : un seul facteur d'échelle pour tout le processus.
    // Toutes les dimensions en pixels écrites dans le code passent par Px() pour rester lisibles sur écrans HiDPI.
    private static readonly Lazy<float> Scale = new(() =>
    {
        using var g = Graphics.FromHwnd(IntPtr.Zero);
        return g.DpiX / 96f;
    });

    /// <summary>Convertit une taille "à 100 %" en pixels réels pour l'écran courant.</summary>
    public static int Px(int value) => (int)Math.Round(value * Scale.Value);

    /// <summary>Espace standard DPI-aware.</summary>
    public static int Spacing(int baseValue) => Px(baseValue);

    /// <summary>Rayon standard DPI-aware.</summary>
    public static int Radius(int baseValue) => Px(baseValue);

    /// <summary>Mesure la taille d'un texte avec une police donnée.</summary>
    public static Size MeasureString(string text, Font font)
    {
        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        return g.MeasureString(text, font).ToSize();
    }
}
