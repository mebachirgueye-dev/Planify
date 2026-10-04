using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Helpers;
using Planify.Models;

namespace Planify.Services;

/// <summary>
/// Règles métier et accès aux données des comptes utilisateurs.
/// Les écrans n'utilisent jamais le DbContext directement : ils passent par ce service.
/// </summary>
public class UtilisateurService
{
    public const int MinPasswordLength = 8;

    private readonly IDbContextFactory<PlanifyDbContext> _factory;

    public UtilisateurService(IDbContextFactory<PlanifyDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>Tous les comptes, triés par nom puis prénom (sans tenir compte de la casse).</summary>
    public List<Utilisateur> GetAll()
    {
        using var db = _factory.CreateDbContext();
        var list = db.Utilisateurs.AsNoTracking().ToList();
        list.Sort((a, b) =>
            string.Compare(a.Nom, b.Nom, StringComparison.CurrentCultureIgnoreCase)
            is int c and not 0 ? c
            : string.Compare(a.Prenom, b.Prenom, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

    public int Count()
    {
        using var db = _factory.CreateDbContext();
        return db.Utilisateurs.Count();
    }

    /// <summary>Nombre d'administrateurs actifs (pour protéger le dernier administrateur).</summary>
    public int CountAdmins()
    {
        using var db = _factory.CreateDbContext();
        return db.Utilisateurs.Count(u => u.Role == Role.Administrateur && u.Statut == StatutUtilisateur.Actif);
    }

    /// <summary>
    /// Crée (Id == 0) ou modifie un compte après validation.
    /// Lève une <see cref="BusinessRuleException"/> avec un message clair si la saisie est refusée.
    /// </summary>
    public void Save(Utilisateur user, string? newPassword)
    {
        string nom = user.Nom?.Trim() ?? string.Empty;
        string prenom = user.Prenom?.Trim() ?? string.Empty;
        string email = user.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        // --- Validation ---
        if (nom.Length == 0)
            throw new BusinessRuleException("Le nom est obligatoire.");
        if (nom.Length > 100)
            throw new BusinessRuleException("Le nom ne peut pas dépasser 100 caractères.");
        if (prenom.Length == 0)
            throw new BusinessRuleException("Le prénom est obligatoire.");
        if (prenom.Length > 100)
            throw new BusinessRuleException("Le prénom ne peut pas dépasser 100 caractères.");
        if (email.Length == 0)
            throw new BusinessRuleException("L'adresse e-mail est obligatoire.");
        if (email.Length > 200)
            throw new BusinessRuleException("L'adresse e-mail ne peut pas dépasser 200 caractères.");
        if (!email.Contains('@') || email.IndexOf('@') == email.Length - 1 || email.IndexOf('@') == 0)
            throw new BusinessRuleException("L'adresse e-mail n'est pas valide.");

        bool isNew = user.Id == 0;
        if (isNew && string.IsNullOrEmpty(newPassword))
            throw new BusinessRuleException("Le mot de passe est obligatoire pour un nouveau compte.");
        if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < MinPasswordLength)
            throw new BusinessRuleException($"Le mot de passe doit contenir au moins {MinPasswordLength} caractères.");

        using var db = _factory.CreateDbContext();

        bool emailDejaUtilise = db.Utilisateurs.Any(u => u.Id != user.Id && u.Email.ToLower() == email);
        if (emailDejaUtilise)
            throw new BusinessRuleException($"Un compte avec l'adresse « {email} » existe déjà.");

        // Protection du dernier administrateur actif : on ne peut ni le supprimer,
        // ni le désactiver, ni lui retirer le rôle administrateur.
        bool lastAdmin = false;
        if (!isNew)
        {
            var existing = db.Utilisateurs.Find(user.Id);
            if (existing is not null
                && existing.Role == Role.Administrateur
                && existing.Statut == StatutUtilisateur.Actif
                && CountAdmins() <= 1)
            {
                lastAdmin = true;
            }
        }

        if (lastAdmin && (user.Role != Role.Administrateur || user.Statut == StatutUtilisateur.Desactive))
            throw new BusinessRuleException(
                "Impossible de modifier ce compte : c'est le dernier administrateur actif. " +
                "Créez d'abord un autre administrateur.");

        Utilisateur entity;
        if (isNew)
        {
            entity = new Utilisateur { DateCreation = DateTime.Now };
            db.Utilisateurs.Add(entity);
        }
        else
        {
            entity = db.Utilisateurs.Find(user.Id)
                     ?? throw new BusinessRuleException("Ce compte n'existe plus. Actualisez la liste.");
        }

        entity.Nom = nom;
        entity.Prenom = prenom;
        entity.Email = email;
        entity.Role = user.Role;
        entity.Statut = user.Statut;
        if (!string.IsNullOrEmpty(newPassword))
            entity.MotDePasseHash = PasswordHasher.Hash(newPassword);

        db.SaveChanges();
        user.Id = entity.Id;
    }

    public void Delete(int id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Utilisateurs.Find(id);
        if (entity is null)
            return; // déjà supprimé

        if (entity.Role == Role.Administrateur
            && entity.Statut == StatutUtilisateur.Actif
            && CountAdmins() <= 1)
        {
            throw new BusinessRuleException(
                "Impossible de supprimer ce compte : c'est le dernier administrateur actif. " +
                "Créez d'abord un autre administrateur.");
        }

        db.Utilisateurs.Remove(entity);
        db.SaveChanges();
    }
}
