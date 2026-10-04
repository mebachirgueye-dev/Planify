using Microsoft.EntityFrameworkCore;
using Planify.Models;

namespace Planify.Data;

/// <summary>
/// Contexte Entity Framework Core de Planify. Une seule base SQLite locale.
/// Chaque nouvelle entité = un DbSet ici + sa configuration dans OnModelCreating + une migration.
/// </summary>
public class PlanifyDbContext : DbContext
{
    public PlanifyDbContext(DbContextOptions<PlanifyDbContext> options) : base(options)
    {
    }

    public DbSet<Batiment> Batiments => Set<Batiment>();
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Batiment>(e =>
        {
            e.Property(b => b.Nom).IsRequired().HasMaxLength(100);
            e.Property(b => b.Adresse).HasMaxLength(200);
            e.Property(b => b.Description).HasMaxLength(500);

            // Deux bâtiments ne peuvent pas porter exactement le même nom.
            e.HasIndex(b => b.Nom).IsUnique();
        });

        modelBuilder.Entity<Utilisateur>(e =>
        {
            e.Property(u => u.Nom).IsRequired().HasMaxLength(100);
            e.Property(u => u.Prenom).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.MotDePasseHash).IsRequired().HasMaxLength(200);

            // L'adresse e-mail sert d'identifiant de connexion : elle doit être unique.
            e.HasIndex(u => u.Email).IsUnique();
        });
    }
}
