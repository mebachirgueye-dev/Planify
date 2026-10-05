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
    public DbSet<Equipement> Equipements => Set<Equipement>();
    public DbSet<Salle> Salles => Set<Salle>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

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

        modelBuilder.Entity<Equipement>(e =>
        {
            e.Property(eq => eq.Nom).IsRequired().HasMaxLength(100);
            e.Property(eq => eq.Description).HasMaxLength(500);

            // Deux équipements ne peuvent pas porter le même nom.
            e.HasIndex(eq => eq.Nom).IsUnique();
        });

        modelBuilder.Entity<Salle>(e =>
        {
            e.Property(s => s.Numero).IsRequired().HasMaxLength(50);
            e.Property(s => s.Type).HasMaxLength(50);
            e.Property(s => s.Description).HasMaxLength(500);

            // Foreign key vers Batiment
            e.HasOne(s => s.Batiment)
                .WithMany()
                .HasForeignKey(s => s.BatimentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-many : Salle <-> Equipement
            e.HasMany(s => s.Equipements)
                .WithMany(eq => eq.Salles)
                .UsingEntity("SalleEquipement",
                    right => right.HasOne(typeof(Equipement)).WithMany().HasForeignKey("EquipementId"),
                    left => left.HasOne(typeof(Salle)).WithMany().HasForeignKey("SalleId"),
                    join => join.HasKey("SalleId", "EquipementId"));

            // Combo (Numero, BatimentId) = unique (impossible d'avoir deux "A101" dans le même bâtiment)
            e.HasIndex(s => new { s.Numero, s.BatimentId }).IsUnique();
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(r => r.Motif).IsRequired().HasMaxLength(200);

            // Foreign keys
            e.HasOne(r => r.Salle)
                .WithMany(s => s.Reservations)
                .HasForeignKey(r => r.SalleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Utilisateur)
                .WithMany()
                .HasForeignKey(r => r.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index pour rechercher rapide : réservations d'une salle sur une date donnée
            e.HasIndex(r => new { r.SalleId, r.Date });

            // Index pour chercher les réservations d'un utilisateur
            e.HasIndex(r => r.UtilisateurId);
        });
    }
}

