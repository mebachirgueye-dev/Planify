using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Models;

namespace Planify.Services;

/// <summary>
/// Règles métier et accès aux données des réservations.
/// Responsable de la détection des conflits horaires.
/// </summary>
public class ReservationService
{
    private readonly IDbContextFactory<PlanifyDbContext> _factory;

    public ReservationService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>Toutes les réservations confirmées, triées par date puis par heure de début.</summary>
    public List<Reservation> GetAll()
    {
        using var db = _factory.CreateDbContext();
        var list = db.Reservations
            .Where(r => r.Statut == StatutReservation.Confirmee)
            .Include(r => r.Salle)
            .Include(r => r.Utilisateur)
            .AsNoTracking()
            .ToList();
        list.Sort((a, b) =>
            a.Date.CompareTo(b.Date) is int c and not 0 ? c
            : a.HeureDebut.CompareTo(b.HeureDebut));
        return list;
    }

    /// <summary>Toutes les réservations d'une salle (confirmées ou annulées), triées par date.</summary>
    public List<Reservation> GetBySalle(int salleId)
    {
        using var db = _factory.CreateDbContext();
        var list = db.Reservations
            .Where(r => r.SalleId == salleId)
            .Include(r => r.Utilisateur)
            .AsNoTracking()
            .ToList();
        list.Sort((a, b) => a.Date.CompareTo(b.Date));
        return list;
    }

    /// <summary>Réservations d'un utilisateur (confirmées et en cours ou futures).</summary>
    public List<Reservation> GetByUtilisateur(int utilisateurId)
    {
        using var db = _factory.CreateDbContext();
        var list = db.Reservations
            .Where(r => r.UtilisateurId == utilisateurId && r.Statut == StatutReservation.Confirmee)
            .Include(r => r.Salle)
            .AsNoTracking()
            .ToList();
        list.Sort((a, b) =>
            a.Date.CompareTo(b.Date) is int c and not 0 ? c
            : a.HeureDebut.CompareTo(b.HeureDebut));
        return list;
    }

    /// <summary>Réservations d'un jour spécifique (confirmées), triées par heure de début.</summary>
    public List<Reservation> GetByDate(DateTime date)
    {
        date = date.Date;
        using var db = _factory.CreateDbContext();
        var list = db.Reservations
            .Where(r => r.Date == date && r.Statut == StatutReservation.Confirmee)
            .Include(r => r.Salle)
            .Include(r => r.Utilisateur)
            .AsNoTracking()
            .ToList();
        list.Sort((a, b) => a.HeureDebut.CompareTo(b.HeureDebut));
        return list;
    }

    public int Count()
    {
        using var db = _factory.CreateDbContext();
        return db.Reservations.Count(r => r.Statut == StatutReservation.Confirmee);
    }

