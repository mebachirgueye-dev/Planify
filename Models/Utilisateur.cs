namespace Planify.Models;

/// <summary>
/// Rôle de l'utilisateur : détermine les actions autorisées dans le logiciel.
/// Les valeurs sont ordonnées par privilège (0 = le plus élevé) :
/// <see cref="Session.HasRole"/> compare donc les rôles par valeur.
/// </summary>
public enum Role
{
    /// <summary>Accès complet : gère les utilisateurs et l'ensemble du logiciel.</summary>
    Administrateur = 0,

    /// <summary>Gère les salles, le planning et les réservations.</summary>
    Gestionnaire = 1,

    /// <summary>Consultation et réservation selon les permissions.</summary>
    Utilisateur = 2
}

/// <summary>État du compte.</summary>
public enum StatutUtilisateur
{
    Actif = 0,
    Desactive = 1
}

/// <summary>
/// Un compte utilisateur. Le mot de passe n'est jamais stocké en clair :
/// seul son hachage (<see cref="MotDePasseHash"/>) est conservé dans la base.
/// </summary>
public class Utilisateur
{
    public int Id { get; set; }

    /// <summary>Nom de famille (obligatoire).</summary>
    public string Nom { get; set; } = string.Empty;

    /// <summary>Prénom (obligatoire).</summary>
    public string Prenom { get; set; } = string.Empty;

    /// <summary>Adresse e-mail (obligatoire, unique) : sert d'identifiant de connexion.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hachage du mot de passe (PBKDF2). Jamais le mot de passe en clair.</summary>
    public string MotDePasseHash { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.Utilisateur;

    public DateTime DateCreation { get; set; }

    public StatutUtilisateur Statut { get; set; } = StatutUtilisateur.Actif;

    /// <summary>Nom complet (ex. : « Jean Dupont »), pour l'affichage. Non stocké en base.</summary>
    [NotMapped]
    public string NomComplet => $"{Prenom} {Nom}".Trim();
}
