using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Models;

namespace Planify.Services;

/// <summary>
/// Règles métier et accès aux données des équipements.
/// Les écrans n'utilisent jamais le DbContext directement : ils passent par ce service.
/// </summary>
public class EquipementService
{
    private readonly IDbContextFactory<PlanifyDbContext> _factory;

    public EquipementService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>Tous les équipements, triés par nom (sans tenir compte de la casse).</summary>
    public List<Equipement> GetAll()
    {
        using var db = _factory.CreateDbContext();
        var list = db.Equipements.AsNoTracking().ToList();
        list.Sort((a, b) => string.Compare(a.Nom, b.Nom, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

    public int Count()
    {
        using var db = _factory.CreateDbContext();
        return db.Equipements.Count();
    }

    /// <summary>
    /// Crée (Id == 0) ou modifie un équipement après validation.
    /// Lève une <see cref="BusinessRuleException"/> avec un message clair si la saisie est refusée.
    /// </summary>
    public void Save(Equipement equipement)
    {
        string nom = equipement.Nom?.Trim() ?? string.Empty;
        string? description = NullIfEmpty(equipement.Description);

        // --- Validation ---
        if (nom.Length == 0)
            throw new BusinessRuleException("Le nom de l'équipement est obligatoire.");
        if (nom.Length > 100)
            throw new BusinessRuleException("Le nom de l'équipement ne peut pas dépasser 100 caractères.");
        if (description is { Length: > 500 })
            throw new BusinessRuleException("La description ne peut pas dépasser 500 caractères.");

        using var db = _factory.CreateDbContext();

        bool nomDejaUtilise = db.Equipements.Any(eq => eq.Id != equipement.Id && eq.Nom.ToLower() == nom.ToLower());
        if (nomDejaUtilise)
            throw new BusinessRuleException($"Un équipement nommé « {nom} » existe déjà. Choisissez un autre nom.");

        Equipement entity;
        if (equipement.Id == 0)
        {
            entity = new Equipement();
            db.Equipements.Add(entity);
        }
        else
        {
            entity = db.Equipements.Find(equipement.Id)
                     ?? throw new BusinessRuleException("Cet équipement n'existe plus. Actualisez la liste.");
        }

        entity.Nom = nom;
        entity.Description = description;

        db.SaveChanges();
        equipement.Id = entity.Id;
    }

    public void Delete(int id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Equipements.Find(id);
        if (entity is null)
            return; // déjà supprimé

        // Vérifier si l'équipement est utilisé par au moins une salle
        bool used = db.Salles.Any(s => s.Equipements.Any(eq => eq.Id == id));
        if (used)
            throw new BusinessRuleException(
                "Impossible de supprimer cet équipement : il est utilisé par au moins une salle. " +
                "Supprimez d'abord l'équipement de toutes les salles.");

        db.Equipements.Remove(entity);
        db.SaveChanges();
    }

    private static string? NullIfEmpty(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
