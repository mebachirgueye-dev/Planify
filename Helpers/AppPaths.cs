namespace Planify.Helpers;

/// <summary>
/// Emplacements permanents des fichiers de Planify.
/// Tout est stocké dans %LocalAppData%\Planify : ce dossier est inscriptible sans droits administrateur
/// et n'est jamais touché par une mise à jour ou une réinstallation du logiciel.
/// </summary>
public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Planify");

    public static string DataDirectory => Path.Combine(Root, "Data");

    public static string DatabaseFile => Path.Combine(DataDirectory, "planify.db");

    /// <summary>Dossier de sauvegarde par défaut (utilisé en phase 2).</summary>
    public static string BackupDirectory => Path.Combine(Root, "Backups");

    public static string LogDirectory => Path.Combine(Root, "Logs");
}
