using Microsoft.EntityFrameworkCore;
using Planify.Data;

namespace Planify.Services;

/// <summary>État de la base SQLite (affiché sur le Dashboard, utile pour vérifier que tout fonctionne).</summary>
public sealed record DatabaseStatus(
    bool CanConnect,
    string FilePath,
    long SizeBytes,
    int AppliedMigrations,
    int PendingMigrations,
    string? LastMigration,
    string? Error);

public class DatabaseInfoService
{
    private readonly IDbContextFactory<PlanifyDbContext> _factory;

    public DatabaseInfoService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
    }

    public DatabaseStatus GetStatus()
    {
        string path = string.Empty;
        try
        {
            using var db = _factory.CreateDbContext();
            path = db.Database.GetDbConnection().DataSource;

            bool canConnect = db.Database.CanConnect();
            if (!canConnect)
                return new DatabaseStatus(false, path, 0, 0, 0, null, "Connexion impossible.");

            var applied = db.Database.GetAppliedMigrations().ToList();
            int pending = db.Database.GetPendingMigrations().Count();
            long size = File.Exists(path) ? new FileInfo(path).Length : 0;

            return new DatabaseStatus(true, path, size, applied.Count, pending, applied.LastOrDefault(), null);
        }
        catch (Exception ex)
        {
            return new DatabaseStatus(false, path, 0, 0, 0, null, ex.Message);
        }
    }
}
