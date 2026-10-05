namespace Planify.Models;

/// <summary>État d'une salle.</summary>
public enum StatutSalle
{
    /// <summary>Salle disponible à la réservation.</summary>
    Disponible = 0,

    /// <summary>Salle temporairement fermée (maintenance, travaux...).</summary>
    Indisponible = 1,

    /// <summary>Salle supprimée (conservée pour historique des réservations).</summary>
    Supprimee = 2
}

/// <summary>
/// Une salle physique (ex. : "A101", "Amphi 1") dans un bâtiment.
/// Les utilisateurs peuvent la réserver selon sa capacité et ses équipements.
/// </summary>
public class Salle
{
    public int Id { get; set; }

    /// <summary>Identifiant court de la salle (ex. : "A101", "Amphi 1"). Obligatoire, unique.</summary>
    public string Numero { get; set; } = string.Empty;

    /// <summary>Bâtiment auquel appartient la salle (obligatoire).</summary>
    public int BatimentId { get; set; }
    public Batiment? Batiment { get; set; }

    /// <summary>Capacité d'accueil (nombre de places). Obligatoire, > 0.</summary>
    public int Capacite { get; set; }

    /// <summary>Type : "Salle", "Amphi", "Labo", "Atelier", etc. Optionnel, max 50 caractères.</summary>
    public string? Type { get; set; }

    public string? Description { get; set; }

    public StatutSalle Statut { get; set; } = StatutSalle.Disponible;

    // Navigation : équipements de la salle (many-to-many).
    public ICollection<Equipement> Equipements { get; set; } = new List<Equipement>();

    // Navigation : réservations de la salle.
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
