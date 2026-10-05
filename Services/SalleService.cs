using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Models;

namespace Planify.Services;

/// <summary>
/// Règles métier et accès aux données des salles.
/// Les écrans n'utilisent jamais le DbContext directement : ils passent par ce service.
/// </summary>
public class SalleService
{
    public const int MaxCapacite = 2000;

    private readonly IDbContextFactory<PlanifyDbContext> _factory;

    public SalleService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>Toutes les salles, triées par numéro (sans tenir compte de la casse).</summary>
    public List<Salle> GetAll()
    {
        using var db = _factory.CreateDbContext();
        var list = db.Salles
            .Include(s => s.Batiment)
            .Include(s => s.Equipements)
            .AsNoTracking()
            .ToList();
        list.Sort((a, b) => string.Compare(a.Numero, b.Numero, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

    public int Count()
    {
        using var db = _factory.CreateDbContext();
        return db.Salles.Count();
    }

    /// <summary>
    /// Recherche les salles disponibles selon les critères.
    /// </summary>
    /// <param name="capaciteMin">Capacité minimale requise (incluse).</param>
    /// <param name="batimentId">Bâtiment optionnel (null = tous les bâtiments).</param>
    /// <param name="type">Type optionnel (null = tous les types).</param>
    /// <param name="equipementsRequis">Liste d'IDs d'équipements obligatoires (vide = aucune exigence).</param>
    /// <param name="date">Date à vérifier (optionnel, pour les conflits horaires).</param>
    /// <param name="heureDebut">Heure de début de la plage demandée (optionnel).</param>
    /// <param name="heureFin">Heure de fin de la plage demandée (optionnel).</param>
    /// <returns>Salles correspondant aux critères, triées par capacité puis par numéro.</returns>
    public List<Salle> Search(
        int capaciteMin = 1,
        int? batimentId = null,
        string? type = null,
        List<int>? equipementsRequis = null,
        DateTime? date = null,
        TimeSpan? heureDebut = null,
        TimeSpan? heureFin = null)
    {
        using var db = _factory.CreateDbContext();
        var query = db.Salles
            .Where(s => s.Statut == StatutSalle.Disponible && s.Capacite >= capaciteMin)
            .AsQueryable();

        if (batimentId.HasValue)
            query = query.Where(s => s.BatimentId == batimentId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(s => s.Type != null && s.Type.Contains(type));

        var results = query
            .Include(s => s.Batiment)
            .Include(s => s.Equipements)
            .Include(s => s.Reservations)
            .AsNoTracking()
            .ToList();

        // Filtrer par équipements
        if (equipementsRequis is { Count: > 0 })
        {
            results = results
                .Where(s => equipementsRequis.All(eqId => s.Equipements.Any(eq => eq.Id == eqId)))
                .ToList();
        }

        // Filtrer par plage horaire (si demandé)
        if (date.HasValue && heureDebut.HasValue && heureFin.HasValue)
        {
            results = results
                .Where(s => !HasConflict(s, date.Value, heureDebut.Value, heureFin.Value))
                .ToList();
        }

        // Tri : capacité croissante, puis numéro
        results.Sort((a, b) =>
            a.Capacite.CompareTo(b.Capacite) is int c and not 0 ? c
            : string.Compare(a.Numero, b.Numero, StringComparison.CurrentCultureIgnoreCase));

        return results;
    }

    /// <summary>Indique si la salle a un conflit horaire sur la date et plage donnée.</summary>
    private static bool HasConflict(Salle salle, DateTime date, TimeSpan heureDebut, TimeSpan heureFin)
    {
        return salle.Reservations.Any(r =>
            r.Date == date.Date
            && r.Statut == StatutReservation.Confirmee
            && r.HeureDebut < heureFin
            && r.HeureFin > heureDebut);
    }

    /// <summary>
    /// Crée (Id == 0) ou modifie une salle après validation.
    /// Lève une <see cref="BusinessRuleException"/> avec un message clair si la saisie est refusée.
    /// </summary>
    public void Save(Salle salle, List<int>? equipementIds = null)
    {
        string numero = salle.Numero?.Trim() ?? string.Empty;
        string? type = NullIfEmpty(salle.Type);
        string? description = NullIfEmpty(salle.Description);

        // --- Validation ---
        if (numero.Length == 0)
            throw new BusinessRuleException("Le numéro de la salle est obligatoire.");
        if (numero.Length > 50)
            throw new BusinessRuleException("Le numéro de la salle ne peut pas dépasser 50 caractères.");
        if (salle.BatimentId <= 0)
            throw new BusinessRuleException("Sélectionnez un bâtiment.");
        if (salle.Capacite < 1 || salle.Capacite > MaxCapacite)
            throw new BusinessRuleException($"La capacité doit être comprise entre 1 et {MaxCapacite}.");
        if (type is { Length: > 50 })
            throw new BusinessRuleException("Le type ne peut pas dépasser 50 caractères.");
        if (description is { Length: > 500 })
            throw new BusinessRuleException("La description ne peut pas dépasser 500 caractères.");

        using var db = _factory.CreateDbContext();

        // Vérifier que le bâtiment existe
        if (!db.Batiments.Any(b => b.Id == salle.BatimentId))
            throw new BusinessRuleException("Ce bâtiment n'existe plus. Actualisez la liste.");

        // Vérifier l'unicité (Numero, BatimentId)
        bool numeroDejaUtilise = db.Salles.Any(s =>
            s.Id != salle.Id
            && s.Numero.ToLower() == numero.ToLower()
            && s.BatimentId == salle.BatimentId);
        if (numeroDejaUtilise)
            throw new BusinessRuleException(
                $"Une salle nommée « {numero} » existe déjà dans ce bâtiment. Choisissez un autre numéro.");

        Salle entity;
        if (salle.Id == 0)
        {
            entity = new Salle();
            db.Salles.Add(entity);
        }
        else
        {
            entity = db.Salles
                .Include(s => s.Equipements)
                .FirstOrDefault(s => s.Id == salle.Id)
                ?? throw new BusinessRuleException("Cette salle n'existe plus. Actualisez la liste.");
        }

        entity.Numero = numero;
        entity.BatimentId = salle.BatimentId;
        entity.Capacite = salle.Capacite;
        entity.Type = type;
        entity.Description = description;
        entity.Statut = salle.Statut;

        // Mettre à jour les équipements (many-to-many)
        if (equipementIds is not null)
        {
            entity.Equipements.Clear();
            var equipements = db.Equipements.Where(eq => equipementIds.Contains(eq.Id)).ToList();
            foreach (var eq in equipements)
                entity.Equipements.Add(eq);
        }

        db.SaveChanges();
        salle.Id = entity.Id;
    }

    public void Delete(int id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Salles.Include(s => s.Reservations).FirstOrDefault(s => s.Id == id);
        if (entity is null)
            return; // déjà supprimée

        // Vérifier s'il y a des réservations actives
        bool hasActiveReservations = entity.Reservations.Any(r => r.Statut == StatutReservation.Confirmee);
        if (hasActiveReservations)
            throw new BusinessRuleException(
                "Impossible de supprimer cette salle : elle a des réservations confirmées. " +
                "Annulez les réservations d'abord.");

        db.Salles.Remove(entity);
        db.SaveChanges();
    }

    private static string? NullIfEmpty(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
