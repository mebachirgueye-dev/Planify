# Planify — Logiciel de gestion de salles, réservations et plannings

Application Windows Forms professionnelle pour la gestion de salles, bâtiments, utilisateurs, réservations, cours/événements, plannings et conflits horaires.

**Stack** : C# · .NET 10 · WinForms · SQLite · Entity Framework Core (2 paquets NuGet seulement : `Microsoft.EntityFrameworkCore.Sqlite` + `Microsoft.EntityFrameworkCore.Design`).

---

## Prérequis
- Windows 10/11
- Visual Studio 2026, ou Visual Studio 2022 ≥ 17.14, avec la charge de travail **« Développement .NET Desktop »**
- **.NET 10 SDK** (inclus avec les versions de Visual Studio ci-dessus)

> Pour rester sur .NET 8 : dans `Planify.csproj`, remplacer `net10.0-windows` par `net8.0-windows` et les deux paquets `10.0.10` par `8.0.x`.

---

## Lancer
1. Ouvrir `Planify.sln` dans Visual Studio.
2. Laisser la restauration NuGet se faire (connexion Internet requise la première fois).
3. **F5**.

Au premier lancement, la base est créée automatiquement, les migrations sont appliquées et des **données d'exemple** sont insérées (5 bâtiments, 10 équipements, 17 salles).

---

## Où sont les données ?
```
%LocalAppData%\Planify\
    Data\planify.db     ← la base SQLite (permanente)
    Backups\            ← sauvegardes manuelles
    Logs\planify.log    ← erreurs inattendues
```
Ce dossier n'est jamais modifié par une mise à jour, une réinstallation ou un redémarrage du PC.
Pour repartir de zéro : fermer Planify et supprimer `planify.db`.

---

## Structure du projet
```
Planify/
├── Models/        7 entités (Batiment, Utilisateur, Salle, Equipement, Reservation, CoursEvenement, enums)
├── Data/          DbContext, Factory, Initializer, Seeder (données d'exemple), Migrations (4)
├── Services/      10 services (Auth, Batiment, Salle, Equipement, Reservation, CoursEvenement, Utilisateur, DatabaseInfo, Backup, Exporters)
├── Forms/         9 formulaires (Main, Login, FirstRun, 6 EditForms, ExportDialog)
├── Pages/         10 pages (Dashboard, Batiments, Salles, RechercheSalles, Equipements, Planning, Reservations, CoursEvenements, Utilisateurs, Parametres, Rapports)
├── Controls/      4 contrôles custom (NavButton, ThemedButton, CardPanel, StatCard)
├── Helpers/       Theme (couleurs logo, HiDPI), PasswordHasher, AppPaths, AppLog, Dialogs, AppResources, GraphicsHelper
└── Resources/     Logo, symbole, wordmark, icône (.ico)
```

---

## Fonctionnalités implémentées

### 🔐 Authentification & Rôles
- Premier lancement → création compte **Administrateur** (hash PBKDF2)
- Connexion email/mot de passe
- 3 rôles : **Administrateur** (accès complet), **Gestionnaire** (salles, planning, réservations), **Utilisateur** (consultation, réservation)
- Menu filtré selon le rôle

### 🏢 Bâtiments (CRUD complet)
- Nom, adresse, étages, description
- Recherche, export CSV/PDF

### 🏫 Salles (CRUD complet)
- Numéro, bâtiment, capacité, type, description, statut
- Équipements (many-to-many)
- Recherche, export

### 🔧 Équipements (CRUD complet)
- Nom, description
- Association aux salles
- Export

### 📅 Réservations (CRUD + conflits)
- Salle, utilisateur, date, heures, motif, statut
- **Détection conflits** : même salle même horaire, même utilisateur même horaire, salle occupée par un cours
- Annulation (sans suppression)
- Export

### 📚 Cours / Événements (CRUD + conflits)
- Nom, description, date, heures, salle, responsable, statut
- **Détection conflits** : salle déjà réservée ou déjà affectée à un autre cours
- Export

### 🔍 Recherche de salles avancée
- Filtres : capacité min, bâtiment, type, équipements requis, date/heure dispo
- Résultats avec équipements affichés
- Bouton "Réserver" direct, export

### 📊 Planning (3 vues)
- **Jour** : liste créneaux (réservations + cours) triés par heure
- **Semaine** : grille 7 jours × salles avec détails
- **Mois** : résumé par semaine + total
- Navigation jour/semaine/mois, filtre par salle

