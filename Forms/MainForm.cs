using Microsoft.EntityFrameworkCore;
using Planify.Controls;
using Planify.Data;
using Planify.Helpers;
using Planify.Models;
using Planify.Pages;
using Planify.Services;

namespace Planify.Forms;

/// <summary>
/// Fenêtre principale : menu latéral à gauche, page courante à droite.
/// Navigation : cliquer sur une entrée du menu crée la page correspondante (données toujours à jour)
/// et détruit la précédente. Pour ajouter un écran : ajouter un PageId, puis une ligne dans BuildNavigation().
/// </summary>
public sealed class MainForm : Form
{
    private sealed record NavItem(PageId Id, string Label, string Subtitle, Func<Control> CreatePage, Role MinimumRole = Role.Utilisateur);
    private sealed record NavSection(string? Caption, NavItem[] Items);

    private readonly NavSection[] _sections;
    private readonly Dictionary<PageId, NavItem> _items = new();
    private readonly Dictionary<PageId, NavButton> _buttons = new();

    private readonly Panel _pageHost = new();
    private readonly Label _titleLabel = new();
    private readonly Label _subtitleLabel = new();
    private Control? _currentPage;

    public MainForm(IDbContextFactory<PlanifyDbContext> dbFactory)
    {
        var batiments = new BatimentService(dbFactory);
        var utilisateurs = new UtilisateurService(dbFactory);
        var databaseInfo = new DatabaseInfoService(dbFactory);

        // Le menu est filtré selon le rôle de l'utilisateur connecté :
        // une entrée n'apparaît que si le rôle courant a au moins le niveau requis.
        var all = BuildNavigation(batiments, utilisateurs, databaseInfo, dbFactory);
        _sections = all
            .Select(s => new NavSection(s.Caption, s.Items.Where(i => Session.HasRole(i.MinimumRole)).ToArray()))
            .Where(s => s.Items.Length > 0)
            .ToArray();

        foreach (var item in _sections.SelectMany(s => s.Items))
            _items[item.Id] = item;

        InitializeWindow();
        NavigateTo(PageId.Dashboard);
    }

    // ------------------------------------------------------------------ Définition du menu

    private static NavSection[] BuildNavigation(BatimentService batiments, UtilisateurService utilisateurs, DatabaseInfoService databaseInfo, IDbContextFactory<PlanifyDbContext> dbFactory)
    {
        // Services pour Phase 2b et 3
        var equipements = new EquipementService(dbFactory);
        var salles = new SalleService(dbFactory);
        var reservations = new ReservationService(dbFactory);
        var cours = new CoursEvenementService(dbFactory);

        return new[]
        {
            new NavSection(null, new[]
            {
                new NavItem(PageId.Dashboard, "Dashboard", "Vue d'ensemble de votre activité",
                    () => new DashboardPage(batiments, databaseInfo, salles, reservations))
            }),
            new NavSection("RESSOURCES", new[]
            {
                new NavItem(PageId.Salles, "Salles", "Gérez les salles et leurs équipements",
                    () => new SallesPage(salles, batiments, equipements), Role.Gestionnaire),
                new NavItem(PageId.RechercheSalles, "Recherche salles", "Trouvez une salle selon capacité, équipements, disponibilité",
                    () => new RechercheSallesPage(salles, batiments, equipements), Role.Gestionnaire),
                new NavItem(PageId.Batiments, "Bâtiments", "Gérez les bâtiments de votre établissement",
                    () => new BatimentsPage(batiments), Role.Gestionnaire)
            }),
            new NavSection("PLANIFICATION", new[]
            {
                new NavItem(PageId.Planning, "Planning", "Consultez l'occupation des salles par jour, semaine ou mois",
                    () => new PlanningPage(reservations, salles, cours)),
                new NavItem(PageId.Reservations, "Réservations", "Créez et suivez les réservations de salles",
                    () => new ReservationsPage(reservations, salles, utilisateurs)),
                new NavItem(PageId.Evenements, "Cours / Événements", "Organisez les cours et les événements",
                    () => new CoursEvenementsPage(cours, salles, utilisateurs))
            }),
            new NavSection("ADMINISTRATION", new[]
            {
                new NavItem(PageId.Utilisateurs, "Utilisateurs", "Gérez les comptes et les rôles",
                    () => new UtilisateursPage(utilisateurs), Role.Administrateur),
                new NavItem(PageId.Parametres, "Paramètres", "Sauvegardes, restauration et préférences",
                    () => new ParametresPage(new BackupService(dbFactory)), Role.Administrateur),
                new NavItem(PageId.Rapports, "Rapports", "Statistiques d'utilisation et exports",
                    () => new RapportsPage(salles, reservations, cours, batiments, utilisateurs, equipements), Role.Gestionnaire)
            })
        };
    }

    // ------------------------------------------------------------------ Construction de la fenêtre

    private void InitializeWindow()
    {
        Text = "Planify";
        Icon = AppResources.LoadIcon("planify.ico");
        Font = Theme.Body;
        BackColor = Theme.Background;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(Theme.Px(1280), Theme.Px(780));
        MinimumSize = new Size(Theme.Px(1000), Theme.Px(660));

        // Zone de droite : bandeau de titre + page courante
        _pageHost.Dock = DockStyle.Fill;
        _pageHost.BackColor = Theme.Background;

        var topBar = new Panel { Dock = DockStyle.Top, Height = Theme.Px(88), BackColor = Theme.Background };
        _titleLabel.AutoSize = true;
        _titleLabel.Font = Theme.Title;
        _titleLabel.ForeColor = Theme.Navy;
        _titleLabel.Location = new Point(Theme.Px(28), Theme.Px(14));
        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Font = Theme.Body;
        _subtitleLabel.ForeColor = Theme.TextMuted;
        _subtitleLabel.Location = new Point(Theme.Px(30), Theme.Px(54));
        topBar.Controls.Add(_titleLabel);
        topBar.Controls.Add(_subtitleLabel);

        var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        content.Controls.Add(_pageHost);   // Fill : ajouté en premier
        content.Controls.Add(topBar);      // Top : ajouté ensuite
        _pageHost.BringToFront();

        var sidebar = BuildSidebar();

        Controls.Add(content);
        Controls.Add(sidebar);
        content.BringToFront();
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = Theme.Px(260), BackColor = Theme.Surface };

