using Planify.Models;

namespace Planify.Services;

/// <summary>
/// Session de l'utilisateur connecté (en mémoire, pour la durée de la session).
/// Instance unique, créée à la connexion et détruite à la déconnexion.
/// </summary>
public sealed class Session
{
    private static Utilisateur? _current;

    /// <summary>Utilisateur actuellement connecté, ou <c>null</c> si personne n'est connecté.</summary>
    public static Utilisateur? Current => _current;

    /// <summary>Indique si un utilisateur est connecté.</summary>
    public static bool IsAuthenticated => _current is not null;

    /// <summary>Indique si l'utilisateur connecté possède au moins le rôle demandé.</summary>
    public static bool HasRole(Role minimum) =>
        _current is not null && _current.Role <= minimum;

    /// <summary>Indique si l'utilisateur connecté est administrateur.</summary>
    public static bool IsAdmin => HasRole(Role.Administrateur);

    /// <summary>Indique si l'utilisateur connecté peut gérer les ressources (salles, réservations...).</summary>
    public static bool IsManager => HasRole(Role.Gestionnaire);

    /// <summary>Connecte l'utilisateur (remplace toute session existante).</summary>
    public static void Login(Utilisateur user) => _current = user;

    /// <summary>Termine la session courante.</summary>
    public static void Logout() => _current = null;
}
