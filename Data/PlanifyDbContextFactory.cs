using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Planify.Helpers;

namespace Planify.Data;

/// <summary>
/// Fabrique de contextes. On crée un contexte court par opération (using var db = factory.CreateDbContext()),
/// ce qui évite les données périmées et les problèmes de durée de vie.
/// </summary>
public sealed class PlanifyDbContextFactory : IDbContextFactory<PlanifyDbContext>
{
    private readonly DbContextOptions<PlanifyDbContext> _options;

    /// <param name="databasePath">Chemin du fichier .db ; par défaut %LocalAppData%\Planify\Data\planify.db</param>
    public PlanifyDbContextFactory(string? databasePath = null)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath ?? AppPaths.DatabaseFile,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            // Pas de pool de connexions : le fichier n'est jamais verrouillé entre deux opérations,
            // ce qui permettra de le copier (sauvegarde) ou de le remplacer (restauration) sans risque.
            Pooling = false
        }.ToString();

        _options = new DbContextOptionsBuilder<PlanifyDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }

    public PlanifyDbContext CreateDbContext() => new(_options);
}
