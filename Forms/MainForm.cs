using Microsoft.EntityFrameworkCore;
using Planify.Controls;
using Planify.Data;
using Planify.Helpers;
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
    private sealed record NavItem(PageId Id, string Label, string Subtitle, Func<Control> CreatePage);
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
        var databaseInfo = new DatabaseInfoService(dbFactory);

        _sections = BuildNavigation(batiments, databaseInfo);
        foreach (var item in _sections.SelectMany(s => s.Items))
            _items[item.Id] = item;

        InitializeWindow();
        NavigateTo(PageId.Dashboard);
    }

    // ------------------------------------------------------------------ Définition du menu

    private static NavSection[] BuildNavigation(BatimentService batiments, DatabaseInfoService databaseInfo)
    {
        static NavItem Soon(PageId id, string label, string subtitle) =>
            new(id, label, subtitle, () => new PlaceholderPage(label));

        return new[]
        {
            new NavSection(null, new[]
            {
                new NavItem(PageId.Dashboard, "Dashboard", "Vue d'ensemble de votre activité",
                    () => new DashboardPage(batiments, databaseInfo))
            }),
            new NavSection("RESSOURCES", new[]
            {
                Soon(PageId.Salles, "Salles", "Gérez les salles et leurs équipements"),
                new NavItem(PageId.Batiments, "Bâtiments", "Gérez les bâtiments de votre établissement",
                    () => new BatimentsPage(batiments))
            }),
            new NavSection("PLANIFICATION", new[]
            {
                Soon(PageId.Planning, "Planning", "Consultez l'occupation des salles par jour ou par semaine"),
                Soon(PageId.Reservations, "Réservations", "Créez et suivez les réservations de salles"),
                Soon(PageId.Evenements, "Cours / Événements", "Organisez les cours et les événements")
            }),
            new NavSection("ADMINISTRATION", new[]
            {
                Soon(PageId.Utilisateurs, "Utilisateurs", "Gérez les comptes et les rôles"),
                Soon(PageId.Rapports, "Rapports", "Statistiques d'utilisation et exports"),
                Soon(PageId.Parametres, "Paramètres", "Préférences, sauvegardes et restauration")
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

        // Pied de menu : version
        var version = typeof(MainForm).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
        var footer = new Label
        {
            Dock = DockStyle.Bottom,
            Height = Theme.Px(44),
            Text = $"Planify v{version}",
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(Theme.Px(28), 0, 0, 0)
        };

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
