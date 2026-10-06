using Microsoft.EntityFrameworkCore;
using Planify.Data;
using Planify.Helpers;
using Planify.Models;

namespace Planify.Data;

/// <summary>
/// Initialisation de données d'exemple pour la base de données.
/// Exécuté une seule fois au premier lancement si la base est vide.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Remplit la base avec des données d'exemple si elle est vide.
    /// </summary>
    public static void SeedIfEmpty(IDbContextFactory<PlanifyDbContext> factory)
    {
        using var db = factory.CreateDbContext();

        // Vérifier si des données existent déjà
        if (db.Batiments.Any() || db.Salles.Any() || db.Utilisateurs.Any() || db.Equipements.Any())
            return; // Base déjà initialisée

        try
        {
            // ===== BÂTIMENTS =====
            var batiments = new List<Batiment>
            {
                new Batiment { Nom = "Bâtiment A", Adresse = "12 Rue de l'Université", NombreEtages = 4, Description = "Bâtiment principal - Sciences et Technologies" },
                new Batiment { Nom = "Bâtiment B", Adresse = "12 Rue de l'Université", NombreEtages = 3, Description = "Lettres et Sciences Humaines" },
                new Batiment { Nom = "Bâtiment C", Adresse = "12 Rue de l'Université", NombreEtages = 2, Description = "Sciences Économiques et Gestion" },
                new Batiment { Nom = "Bâtiment D", Adresse = "5 Avenue du Campus", NombreEtages = 5, Description = "Ingénierie - Laboratoires et ateliers" },
                new Batiment { Nom = "Bibliothèque Centrale", Adresse = "10 Place du Savoir", NombreEtages = 3, Description = "Espace de travail, salles de lecture, informatique" }
            };
            db.Batiments.AddRange(batiments);
            db.SaveChanges();

            // ===== ÉQUIPEMENTS =====
            var equipements = new List<Equipement>
            {
                new Equipement { Nom = "Vidéoprojecteur", Description = "Vidéoprojecteur HDMI/VGA, 4000 lumens" },
                new Equipement { Nom = "Tableau blanc", Description = "Tableau blanc magnétique 2x1m" },
                new Equipement { Nom = "Tableau numérique", Description = "Écran interactif 65 pouces" },
                new Equipement { Nom = "Climatisation", Description = "Climatisation réversible individuelle" },
                new Equipement { Nom = "Sonorisation", Description = "Système audio avec micros sans fil" },
                new Equipement { Nom = "Visioconférence", Description = "Caméra PTZ, micros plafond, écran double" },
                new Equipement { Nom = "Prises électriques", Description = "Prises sur chaque rangée (1 par 2 places)" },
                new Equipement { Nom = "WiFi Haute Densité", Description = "Point d'accès dédié salle" },
                new Equipement { Nom = "Éclairage variable", Description = "Variateur d'intensité, zones séparées" },
                new Equipement { Nom = "Esthétique / Design", Description = "Mobilier design, stores occultants" }
            };
            db.Equipements.AddRange(equipements);
            db.SaveChanges();

            // ===== SALLES =====
            var batA = batiments.First(b => b.Nom == "Bâtiment A");
            var batB = batiments.First(b => b.Nom == "Bâtiment B");
            var batC = batiments.First(b => b.Nom == "Bâtiment C");
            var batD = batiments.First(b => b.Nom == "Bâtiment D");
            var biblio = batiments.First(b => b.Nom == "Bibliothèque Centrale");

            var equipProj = equipements.First(e => e.Nom == "Vidéoprojecteur");
            var equipBoard = equipements.First(e => e.Nom == "Tableau blanc");
            var equipDigital = equipements.First(e => e.Nom == "Tableau numérique");
            var equipClim = equipements.First(e => e.Nom == "Climatisation");
            var equipSound = equipements.First(e => e.Nom == "Sonorisation");
            var equipVC = equipements.First(e => e.Nom == "Visioconférence");
            var equipPower = equipements.First(e => e.Nom == "Prises électriques");
            var equipWiFi = equipements.First(e => e.Nom == "WiFi Haute Densité");
            var equipLight = equipements.First(e => e.Nom == "Éclairage variable");
            var equipDesign = equipements.First(e => e.Nom == "Esthétique / Design");

            var salles = new List<Salle>
            {
                // Bâtiment A
                new Salle { Numero = "A101", BatimentId = batA.Id, Capacite = 35, Type = "Salle de cours", Description = "Salle de cours standard", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "A102", BatimentId = batA.Id, Capacite = 40, Type = "Salle de cours", Description = "Salle de cours standard", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "A103", BatimentId = batA.Id, Capacite = 30, Type = "TD", Description = "Salle de travaux dirigés", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "A201", BatimentId = batA.Id, Capacite = 80, Type = "Amphithéâtre", Description = "Grand amphithéâtre", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipSound, equipClim, equipPower, equipWiFi, equipLight } },
                new Salle { Numero = "A202", BatimentId = batA.Id, Capacite = 60, Type = "Amphithéâtre", Description = "Moyen amphithéâtre", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipSound, equipClim, equipPower, equipWiFi } },

                // Bâtiment B
                new Salle { Numero = "B101", BatimentId = batB.Id, Capacite = 25, Type = "Salle de cours", Description = "Salle de cours Lettres", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "B102", BatimentId = batB.Id, Capacite = 28, Type = "Salle de cours", Description = "Salle de cours Lettres", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "B201", BatimentId = batB.Id, Capacite = 50, Type = "Amphithéâtre", Description = "Amphithéâtre Lettres", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipSound, equipClim, equipPower, equipWiFi } },

                // Bâtiment C
                new Salle { Numero = "C101", BatimentId = batC.Id, Capacite = 45, Type = "Salle de cours", Description = "Salle de cours Éco/Gestion", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "C102", BatimentId = batC.Id, Capacite = 40, Type = "Salle de cours", Description = "Salle de cours Éco/Gestion", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "C201", BatimentId = batC.Id, Capacite = 100, Type = "Amphithéâtre", Description = "Grand amphithéâtre Éco/Gestion", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipSound, equipClim, equipPower, equipWiFi, equipLight, equipVC } },

                // Bâtiment D
                new Salle { Numero = "D-LAB1", BatimentId = batD.Id, Capacite = 24, Type = "Laboratoire", Description = "Labo informatique - 24 postes", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipClim, equipPower, equipWiFi, equipVC } },
                new Salle { Numero = "D-LAB2", BatimentId = batD.Id, Capacite = 20, Type = "Laboratoire", Description = "Labo électronique / Arduino", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "D-ATELIER", BatimentId = batD.Id, Capacite = 30, Type = "Atelier", Description = "Atelier mécanique / impression 3D", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipBoard, equipClim, equipPower, equipWiFi } },

                // Bibliothèque
                new Salle { Numero = "BIB-S1", BatimentId = biblio.Id, Capacite = 12, Type = "Salle de travail", Description = "Salle de travail en groupe silencieuse", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipDigital, equipBoard, equipClim, equipPower, equipWiFi, equipDesign } },
                new Salle { Numero = "BIB-S2", BatimentId = biblio.Id, Capacite = 8, Type = "Salle de travail", Description = "Salle de travail en groupe", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipDigital, equipBoard, equipClim, equipPower, equipWiFi } },
                new Salle { Numero = "BIB-REUNION", BatimentId = biblio.Id, Capacite = 16, Type = "Réunion", Description = "Salle de réunion / jury", Statut = StatutSalle.Disponible, Equipements = new List<Equipement> { equipProj, equipDigital, equipVC, equipSound, equipClim, equipPower, equipWiFi, equipDesign } }
            };
            db.Salles.AddRange(salles);
            db.SaveChanges();

            // ===== UTILISATEURS (en plus de l'admin créé au premier lancement) =====
            // Note : Les mots de passe seront hashés par UtilisateurService.Save()
            // On ne peut pas les créer ici directement car le hash nécessite PasswordHasher
            // Ils seront créés via l'interface ou un script séparé

            // ===== RÉSERVATIONS D'EXEMPLE (sur les 30 prochains jours) =====
            // On crée quelques réservations pour montrer le planning
            // Nécessite des utilisateurs existants, donc on attend que l'admin crée des comptes
            // Ou on peut créer des utilisateurs "démo" ici

            AppLog.Info("Données d'exemple insérées avec succès");
        }
        catch (Exception ex)
        {
            AppLog.Error("Erreur lors du seeding de la base", ex);
        }
    }
}