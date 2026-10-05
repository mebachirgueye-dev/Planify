using Planify.Controls;
using Planify.Forms;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Pages;

/// <summary>
/// Page Rapports : statistiques d'utilisation, taux d'occupation, top salles, etc.
/// Accessible aux Gestionnaires et Administrateurs.
/// </summary>
public sealed class RapportsPage : UserControl
{
    private readonly SalleService _salleService;
    private readonly ReservationService _reservationService;
    private readonly CoursEvenementService _coursService;
    private readonly BatimentService _batimentService;
    private readonly UtilisateurService _utilisateurService;
    private readonly EquipementService _equipementService;

    private readonly DateTimePicker _dateDebut = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _dateFin = new() { Format = DateTimePickerFormat.Short };
    private readonly Button _refreshButton = new();
    private readonly ThemedButton _exportButton = new();

    private readonly TableLayoutPanel _statsContainer = new();

    public RapportsPage(
        SalleService salleService,
        ReservationService reservationService,
        CoursEvenementService coursService,
        BatimentService batimentService,
        UtilisateurService utilisateurService,
        EquipementService equipementService)
    {
        _salleService = salleService;
        _reservationService = reservationService;
        _coursService = coursService;
        _batimentService = batimentService;
        _utilisateurService = utilisateurService;
        _equipementService = equipementService;

        BackColor = Theme.Background;
        Padding = new Padding(Theme.Px(28), Theme.Px(8), Theme.Px(28), Theme.Px(28));

        var card = BuildStatsCard();
        var toolbar = BuildToolbar();

        Controls.Add(card);
        Controls.Add(toolbar);
        card.BringToFront();

        _dateDebut.Value = DateTime.Today.AddDays(-30);
        _dateFin.Value = DateTime.Today;
        RefreshStats();
    }

