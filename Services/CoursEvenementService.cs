using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Models;

namespace Planify.Services;

/// <summary>
/// Gestion des cours et événements : validation, création, modification et détection de conflits.
/// </summary>
public class CoursEvenementService
{
    private readonly IDbContextFactory<PlanifyDbContext> _factory;

    public CoursEvenementService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
    }

    public List<CoursEvenement> GetAll()
    {
        using var db = _factory.CreateDbContext();
        var list = db.CoursEvenements
            .Include(c => c.Salle)
            .Include(c => c.Responsable)
            .AsNoTracking()
            .ToList();
        list.Sort((a, b) =>
            a.Date.CompareTo(b.Date) is int c and not 0 ? c
            : a.HeureDebut.CompareTo(b.HeureDebut));
        return list;
    }

    public List<CoursEvenement> GetByDate(DateTime date)
    {
        date = date.Date;
        using var db = _factory.CreateDbContext();
        var list = db.CoursEvenements
            .Where(c => c.Date == date && c.Statut != StatutEvenement.Annule)
            .Include(c => c.Salle)
            .Include(c => c.Responsable)
            .AsNoTracking()
            .ToList();
        list.Sort((a, b) => a.HeureDebut.CompareTo(b.HeureDebut));
        return list;
    }

    public int Count()
    {
        using var db = _factory.CreateDbContext();
        return db.CoursEvenements.Count(c => c.Statut != StatutEvenement.Annule);
    }

    public void Save(CoursEvenement evenement)
    {
        string nom = evenement.Nom?.Trim() ?? string.Empty;
        string? description = string.IsNullOrWhiteSpace(evenement.Description) ? null : evenement.Description.Trim();

        if (nom.Length == 0)
            throw new BusinessRuleException("Le nom du cours ou événement est obligatoire.");
        if (nom.Length > 120)
            throw new BusinessRuleException("Le nom ne peut pas dépasser 120 caractères.");
        if (evenement.Date == default)
            throw new BusinessRuleException("La date du cours ou événement est obligatoire.");
        if (description is { Length: > 500 })
            throw new BusinessRuleException("La description ne peut pas dépasser 500 caractères.");
        if (evenement.SalleId <= 0)
            throw new BusinessRuleException("Sélectionnez une salle.");
        if (evenement.ResponsableId <= 0)
            throw new BusinessRuleException("Sélectionnez un responsable.");
        if (evenement.HeureDebut >= evenement.HeureFin)
            throw new BusinessRuleException("L'heure de fin doit être après l'heure de début.");

        using var db = _factory.CreateDbContext();

        var salle = db.Salles.Find(evenement.SalleId);
        if (salle is null)
            throw new BusinessRuleException("Cette salle n'existe plus. Actualisez la liste.");
        if (salle.Statut != StatutSalle.Disponible)
            throw new BusinessRuleException("Cette salle n'est pas disponible pour un cours ou événement.");

        var responsable = db.Utilisateurs.Find(evenement.ResponsableId);
        if (responsable is null)
            throw new BusinessRuleException("Ce responsable n'existe plus. Actualisez la liste.");

        bool hasRoomConflict = db.CoursEvenements.Any(e =>
            e.Id != evenement.Id
            && e.SalleId == evenement.SalleId
            && e.Date == evenement.Date.Date
            && e.Statut != StatutEvenement.Annule
            && e.HeureDebut < evenement.HeureFin
            && e.HeureFin > evenement.HeureDebut);
        if (hasRoomConflict)
            throw new BusinessRuleException(
                $"Conflit détecté : la salle « {salle.Numero} » est déjà affectée à un autre cours ou événement sur cette plage horaire.");

        bool hasReservationConflict = db.Reservations.Any(r =>
            r.SalleId == evenement.SalleId
            && r.Date == evenement.Date.Date
            && r.Statut == StatutReservation.Confirmee
            && r.HeureDebut < evenement.HeureFin
            && r.HeureFin > evenement.HeureDebut);
        if (hasReservationConflict)
            throw new BusinessRuleException(
                "Conflit détecté : la salle est déjà réservée pendant cette période par un autre utilisateur. Supprimez ou déplacez la réservation avant d'enregistrer le cours.");

        CoursEvenement entity;
        if (evenement.Id == 0)
        {
            entity = new CoursEvenement { DateCreation = DateTime.Now };
            db.CoursEvenements.Add(entity);
        }
        else
        {
            entity = db.CoursEvenements.Find(evenement.Id)
                ?? throw new BusinessRuleException("Ce cours ou événement n'existe plus. Actualisez la liste.");
        }

        entity.Nom = nom;
        entity.Description = description;
        entity.Date = evenement.Date.Date;
        entity.HeureDebut = evenement.HeureDebut;
        entity.HeureFin = evenement.HeureFin;
        entity.SalleId = evenement.SalleId;
        entity.ResponsableId = evenement.ResponsableId;
        entity.Statut = evenement.Statut;

        db.SaveChanges();
        evenement.Id = entity.Id;
    }

    public void Delete(int id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.CoursEvenements.Find(id);
        if (entity is null)
            return;

        db.CoursEvenements.Remove(entity);
        db.SaveChanges();
    }
}