### 📈 Dashboard temps réel
- Statistiques : bâtiments, salles, équipements, utilisateurs
- Réservations du jour, cours à venir
- État base de données (taille, migrations, chemin)

### 📋 Rapports
- Vue d'ensemble période (30 jours par défaut)
- Taux occupation global + par bâtiment
- Top 5 salles, Top 5 utilisateurs
- Répartition par type de salle
- Équipements les plus demandés
- Export CSV/PDF

### ⚙️ Paramètres / Sauvegardes
- Sauvegarde manuelle (choix dossier)
- Restauration (avec copie de sécurité auto + redémarrage requis)
- Liste historique, nettoyage (garde N dernières)
- Ouverture dossier, changement dossier temporaire

### 📤 Exports (sur toutes les pages listes)
- **CSV** : fonctionnel, UTF-8 avec BOM, séparateur `;`
- **PDF** : générateur natif sans dépendance (tableau simple, Helvetica)
- **Excel** : placeholder (nécessite EPPlus/ClosedXML)

---

## Migrations EF Core
4 migrations appliquées automatiquement au démarrage :
1. `InitialCreate` : schéma de base
2. `AddUtilisateurs` : table Utilisateurs
3. `AddSallesEquipementsReservations` : salles, équipements, réservations
4. `AddCoursEvenements` : cours/événements

Commandes (Console Package Manager) :
```
Add-Migration NomMigration
Update-Database
```
ou CLI : `dotnet ef migrations add NomMigration` (nécessite `dotnet tool install --global dotnet-ef`).

Si erreur « model changes / PendingModelChanges » :
1. Supprimer dossier `Migrations/`
2. `Add-Migration InitialCreate`

---

## Créer le .exe
```
dotnet publish -c Release -r win-x64 --self-contained false
```
Résultat dans `bin\Release\net10.0-windows\win-x64\publish\` (nécessite runtime .NET 10 Desktop).
Autonome (sans runtime) : `--self-contained true`.
Ou : Visual Studio → clic droit projet → **Publier**.

---

## Architecture & Principes
- **Clean Architecture simplifiée** : Pages → Services → DbContext (jamais DbContext direct dans les Pages)
- **Erreurs métier** : `BusinessRuleException` avec message utilisateur clair
- **Navigation** : `MainForm.BuildNavigation()` définit le menu. Ajouter un écran = 1 `PageId` + 1 ligne
- **Thème** : tout dans `Theme.cs` (couleurs mesurées sur le logo, scaling HiDPI via `Px()`)
- **UI en code** : pas de `.Designer.cs` - construction programmatique
- **DataGridView** : binding typé, `DataTable` pour vues dynamiques (Semaine/Mois)

---

## Checklist de test (version complète)
- [ ] Projet compile sans erreur (0 avertissements)
- [ ] Premier lancement : création admin → Dashboard avec données d'exemple
- [ ] Connexion admin/gestionnaire/utilisateur → menu filtré par rôle
- [ ] Bâtiments / Salles / Équipements / Utilisateurs : CRUD + export
- [ ] Réservations : création, conflit même salle détecté, conflit même utilisateur détecté, conflit avec cours détecté
- [ ] Cours/Événements : création, conflit salle réservée détecté, conflit autre cours détecté
- [ ] Recherche salles : filtres capacité/bâtiment/type/équipements/date/heure → résultats
- [ ] Planning : bascule Jour/Semaine/Mois, navigation, filtre salle
- [ ] Dashboard : stats temps réel + état BDD
- [ ] Rapports : période configurable, toutes sections, export
- [ ] Paramètres : sauvegarde, restauration, nettoyage, dossier
- [ ] HiDPI (125%, 150%, 200%) : interface lisible, pas de texte coupé
- [ ] Redémarrage app : données persistantes

---

## Prochaines améliorations possibles
1. **Sauvegarde automatique planifiée** (tâche de fond quotidienne/hebdo)
2. **Librairies Excel/PDF avancées** (EPPlus, QuestPDF) pour exports riches
3. **Planning visuel type calendrier** (grille heures × jours avec cellules colorées)
4. **Notifications/rappels** (réservations à venir, conflits)
5. **API REST** pour intégrations externes
6. **Gestion des utilisateurs** : modification mot de passe, récupération, désactivation

---

## Licence
Projet développé dans le cadre d'un exercice de développement logiciel. Code source disponible pour étude et réutilisation.