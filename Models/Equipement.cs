namespace Planify.Models;

/// <summary>
/// Un équipement disponible dans les salles (ex. : vidéoprojecteur, tableau blanc, climatisation).
/// Plusieurs salles peuvent partager les mêmes équipements ; plusieurs équipements peuvent être dans une salle.
/// </summary>
public class Equipement
{
    public int Id { get; set; }

    /// <summary>Nom de l'équipement (obligatoire, unique).</summary>
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation : salles qui possèdent cet équipement.
    public ICollection<Salle> Salles { get; set; } = new List<Salle>();
}
