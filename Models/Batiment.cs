namespace Planify.Models;

/// <summary>
/// Un bâtiment (ex. : "Bâtiment A", "Campus Nord"). Les salles y seront rattachées en phase 2.
/// </summary>
public class Batiment
{
    public int Id { get; set; }

    /// <summary>Nom du bâtiment (obligatoire, unique).</summary>
    public string Nom { get; set; } = string.Empty;

    /// <summary>Adresse ou localisation (facultatif).</summary>
    public string? Adresse { get; set; }

    public int NombreEtages { get; set; } = 1;

    public string? Description { get; set; }
}
