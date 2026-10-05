using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Models;

namespace Planify.Services;

/// <summary>
/// Règles métier et accès aux données des bâtiments.
/// Les écrans n'utilisent jamais le DbContext directement : ils passent par ce service.
/// </summary>
public class BatimentService
{
    public const int MaxEtages = 200;

    private readonly IDbContextFactory<PlanifyDbContext> _factory;

    public BatimentService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>Tous les bâtiments, triés par nom (sans tenir compte de la casse).</summary>
    public List<Batiment> GetAll()
    {
        using var db = _factory.CreateDbContext();
        var list = db.Batiments.AsNoTracking().ToList();
        list.Sort((a, b) => string.Compare(a.Nom, b.Nom, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

    public int Count()
    {
        using var db = _factory.CreateDbContext();
        return db.Batiments.Count();
    }

    /// <summary>
    /// Crée (Id == 0) ou modifie un bâtiment après validation.
    /// Lève une <see cref="BusinessRuleException"/> avec un message clair si la saisie est refusée.
    /// </summary>
    public void Save(Batiment batiment)
    {
        string nom = batiment.Nom?.Trim() ?? string.Empty;
        string? adresse = NullIfEmpty(batiment.Adresse);
        string? description = NullIfEmpty(batiment.Description);

        // --- Validation ---
        if (nom.Length == 0)
            throw new BusinessRuleException("Le nom du bâtiment est obligatoire.");
        if (nom.Length > 100)
            throw new BusinessRuleException("Le nom du bâtiment ne peut pas dépasser 100 caractères.");
        if (adresse is { Length: > 200 })
            throw new BusinessRuleException("L'adresse ne peut pas dépasser 200 caractères.");
        if (description is { Length: > 500 })
            throw new BusinessRuleException("La description ne peut pas dépasser 500 caractères.");
        if (batiment.NombreEtages < 1 || batiment.NombreEtages > MaxEtages)
            throw new BusinessRuleException($"Le nombre d'étages doit être compris entre 1 et {MaxEtages}.");

        using var db = _factory.CreateDbContext();

        bool nomDejaUtilise = db.Batiments.Any(b => b.Id != batiment.Id && b.Nom.ToLower() == nom.ToLower());
        if (nomDejaUtilise)
            throw new BusinessRuleException($"Un bâtiment nommé « {nom} » existe déjà. Choisissez un autre nom.");

        Batiment entity;
        if (batiment.Id == 0)
        {
            entity = new Batiment();
            db.Batiments.Add(entity);
        }
        else
        {
            entity = db.Batiments.Find(batiment.Id)
                     ?? throw new BusinessRuleException("Ce bâtiment n'existe plus. Actualisez la liste.");
        }

        entity.Nom = nom;
        entity.Adresse = adresse;
        entity.NombreEtages = batiment.NombreEtages;
        entity.Description = description;

        db.SaveChanges();
        batiment.Id = entity.Id;
    }

    public void Delete(int id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Batiments.Find(id);
        if (entity is null)
            return; // déjà supprimé

        // Vérifier s'il y a des salles dans ce bâtiment
        bool hasSalles = db.Salles.Any(s => s.BatimentId == id);
        if (hasSalles)
            throw new BusinessRuleException(
                "Impossible de supprimer ce bâtiment : il contient au moins une salle. " +
                "Supprimez d'abord toutes les salles du bâtiment.");

        db.Batiments.Remove(entity);
        db.SaveChanges();
    }

    private static string? NullIfEmpty(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
