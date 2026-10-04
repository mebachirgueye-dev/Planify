using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Helpers;
using Planify.Models;

namespace Planify.Services;

/// <summary>
/// Connexion des utilisateurs : vérifie l'identifiant (e-mail) et le mot de passe
/// (comparaison de hachage), puis ouvre la session.
/// </summary>
public class AuthService
{
    private readonly IDbContextFactory<PlanifyDbContext> _factory;

    public AuthService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>Indique s'il existe déjà au moins un compte (pour le premier lancement).</summary>
    public bool HasAnyUser()
    {
        using var db = _factory.CreateDbContext();
        return db.Utilisateurs.Any();
    }

    /// <summary>
    /// Tente de connecter l'utilisateur.
    /// Lève une <see cref="BusinessRuleException"/> avec un message clair si la connexion est refusée.
    /// </summary>
    public void Login(string email, string password)
    {
        string normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
            throw new BusinessRuleException("Veuillez saisir votre adresse e-mail.");
        if (password.Length == 0)
            throw new BusinessRuleException("Veuillez saisir votre mot de passe.");

        using var db = _factory.CreateDbContext();
        var user = db.Utilisateurs.AsNoTracking()
            .FirstOrDefault(u => u.Email.ToLower() == normalized);

        // Même message pour « compte inconnu » et « mot de passe erroné » :
        // on ne révèle pas quels e-mails existent.
        if (user is null || !PasswordHasher.Verify(password, user.MotDePasseHash))
            throw new BusinessRuleException("Adresse e-mail ou mot de passe incorrect.");

        if (user.Statut == StatutUtilisateur.Desactive)
            throw new BusinessRuleException("Ce compte est désactivé. Contactez un administrateur.");

        Session.Login(user);
    }
}
