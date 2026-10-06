namespace Planify.Helpers;

/// <summary>Journal d'erreurs minimal (fichier texte) pour faciliter le diagnostic.</summary>
public static class AppLog
{
    public static string LogFile => Path.Combine(AppPaths.LogDirectory, "planify.log");

    public static void Error(string context, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);
            File.AppendAllText(LogFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Le journal ne doit jamais provoquer une nouvelle erreur.
        }
    }

    public static void Info(string message)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);
            File.AppendAllText(LogFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}{Environment.NewLine}");
        }
        catch { }
    }
}