        // Séparateur vertical à droite du menu
        var border = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Border };

        // En-tête : logo (symbole + mot "Planify")
        var header = new Panel { Dock = DockStyle.Top, Height = Theme.Px(88), BackColor = Theme.Surface };
        var markImage = AppResources.LoadImage("logo_mark.png", Theme.Px(44));
        var wordmarkImage = AppResources.LoadImage("logo_wordmark.png", Theme.Px(26));
        var mark = new PictureBox
        {
            Image = markImage,
            Size = markImage.Size,
            Location = new Point(Theme.Px(24), (header.Height - markImage.Height) / 2),
            BackColor = Theme.Surface
        };
        var wordmark = new PictureBox
        {
            Image = wordmarkImage,
            Size = wordmarkImage.Size,
            Location = new Point(mark.Right + Theme.Px(10), (header.Height - wordmarkImage.Height) / 2 + Theme.Px(2)),
            BackColor = Theme.Surface
        };
        header.Controls.Add(mark);
        header.Controls.Add(wordmark);

        // Pied de menu : utilisateur connecté + déconnexion + version
        var footer = BuildUserFooter();

        // Entrées du menu
        var nav = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(0, Theme.Px(4), 0, 0)
        };

        var navControls = new List<Control>();
        foreach (var section in _sections)
        {
            if (section.Caption is not null)
            {
                navControls.Add(new Label
                {
                    Text = section.Caption,
                    Dock = DockStyle.Top,
                    Height = Theme.Px(38),
                    Font = Theme.SmallBold,
                    ForeColor = Theme.TextMuted,
                    TextAlign = ContentAlignment.BottomLeft,
                    Padding = new Padding(Theme.Px(28), 0, 0, Theme.Px(6))
                });
            }

            foreach (var item in section.Items)
            {
                var button = new NavButton { Text = item.Label, Dock = DockStyle.Top };
                var id = item.Id;
                button.Click += (_, _) => NavigateTo(id);
                _buttons[id] = button;
                navControls.Add(button);
            }
        }

        // Avec Dock = Top, le dernier contrôle ajouté est placé tout en haut : on ajoute donc à l'envers.
        for (int i = navControls.Count - 1; i >= 0; i--)
            nav.Controls.Add(navControls[i]);

        // Ordre d'ajout : Fill d'abord, puis les bords (le dernier ajouté est positionné en premier).
        sidebar.Controls.Add(nav);
        sidebar.Controls.Add(header);
        sidebar.Controls.Add(footer);
        sidebar.Controls.Add(border);
        nav.BringToFront();
        return sidebar;
    }

    /// <summary>
    /// Pied du menu : nom et rôle de l'utilisateur connecté, bouton de déconnexion et version.
    /// </summary>
    private Control BuildUserFooter()
    {
        var user = Session.Current;
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = Theme.Px(132),
            BackColor = Theme.Surface
        };

        // Ligne de séparation en haut du pied
        var separator = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };

        var name = new Label
        {
            Text = user?.NomComplet ?? "Utilisateur",
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Location = new Point(Theme.Px(24), Theme.Px(14))
        };

        var role = new Label
        {
            Text = user is not null ? user.Role.ToString() : string.Empty,
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Location = new Point(Theme.Px(24), name.Bottom + Theme.Px(2))
        };

        var logout = new ThemedButton
        {
            Text = "Déconnexion",
            Kind = ButtonKind.Secondary,
            Width = Theme.Px(130),
            Height = Theme.Px(34),
            Location = new Point(Theme.Px(24), role.Bottom + Theme.Px(8))
        };
        logout.Click += (_, _) =>
        {
            if (Dialogs.Confirm("Voulez-vous vraiment vous déconnecter ?", this))
                Close();
        };

        var version = typeof(MainForm).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
        var versionLabel = new Label
        {
            Text = $"v{version}",
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Location = new Point(Theme.Px(24), logout.Bottom + Theme.Px(6))
        };

        panel.Controls.Add(name);
        panel.Controls.Add(role);
        panel.Controls.Add(logout);
        panel.Controls.Add(versionLabel);
        panel.Controls.Add(separator);
        return panel;
    }

    // ------------------------------------------------------------------ Navigation

    /// <summary>Affiche la page demandée dans la zone de droite et met à jour le menu et le titre.</summary>
    public void NavigateTo(PageId id)
    {
        var item = _items[id];

        Control page;
        try
        {
            page = item.CreatePage();
        }
        catch (Exception ex)
        {
            AppLog.Error($"Impossible d'ouvrir la page {id}", ex);
            Dialogs.Error($"Impossible d'ouvrir « {item.Label} ».{Environment.NewLine}{ex.Message}", this);
            return;
        }

        SuspendLayout();

        var previous = _currentPage;
        page.Dock = DockStyle.Fill;
        _pageHost.Controls.Add(page);
        _currentPage = page;

        if (previous is not null)
        {
            _pageHost.Controls.Remove(previous);
            previous.Dispose();
        }

        _titleLabel.Text = item.Label;
        _subtitleLabel.Text = item.Subtitle;
        foreach (var (pageId, button) in _buttons)
            button.IsActive = pageId == id;

        ResumeLayout(true);
    }
}