    private Control BuildToolbar()
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Theme.Px(60),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var leftPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Background,
            Margin = new Padding(0, Theme.Px(8), 0, 0)
        };

        var label1 = new Label
        {
            Text = "Période :",
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Margin = new Padding(0, Theme.Px(4), Theme.Px(8), 0)
        };
        leftPanel.Controls.Add(label1);

        _dateDebut.Font = Theme.Body;
        _dateDebut.Width = Theme.Px(140);
        _dateDebut.Margin = new Padding(0, 0, Theme.Px(10), 0);
        leftPanel.Controls.Add(_dateDebut);

        var label2 = new Label
        {
            Text = "au",
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, Theme.Px(6), Theme.Px(10), 0)
        };
        leftPanel.Controls.Add(label2);

        _dateFin.Font = Theme.Body;
        _dateFin.Width = Theme.Px(140);
        _dateFin.Margin = new Padding(0, 0, Theme.Px(20), 0);
        leftPanel.Controls.Add(_dateFin);

        _refreshButton.Text = "Actualiser";
        _refreshButton.Font = Theme.BodyBold;
        _refreshButton.Size = new Size(Theme.Px(110), Theme.Px(34));
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.FlatAppearance.BorderSize = 1;
        _refreshButton.FlatAppearance.BorderColor = Theme.Border;
        _refreshButton.BackColor = Theme.Surface;
        _refreshButton.ForeColor = Theme.Navy;
        _refreshButton.Cursor = Cursors.Hand;
        _refreshButton.Click += (_, _) => RefreshStats();
        _refreshButton.Margin = new Padding(0, Theme.Px(2), 0, 0);
        leftPanel.Controls.Add(_refreshButton);

        var rightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Theme.Background,
            Margin = new Padding(0, Theme.Px(8), 0, 0)
        };

        _exportButton.Text = "Exporter (CSV)";
        _exportButton.Kind = ButtonKind.Secondary;
        _exportButton.Width = Theme.Px(150);
        _exportButton.Click += (_, _) => ExportStats();
        rightPanel.Controls.Add(_exportButton);

        toolbar.Controls.Add(leftPanel, 0, 0);
        toolbar.Controls.Add(rightPanel, 1, 0);
        return toolbar;
    }

    private Control BuildStatsCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Px(24)) };

        _statsContainer.Dock = DockStyle.Fill;
        _statsContainer.ColumnCount = 2;
        _statsContainer.RowCount = 0; // dynamique
        _statsContainer.AutoSize = true;
        _statsContainer.BackColor = Theme.Surface;
        _statsContainer.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
        _statsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _statsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Surface
        };
        scroll.Controls.Add(_statsContainer);

        card.Controls.Add(scroll);
        return card;
    }

    private void RefreshStats()
    {
        _statsContainer.Controls.Clear();
        _statsContainer.RowCount = 0;
        _statsContainer.RowStyles.Clear();

        var debut = _dateDebut.Value.Date;
        var fin = _dateFin.Value.Date.AddDays(1).AddTicks(-1); // fin de journée

        int row = 0;

        // --- Section 1: Vue d'ensemble ---
        AddSectionHeader(row++, "📊 Vue d'ensemble de la période");
        AddStatRow(row++, "Nombre de bâtiments", _batimentService.Count().ToString());
        AddStatRow(row++, "Nombre de salles", _salleService.Count().ToString());
        AddStatRow(row++, "Nombre d'équipements", _equipementService.Count().ToString());
        AddStatRow(row++, "Nombre d'utilisateurs", _utilisateurService.Count().ToString());

        // Réservations dans la période
        var reservations = _reservationService.GetAll()
            .Where(r => r.Date >= debut && r.Date <= fin && r.Statut == StatutReservation.Confirmee)
            .ToList();
        var cours = _coursService.GetAll()
            .Where(c => c.Date >= debut && c.Date <= fin && c.Statut != StatutEvenement.Annule)
            .ToList();

        AddStatRow(row++, "Réservations confirmées", reservations.Count.ToString());
        AddStatRow(row++, "Cours / Événements", cours.Count.ToString());
        AddStatRow(row++, "Total créneaux occupés", (reservations.Count + cours.Count).ToString());

        // Jours ouvrés dans la période
        int joursOuvres = Enumerable.Range(0, (fin - debut).Days + 1)
            .Count(d => debut.AddDays(d).DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday));
        AddStatRow(row++, "Jours ouvrés dans la période", joursOuvres.ToString());

        // --- Section 2: Taux d'occupation ---
        AddSectionHeader(row++, "📈 Taux d'occupation des salles");

        var toutesSalles = _salleService.GetAll();
        int totalCreneauxPossibles = toutesSalles.Count * joursOuvres * 12; // ~12 créneaux/jour (8h-20h par tranche de 1h)
        int creneauxOccupes = reservations.Count + cours.Count;
        double tauxGlobal = totalCreneauxPossibles > 0 ? (double)creneauxOccupes / totalCreneauxPossibles * 100 : 0;

        AddStatRow(row++, "Créneaux possibles (estimation)", totalCreneauxPossibles.ToString("N0"));
        AddStatRow(row++, "Créneaux occupés", creneauxOccupes.ToString("N0"));
        AddStatRow(row++, "Taux d'occupation global", $"{tauxGlobal:0.#} %");

        // Par bâtiment
        var batiments = _batimentService.GetAll();
        foreach (var b in batiments)
        {
            var sallesBatiment = toutesSalles.Where(s => s.BatimentId == b.Id).ToList();
            if (sallesBatiment.Count == 0) continue;

            int creneauxPossiblesBat = sallesBatiment.Count * joursOuvres * 12;
            int creneauxOccupesBat = reservations.Count(r => r.Salle?.BatimentId == b.Id) +
                                     cours.Count(c => c.Salle?.BatimentId == b.Id);
            double tauxBat = creneauxPossiblesBat > 0 ? (double)creneauxOccupesBat / creneauxPossiblesBat * 100 : 0;

            AddStatRow(row++, $"  → {b.Nom}", $"{tauxBat:0.#} % ({creneauxOccupesBat}/{creneauxPossiblesBat} créneaux)");
        }

        // --- Section 3: Top salles ---
        AddSectionHeader(row++, "🏆 Top 5 des salles les plus utilisées");

        var reservationSalleIds = reservations.Select(r => r.SalleId).ToList();
        var coursSalleIds = cours.Select(c => c.SalleId).ToList();
        var allSalleIds = reservationSalleIds.Concat(coursSalleIds).ToList();

        var topSalles = allSalleIds
            .GroupBy(id => id)
            .Select(g => new { SalleId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        if (topSalles.Count == 0)
        {
            AddStatRow(row++, "Aucune utilisation", "—");
        }
        else
        {
            int rank = 1;
            foreach (var ts in topSalles)
            {
                var salle = toutesSalles.FirstOrDefault(s => s.Id == ts.SalleId);
                AddStatRow(row++, $"  #{rank} {salle?.Numero ?? "Inconnue"} ({salle?.Batiment?.Nom ?? ""})", $"{ts.Count} créneaux");
                rank++;
            }
        }

        // --- Section 4: Top utilisateurs ---
        AddSectionHeader(row++, "👥 Top 5 des utilisateurs (réservations)");

        var topUsers = reservations
            .GroupBy(r => r.UtilisateurId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        if (topUsers.Count == 0)
        {
            AddStatRow(row++, "Aucune réservation", "—");
        }
        else
        {
            int rank = 1;
            var allUsers = _utilisateurService.GetAll();
            foreach (var tu in topUsers)
            {
                var user = allUsers.FirstOrDefault(u => u.Id == tu.UserId);
                AddStatRow(row++, $"  #{rank} {user?.NomComplet ?? "Inconnu"}", $"{tu.Count} réservations");
                rank++;
            }
        }

        // --- Section 5: Répartition par type de salle ---
        AddSectionHeader(row++, "📋 Répartition par type de salle");

        var types = toutesSalles
            .Where(s => !string.IsNullOrWhiteSpace(s.Type))
            .GroupBy(s => s.Type!)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (types.Count == 0)
        {
            AddStatRow(row++, "Aucun type défini", "—");
        }
        else
        {
            foreach (var t in types)
            {
                int usage = reservations.Count(r => r.Salle?.Type == t.Type) + cours.Count(c => c.Salle?.Type == t.Type);
                AddStatRow(row++, $"  {t.Type}", $"{t.Count} salle(s), {usage} créneaux");
            }
        }

        // --- Section 6: Équipements les plus demandés ---
        AddSectionHeader(row++, "🔧 Équipements les plus présents");

        var equipements = _equipementService.GetAll();
        var equipementUsage = equipements
            .Select(eq => new
            {
                eq.Nom,
                SalleCount = toutesSalles.Count(s => s.Equipements.Any(e => e.Id == eq.Id)),
                UsageCount = reservations.Count(r => r.Salle?.Equipements.Any(e => e.Id == eq.Id) == true) +
                              cours.Count(c => c.Salle?.Equipements.Any(e => e.Id == eq.Id) == true)
            })
            .Where(x => x.SalleCount > 0)
            .OrderByDescending(x => x.UsageCount)
            .Take(10)
            .ToList();

        if (equipementUsage.Count == 0)
        {
            AddStatRow(row++, "Aucun équipement utilisé", "—");
        }
        else
        {
            foreach (var eu in equipementUsage)
            {
                AddStatRow(row++, $"  {eu.Nom}", $"{eu.SalleCount} salle(s), {eu.UsageCount} créneaux");
            }
        }

        _statsContainer.RowCount = row;
    }

    private void AddSectionHeader(int row, string text)
    {
        _statsContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Px(36)));

        var label = new Label
        {
            Text = text,
            Font = Theme.BodyBold,
            ForeColor = Theme.Primary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, Theme.Px(8), 0, 0)
        };
        _statsContainer.SetColumnSpan(label, 2);
        _statsContainer.Controls.Add(label, 0, row);
    }

    private void AddStatRow(int row, string label, string value)
    {
        _statsContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Px(30)));

        var lbl = new Label
        {
            Text = label,
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statsContainer.Controls.Add(lbl, 0, row);

        var val = new Label
        {
            Text = value,
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight
        };
        _statsContainer.Controls.Add(val, 1, row);
    }

    private void ExportStats()
    {
        using var dialog = new ExportDialog(ExporterFactory.GetAll());
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        var exporter = dialog.SelectedExporter;
        using var saveDialog = new SaveFileDialog
        {
            Filter = exporter.FileFilter,
            DefaultExt = exporter.FileExtension,
            FileName = $"Rapport_{_dateDebut.Value:yyyyMMdd}_{_dateFin.Value:yyyyMMdd}{exporter.FileExtension}"
        };

        if (saveDialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            // Collecter toutes les stats pour l'export
            var debut = _dateDebut.Value.Date;
            var fin = _dateFin.Value.Date.AddDays(1).AddTicks(-1);
            var reservations = _reservationService.GetAll()
                .Where(r => r.Date >= debut && r.Date <= fin && r.Statut == StatutReservation.Confirmee)
                .ToList();
            var cours = _coursService.GetAll()
                .Where(c => c.Date >= debut && c.Date <= fin && c.Statut != StatutEvenement.Annule)
                .ToList();

            var exportData = new List<dynamic>();

            // Vue d'ensemble
            exportData.Add(new { Categorie = "Vue d'ensemble", Metrique = "Bâtiments", Valeur = _batimentService.Count() });
            exportData.Add(new { Categorie = "Vue d'ensemble", Metrique = "Salles", Valeur = _salleService.Count() });
            exportData.Add(new { Categorie = "Vue d'ensemble", Metrique = "Équipements", Valeur = _equipementService.Count() });
            exportData.Add(new { Categorie = "Vue d'ensemble", Metrique = "Utilisateurs", Valeur = _utilisateurService.Count() });
            exportData.Add(new { Categorie = "Vue d'ensemble", Metrique = "Réservations", Valeur = reservations.Count });
            exportData.Add(new { Categorie = "Vue d'ensemble", Metrique = "Cours/Événements", Valeur = cours.Count });
            exportData.Add(new { Categorie = "Vue d'ensemble", Metrique = "Total créneaux", Valeur = reservations.Count + cours.Count });

            // Top salles
            var reservationSalleIds = reservations.Select(r => r.SalleId).ToList();
            var coursSalleIds = cours.Select(c => c.SalleId).ToList();
            var allSalleIds = reservationSalleIds.Concat(coursSalleIds).ToList();

            var topSalles = allSalleIds
                .GroupBy(id => id)
                .Select(g => new { SalleId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5);
            int rank = 1;
            var toutesSalles = _salleService.GetAll();
            foreach (var ts in topSalles)
            {
                var salle = toutesSalles.FirstOrDefault(s => s.Id == ts.SalleId);
                exportData.Add(new { Categorie = "Top Salles", Metrique = $"#{rank} {salle?.Numero}", Valeur = ts.Count });
                rank++;
            }

            // Top utilisateurs
            var topUsers = reservations
                .GroupBy(r => r.UtilisateurId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5);
            rank = 1;
            var allUsers = _utilisateurService.GetAll();
            foreach (var tu in topUsers)
            {
                var user = allUsers.FirstOrDefault(u => u.Id == tu.UserId);
                exportData.Add(new { Categorie = "Top Utilisateurs", Metrique = $"#{rank} {user?.NomComplet}", Valeur = tu.Count });
                rank++;
            }

            // Types de salle
            var types = toutesSalles
                .Where(s => !string.IsNullOrWhiteSpace(s.Type))
                .GroupBy(s => s.Type!)
                .Select(g => new { Type = g.Key, Count = g.Count() });
            foreach (var t in types)
            {
                int usage = reservations.Count(r => r.Salle?.Type == t.Type) + cours.Count(c => c.Salle?.Type == t.Type);
                exportData.Add(new { Categorie = "Types de salle", Metrique = t.Type, Valeur = $"{t.Count} salles, {usage} créneaux" });
            }

            exporter.ExportAsync(exportData, saveDialog.FileName).GetAwaiter().GetResult();
            Dialogs.Info($"Export réussi :\n{saveDialog.FileName}", this);
        }
        catch (Exception ex)
        {
            AppLog.Error("Export rapports", ex);
            Dialogs.Error($"Erreur lors de l'export : {ex.Message}", this);
        }
    }
}