    /// <summary>
    /// Crée (Id == 0) ou modifie une réservation après détection complète des conflits.
    /// Lève une <see cref="BusinessRuleException"/> avec un message clair si la saisie est refusée.
    /// </summary>
    public void Save(Reservation reservation)
    {
        string motif = reservation.Motif?.Trim() ?? string.Empty;

        // --- Validation basique ---
        if (reservation.SalleId <= 0)
            throw new BusinessRuleException("Sélectionnez une salle.");
        if (reservation.UtilisateurId <= 0)
            throw new BusinessRuleException("Sélectionnez un utilisateur.");
        if (motif.Length == 0)
            throw new BusinessRuleException("Le motif est obligatoire.");
        if (motif.Length > 200)
            throw new BusinessRuleException("Le motif ne peut pas dépasser 200 caractères.");
        if (reservation.HeureDebut >= reservation.HeureFin)
            throw new BusinessRuleException("L'heure de fin doit être après l'heure de début.");

        using var db = _factory.CreateDbContext();

        // Vérifier que la salle existe et est disponible
        var salle = db.Salles.Find(reservation.SalleId);
        if (salle is null)
            throw new BusinessRuleException("Cette salle n'existe plus. Actualisez la liste.");
        if (salle.Statut != StatutSalle.Disponible)
            throw new BusinessRuleException("Cette salle n'est pas disponible pour les réservations.");

        // Vérifier que l'utilisateur existe
        var utilisateur = db.Utilisateurs.Find(reservation.UtilisateurId);
        if (utilisateur is null)
            throw new BusinessRuleException("Cet utilisateur n'existe plus. Actualisez la liste.");

        // --- Détection de conflits ---
        
        // Conflit 1 : Deux réservations pour la même salle au même moment
        bool hasConflictSalle = db.Reservations.Any(r =>
            r.Id != reservation.Id
            && r.SalleId == reservation.SalleId
            && r.Date == reservation.Date.Date
            && r.Statut == StatutReservation.Confirmee
            && r.HeureDebut < reservation.HeureFin
            && r.HeureFin > reservation.HeureDebut);
        if (hasConflictSalle)
            throw new BusinessRuleException(
                $"Conflit horaire détecté : la salle « {salle.Numero} » est déjà réservée " +
                $"sur cette plage horaire ({reservation.HeureDebut:HH}:{reservation.HeureDebut:mm} - " +
                $"{reservation.HeureFin:HH}:{reservation.HeureFin:mm}).");

        // Conflit 2 : Un utilisateur ayant deux réservations simultanées
        bool hasConflictUtilisateur = db.Reservations.Any(r =>
            r.Id != reservation.Id
            && r.UtilisateurId == reservation.UtilisateurId
            && r.Date == reservation.Date.Date
            && r.Statut == StatutReservation.Confirmee
            && r.HeureDebut < reservation.HeureFin
            && r.HeureFin > reservation.HeureDebut);
        if (hasConflictUtilisateur)
            throw new BusinessRuleException(
                $"Conflit détecté : l'utilisateur a déjà une réservation confirmée " +
                $"sur cette plage horaire ({reservation.HeureDebut:HH}:{reservation.HeureDebut:mm} - " +
                $"{reservation.HeureFin:HH}:{reservation.HeureFin:mm}).");

        // Conflit 3 : Salle déjà occupée par un cours/événement
        bool hasCoursConflict = db.CoursEvenements.Any(c =>
            c.SalleId == reservation.SalleId
            && c.Date == reservation.Date.Date
            && c.Statut != StatutEvenement.Annule
            && c.HeureDebut < reservation.HeureFin
            && c.HeureFin > reservation.HeureDebut);
        if (hasCoursConflict)
            throw new BusinessRuleException(
                $"Conflit détecté : la salle « {salle.Numero} » est affectée à un cours ou événement " +
                $"sur cette plage horaire ({reservation.HeureDebut:HH}:{reservation.HeureDebut:mm} - " +
                $"{reservation.HeureFin:HH}:{reservation.HeureFin:mm}).");

        // --- Enregistrement ---
        Reservation entity;
        if (reservation.Id == 0)
        {
            entity = new Reservation { DateCreation = DateTime.Now };
            db.Reservations.Add(entity);
        }
        else
        {
            entity = db.Reservations.Find(reservation.Id)
                     ?? throw new BusinessRuleException("Cette réservation n'existe plus. Actualisez la liste.");
        }

        entity.SalleId = reservation.SalleId;
        entity.UtilisateurId = reservation.UtilisateurId;
        entity.Date = reservation.Date.Date;
        entity.HeureDebut = reservation.HeureDebut;
        entity.HeureFin = reservation.HeureFin;
        entity.Motif = motif;
        entity.Statut = reservation.Statut;

        db.SaveChanges();
        reservation.Id = entity.Id;
    }

    /// <summary>Annule une réservation (change son statut en Annulee, sans la supprimer).</summary>
    public void Cancel(int id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Reservations.Find(id);
        if (entity is null)
            return; // déjà supprimée

        if (entity.Statut != StatutReservation.Confirmee)
            return; // déjà annulée ou complétée

        entity.Statut = StatutReservation.Annulee;
        db.SaveChanges();
    }

    public void Delete(int id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Reservations.Find(id);
        if (entity is null)
            return; // déjà supprimée

        db.Reservations.Remove(entity);
        db.SaveChanges();
    }
}
