using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Helpers;

namespace Planify.Services;

/// <summary>
/// Gestion des sauvegardes de la base SQLite : copie du fichier .db vers un dossier de sauvegarde.
/// </summary>
public sealed class BackupService
{
    private readonly IDbContextFactory<PlanifyDbContext> _factory;
    private readonly string _defaultBackupDir;

    public BackupService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
        _defaultBackupDir = AppPaths.BackupDirectory;
        Directory.CreateDirectory(_defaultBackupDir);
    }

    /// <summary>Dossier de sauvegarde par défaut.</summary>
    public string DefaultBackupDirectory => _defaultBackupDir;

    /// <summary>
    /// Crée une sauvegarde manuelle de la base actuelle.
    /// Le fichier est nommé : Planify_YYYY-MM-DD_HH-mm-ss.db
    /// </summary>
    /// <param name="customDirectory">Dossier cible (optionnel, sinon dossier par défaut).</param>
    /// <returns>Chemin complet du fichier de sauvegarde créé.</returns>
    public string CreateBackup(string? customDirectory = null)
    {
        string dbPath;
        using (var db = _factory.CreateDbContext())
        {
            dbPath = db.Database.GetDbConnection().DataSource;
        }

        if (!File.Exists(dbPath))
            throw new BusinessRuleException("Fichier de base de données introuvable.");

        string targetDir = customDirectory ?? _defaultBackupDir;
        Directory.CreateDirectory(targetDir);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"Planify_{timestamp}.db";
        string destPath = Path.Combine(targetDir, fileName);

        // Copier le fichier (la base doit être fermée ou en lecture seule pour être cohérente)
        // Ici on fait une copie simple ; pour une cohérence parfaite, on pourrait utiliser SQLite Backup API.
        File.Copy(dbPath, destPath, overwrite: true);

        return destPath;
    }

    /// <summary>
    /// Restaure une sauvegarde : remplace la base actuelle par le fichier .db fourni.
    /// Nécessite de fermer la connexion EF Core avant (l'application doit redémarrer après).
    /// </summary>
    /// <param name="backupFilePath">Chemin du fichier .db à restaurer.</param>
    public void RestoreBackup(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new BusinessRuleException("Fichier de sauvegarde introuvable.");

        string dbPath;
        using (var db = _factory.CreateDbContext())
        {
            dbPath = db.Database.GetDbConnection().DataSource;
        }

        // Sauvegarde de l'actuelle avant écrasement (sécurité)
        string safetyCopy = Path.Combine(
            Path.GetDirectoryName(dbPath)!,
            $"planify_before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        File.Copy(dbPath, safetyCopy, overwrite: true);

        // Remplacer la base actuelle
        File.Copy(backupFilePath, dbPath, overwrite: true);
    }

    /// <summary>Liste toutes les sauvegardes disponibles dans le dossier donné (ou par défaut).</summary>
    public List<BackupInfo> ListBackups(string? directory = null)
    {
        string dir = directory ?? _defaultBackupDir;
        if (!Directory.Exists(dir))
            return new List<BackupInfo>();

        return Directory.GetFiles(dir, "Planify_*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .Select(f => new BackupInfo(f.FullName, f.Name, f.Length, f.LastWriteTime))
            .ToList();
    }

    /// <summary>Supprime une sauvegarde spécifique.</summary>
    public void DeleteBackup(string backupFilePath)
    {
        if (File.Exists(backupFilePath))
            File.Delete(backupFilePath);
    }

    /// <summary>Nettoie les anciennes sauvegardes (garde les N plus récentes).</summary>
    public int CleanOldBackups(int keepCount = 10, string? directory = null)
    {
        var backups = ListBackups(directory);
        if (backups.Count <= keepCount)
            return 0;

        int deleted = 0;
        foreach (var b in backups.Skip(keepCount))
        {
            DeleteBackup(b.FilePath);
            deleted++;
        }
        return deleted;
    }
}

/// <summary>Informations sur un fichier de sauvegarde.</summary>
public sealed record BackupInfo(
    string FilePath,
    string FileName,
    long SizeBytes,
    DateTime CreatedAt)
{
    public string FormattedSize => FormatSize(SizeBytes);
    public string FormattedDate => CreatedAt.ToString("dd/MM/yyyy HH:mm:ss");

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} o";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} Ko";
        return $"{bytes / (1024.0 * 1024.0):0.#} Mo";
    }
}