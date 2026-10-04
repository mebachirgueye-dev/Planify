using Microsoft.EntityFrameworkCore.Design;

namespace Planify.Data;

/// <summary>
/// Utilisée uniquement par les outils EF Core (Add-Migration, dotnet ef ...) pour créer le contexte
/// sans démarrer l'application.
/// </summary>
public class PlanifyDesignTimeFactory : IDesignTimeDbContextFactory<PlanifyDbContext>
{
    public PlanifyDbContext CreateDbContext(string[] args) => new PlanifyDbContextFactory().CreateDbContext();
}
