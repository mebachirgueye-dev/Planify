# Planify — Phase 1 (socle technique)

Logiciel Windows de gestion de salles, réservations et plannings.
**Stack** : C# · .NET 10 · WinForms · SQLite · Entity Framework Core (2 paquets NuGet seulement).

## Prérequis
- Windows 10/11
- Visual Studio 2026, ou Visual Studio 2022 ≥ 17.14, avec la charge de travail **« Développement .NET Desktop »**
- **.NET 10 SDK** (inclus avec les versions de Visual Studio ci-dessus)

> Pour rester sur .NET 8 : dans `Planify.csproj`, remplacer `net10.0-windows` par `net8.0-windows` et les deux paquets `10.0.10` par `8.0.x`.

## Lancer
1. Ouvrir `Planify.sln` dans Visual Studio.
2. Laisser la restauration NuGet se faire (connexion Internet requise la première fois).
3. **F5**.

Au premier lancement, la base est créée automatiquement et les migrations sont appliquées.

## Où sont les données ?
```
%LocalAppData%\Planify\
    Data\planify.db     ← la base SQLite (permanente)
    Backups\            ← sauvegardes (phase 2)
    Logs\planify.log    ← erreurs inattendues
```
Ce dossier n'est jamais modifié par une mise à jour, une réinstallation ou un redémarrage du PC.
Pour repartir de zéro : fermer Planify et supprimer `planify.db`.

## Structure
```
Planify/
├── Models/        Entités (Batiment)
├── Data/          DbContext, fabrique de contextes, initialisation, fabrique "design-time"
├── Migrations/    Migrations EF Core
├── Services/      Règles métier + accès aux données (BatimentService, DatabaseInfoService)
├── Forms/         Fenêtres et boîtes de dialogue (MainForm, BatimentEditForm)
├── Pages/         Écrans affichés dans la fenêtre principale (Dashboard, Bâtiments, ...)
├── Controls/      Contrôles visuels réutilisables (NavButton, ThemedButton, CardPanel, StatCard)
├── Helpers/       Thème/couleurs du logo, chemins, journal, boîtes de message, ressources
├── Resources/     Logo, symbole, mot-clé, icône (.ico)
└── Program.cs     Point d'entrée
```

### Principes
- **Les pages n'accèdent jamais au DbContext** : elles passent par un service.
- **Erreurs métier** (champ vide, doublon, plus tard conflit horaire) : le service lève une `BusinessRuleException` dont le message est affiché tel quel à l'utilisateur.
- **Navigation** : `MainForm.BuildNavigation()` liste les écrans. Ajouter un écran = un `PageId` + une ligne.
- **Thème** : tout est dans `Helpers/Theme.cs` (couleurs mesurées sur le logo).
- **Interface construite en code** (pas de fichiers `.Designer.cs`) : plus lisible pour des contrôles personnalisés.
- `BatimentsPage` + `BatimentEditForm` + `BatimentService` forment le **modèle à copier** pour Salles, Utilisateurs, etc.

## Migrations EF Core
La migration `InitialCreate` a été **écrite à la main** (voir plus bas). Commandes habituelles
(Visual Studio → Outils → Gestionnaire de package NuGet → Console) :
```
Add-Migration NomDeLaMigration
Update-Database
```
ou en ligne de commande, depuis le dossier du projet : `dotnet ef migrations add NomDeLaMigration`
(nécessite `dotnet tool install --global dotnet-ef`).

**Si le logiciel affiche une erreur « model changes / PendingModelChanges » au démarrage**, la migration manuscrite
ne correspond pas exactement au modèle. Correction en 1 minute :
1. Supprimer le dossier `Migrations/`
2. Console du gestionnaire de package : `Add-Migration InitialCreate`

## Créer le .exe
```
dotnet publish -c Release -r win-x64 --self-contained false
```
Le résultat est dans `bin\Release\net10.0-windows\win-x64\publish\` (nécessite le runtime .NET 10 Desktop sur le PC cible).
Pour un exécutable autonome (sans runtime à installer) : `--self-contained true`.
Ou : Visual Studio → clic droit sur le projet → **Publier**.

## Checklist de test — Phase 1
- [ ] Le projet compile sans erreur
- [ ] La fenêtre s'ouvre avec le logo, l'icône Planify dans la barre des tâches et le menu à gauche
- [ ] Le fichier `%LocalAppData%\Planify\Data\planify.db` existe
- [ ] Dashboard : « Base de données : Connectée », 1 migration appliquée, 0 en attente
- [ ] Bâtiments → Ajouter un bâtiment → il apparaît dans la liste
- [ ] Doublon de nom → message clair ; nom vide → message clair
- [ ] Modifier (bouton ou double-clic) et Supprimer fonctionnent
- [ ] **Fermer puis relancer Planify : les bâtiments sont toujours là**
- [ ] Les autres entrées du menu affichent l'écran « sera ajouté dans une prochaine phase »
- [ ] Écran HiDPI (125 %, 150 %) : interface lisible, pas de texte coupé

## Phase 2 (prévu)
Utilisateurs + connexion + rôles → Salles/Équipements → Réservations + détection de conflits →
Planning → Recherche de salles → Dashboard réel → Sauvegardes/restauration → Exports.
