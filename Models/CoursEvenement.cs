namespace Planify.Models;

/// <summary>
/// Un cours ou événement planifié dans une salle.
/// Le responsable est l'utilisateur qui anime ou organise l'événement.
/// </summary>
public enum StatutEvenement
{
    Planifie = 0,
    Annule = 1,
    Termine = 2
}

public class CoursEvenement
{
    public int Id { get; set; }

    /// <summary>Nom du cours / événement.</summary>
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime Date { get; set; }

    public TimeSpan HeureDebut { get; set; }

    public TimeSpan HeureFin { get; set; }

    public int SalleId { get; set; }
    public Salle? Salle { get; set; }

    public int ResponsableId { get; set; }
    public Utilisateur? Responsable { get; set; }

    public StatutEvenement Statut { get; set; } = StatutEvenement.Planifie;

    public DateTime DateCreation { get; set; }

    public string PlageHoraire => $"{HeureDebut:HH}:{HeureDebut:mm} - {HeureFin:HH}:{HeureFin:mm}";
    public string DateHeure => $"{Date:dd/MM/yyyy} {PlageHoraire}";
}
