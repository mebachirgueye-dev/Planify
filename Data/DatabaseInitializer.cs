using Microsoft.EntityFrameworkCore;
using Planify.Helpers;

namespace Planify.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Crée le dossier de données si besoin, puis applique toutes les migrations en attente.
    /// - Premier lancement : la base est créée.
    /// - Mise à jour du logiciel : seules les nouvelles migrations sont appliquées, les données existantes sont conservées.
    /// </summary>
    public static void Initialize(IDbContextFactory<PlanifyDbContext> factory)
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);

        using var db = factory.CreateDbContext();
        db.Database.Migrate();
    }
}
