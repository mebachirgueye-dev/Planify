namespace Planify.Models;

/// <summary>État d'une réservation.</summary>
public enum StatutReservation
{
    /// <summary>Réservation confirmée.</summary>
    Confirmee = 0,

    /// <summary>Réservation annulée par l'utilisateur ou un administrateur.</summary>
    Annulee = 1,

    /// <summary>Réservation marquée comme complétée après son heure de fin.</summary>
    Completee = 2
}

/// <summary>
/// Une réservation de salle : qui l'a réservée, quand, pour quel motif.
/// Les réservations ne peuvent pas se chevaucher dans la même salle.
/// </summary>
public class Reservation
{
    public int Id { get; set; }

    /// <summary>Salle réservée (obligatoire).</summary>
    public int SalleId { get; set; }
    public Salle? Salle { get; set; }

    /// <summary>Utilisateur qui a fait la réservation (obligatoire).</summary>
    public int UtilisateurId { get; set; }
    public Utilisateur? Utilisateur { get; set; }

    /// <summary>Date de la réservation (ex. : "2026-10-15", sans heure).</summary>
    public DateTime Date { get; set; }

    /// <summary>Heure de début (ex. : 14:00).</summary>
    public TimeSpan HeureDebut { get; set; }

    /// <summary>Heure de fin (ex. : 16:30). Doit être > HeureDebut.</summary>
    public TimeSpan HeureFin { get; set; }

    /// <summary>Motif de la réservation (ex. : "Cours", "Réunion d'équipe"). Obligatoire, max 200.</summary>
    public string Motif { get; set; } = string.Empty;

    public StatutReservation Statut { get; set; } = StatutReservation.Confirmee;

    /// <summary>Date et heure de création de la réservation.</summary>
    public DateTime DateCreation { get; set; }

    // --- Propriétés calculées pour l'affichage et la logique ---

    /// <summary>Plage horaire sous forme lisible (ex. : "14:00 - 16:30").</summary>
    public string PlageHoraire => $"{HeureDebut:hh\\:mm} - {HeureFin:hh\\:mm}";

    /// <summary>Horodatage complet (date + heure début + fin, ex. : "15/10 14:00-16:30").</summary>
    public string DateHeure =>
        $"{Date:dd/MM} {PlageHoraire}";
}
