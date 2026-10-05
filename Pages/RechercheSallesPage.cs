using Planify.Controls;
using Planify.Forms;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Pages;

/// <summary>
/// Recherche de salles selon critères : capacité, bâtiment, type, équipements, disponibilité horaire.
/// Utilise SalleService.Search() pour la logique de filtrage et détection de conflits.
/// </summary>
public sealed class RechercheSallesPage : UserControl
{
    private readonly SalleService _salleService;
    private readonly BatimentService _batimentService;
    private readonly EquipementService _equipementService;

    private readonly NumericUpDown _capaciteMin = new() { Minimum = 1, Maximum = SalleService.MaxCapacite };
    private readonly ComboBox _batimentCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _type = new() { MaxLength = 50 };
    private readonly CheckedListBox _equipementsList = new();
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
    private readonly TextBox _heureDebut = new() { MaxLength = 5, PlaceholderText = "HH:mm" };
    private readonly TextBox _heureFin = new() { MaxLength = 5, PlaceholderText = "HH:mm" };
    private readonly ThemedButton _searchButton = new();
    private readonly ThemedButton _clearButton = new();
    private readonly DataGridView _grid = new();
    private readonly Label _emptyLabel = new();
    private readonly ThemedButton _reserverButton = new();
    private readonly ThemedButton _exportButton = new();

    private List<Salle> _results = new();

    public RechercheSallesPage(SalleService salleService, BatimentService batimentService, EquipementService equipementService)
    {
        _salleService = salleService;
        _batimentService = batimentService;
        _equipementService = equipementService;

        BackColor = Theme.Background;
        Padding = new Padding(Theme.Px(28), Theme.Px(8), Theme.Px(28), Theme.Px(28));

        InitializeButtons();

        var card = BuildGridCard();
        var toolbar = BuildToolbar();

        Controls.Add(card);
        Controls.Add(toolbar);
        card.BringToFront();

        LoadFilters();
        _date.Value = DateTime.Today;
        _date.Checked = false;
    }

    private void InitializeButtons()
    {
        _searchButton.Text = "🔍 Rechercher";
        _searchButton.Kind = ButtonKind.Primary;
        _searchButton.Width = Theme.Px(150);
        _searchButton.Click += (_, _) => Search();

        _clearButton.Text = "Effacer filtres";
        _clearButton.Kind = ButtonKind.Secondary;
        _clearButton.Width = Theme.Px(130);
        _clearButton.Click += (_, _) => ClearFilters();

        _reserverButton.Text = "Réserver la sélection";
        _reserverButton.Kind = ButtonKind.Primary;
        _reserverButton.Width = Theme.Px(200);
        _reserverButton.Click += (_, _) => ReserverSelection();
        _reserverButton.Enabled = false;

        _exportButton.Text = "Exporter résultats";
        _exportButton.Kind = ButtonKind.Secondary;
        _exportButton.Width = Theme.Px(160);
        _exportButton.Click += (_, _) => ExportResults();
    }

    private Control BuildToolbar()
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Theme.Background
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = new Padding(0, Theme.Px(4), 0, Theme.Px(12))
        };

        // Ligne 1 : Capacité + Bâtiment + Type
        var line1 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, Theme.Px(8), 0, Theme.Px(6)) };
        AddField(line1, "Capacité min :", _capaciteMin, Theme.Px(100));
        _capaciteMin.Value = 1;

        var batimentLabel = new Label { Text = "Bâtiment :", Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(Theme.Px(20), Theme.Px(4), Theme.Px(8), 0) };
        line1.Controls.Add(batimentLabel);
        _batimentCombo.Width = Theme.Px(200);
        _batimentCombo.Font = Theme.Body;
        _batimentCombo.Margin = new Padding(0, 0, Theme.Px(20), 0);
        line1.Controls.Add(_batimentCombo);

        var typeLabel = new Label { Text = "Type :", Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(Theme.Px(20), Theme.Px(4), Theme.Px(8), 0) };
        line1.Controls.Add(typeLabel);
        _type.Width = Theme.Px(150);
        _type.Font = Theme.Body;
        _type.BorderStyle = BorderStyle.FixedSingle;
        _type.PlaceholderText = "ex: Salle, Amphi, Labo";
        line1.Controls.Add(_type);
        panel.Controls.Add(line1);

        // Ligne 2 : Équipements
        var line2 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 0, 0, Theme.Px(6)) };
        var eqLabel = new Label { Text = "Équipements requis :", Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(0, Theme.Px(4), Theme.Px(8), 0) };
        line2.Controls.Add(eqLabel);
        _equipementsList.Width = Theme.Px(500);
        _equipementsList.Height = Theme.Px(80);
        _equipementsList.BorderStyle = BorderStyle.FixedSingle;
        _equipementsList.CheckOnClick = true;
        _equipementsList.Margin = new Padding(0, 0, Theme.Px(20), 0);
        line2.Controls.Add(_equipementsList);
        panel.Controls.Add(line2);

        // Ligne 3 : Date + Heures
        var line3 = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 0, 0, Theme.Px(8)) };
        var dateLabel = new Label { Text = "Date :", Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(0, Theme.Px(4), Theme.Px(8), 0) };
        line3.Controls.Add(dateLabel);
        _date.Width = Theme.Px(140);
        _date.Font = Theme.Body;
        _date.Margin = new Padding(0, 0, Theme.Px(20), 0);
        line3.Controls.Add(_date);

        var hdLabel = new Label { Text = "Heure début :", Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(Theme.Px(20), Theme.Px(4), Theme.Px(8), 0) };
        line3.Controls.Add(hdLabel);
        _heureDebut.Width = Theme.Px(80);
        _heureDebut.Font = Theme.Body;
        _heureDebut.BorderStyle = BorderStyle.FixedSingle;
        _heureDebut.Margin = new Padding(0, 0, Theme.Px(20), 0);
        line3.Controls.Add(_heureDebut);

        var hfLabel = new Label { Text = "Heure fin :", Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(Theme.Px(20), Theme.Px(4), Theme.Px(8), 0) };
        line3.Controls.Add(hfLabel);
        _heureFin.Width = Theme.Px(80);
        _heureFin.Font = Theme.Body;
        _heureFin.BorderStyle = BorderStyle.FixedSingle;
        _heureFin.Margin = new Padding(0, 0, Theme.Px(20), 0);
        line3.Controls.Add(_heureFin);
        panel.Controls.Add(line3);

        // Ligne 4 : Boutons
        var line4 = new FlowLayoutPanel 
        { 
            AutoSize = true, 
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight, 
            WrapContents = true,
            Margin = new Padding(0, Theme.Px(4), 0, 0) 
        };
        _searchButton.Margin = new Padding(0, Theme.Px(9), Theme.Px(10), Theme.Px(9));
        line4.Controls.Add(_searchButton);

        _clearButton.Margin = new Padding(Theme.Px(10), Theme.Px(9), 0, Theme.Px(9));
        line4.Controls.Add(_clearButton);

        _reserverButton.Margin = new Padding(Theme.Px(20), Theme.Px(9), 0, Theme.Px(9));
        line4.Controls.Add(_reserverButton);

        _exportButton.Margin = new Padding(Theme.Px(10), Theme.Px(9), 0, Theme.Px(9));
        line4.Controls.Add(_exportButton);

        panel.Controls.Add(line4);

        toolbar.Controls.Add(panel, 0, 0);
        return toolbar;
    }

    private void AddField(FlowLayoutPanel panel, string label, Control input, int width)
    {
        var lbl = new Label { Text = label, Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(0, Theme.Px(4), Theme.Px(8), 0) };
        panel.Controls.Add(lbl);
        input.Width = width;
        input.Font = Theme.Body;
        if (input is TextBox tb) { tb.BorderStyle = BorderStyle.FixedSingle; }
        input.Margin = new Padding(0, 0, Theme.Px(20), 0);
        panel.Controls.Add(input);
    }

    private Control BuildGridCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill };

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.BorderStyle = BorderStyle.None;
        _grid.BackgroundColor = Theme.Surface;
        _grid.GridColor = Theme.Border;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.ColumnHeadersHeight = Theme.Px(40);
        _grid.RowTemplate.Height = Theme.Px(40);
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Surface;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextMuted;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Surface;
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.TextMuted;
        _grid.ColumnHeadersDefaultCellStyle.Font = Theme.SmallBold;
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(Theme.Px(8), 0, Theme.Px(8), 0);

        _grid.DefaultCellStyle.BackColor = Theme.Surface;
        _grid.DefaultCellStyle.ForeColor = Theme.Navy;
        _grid.DefaultCellStyle.SelectionBackColor = Theme.PrimarySoft;
        _grid.DefaultCellStyle.SelectionForeColor = Theme.Navy;
        _grid.DefaultCellStyle.Font = Theme.Body;
        _grid.DefaultCellStyle.Padding = new Padding(Theme.Px(8), 0, Theme.Px(8), 0);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Salle.Numero), HeaderText = "Numéro", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Batiment.Nom", HeaderText = "Bâtiment", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Salle.Capacite), HeaderText = "Capacité", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Salle.Type), HeaderText = "Type", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Salle.Statut), HeaderText = "Statut", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EquipementsDisplay", HeaderText = "Équipements", FillWeight = 26 });

        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) ReserverSelection(); };

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Font = Theme.Body;
        _emptyLabel.ForeColor = Theme.TextMuted;
        _emptyLabel.Visible = false;
        _emptyLabel.Text = "Définissez vos critères et cliquez sur « Rechercher ».";

        card.Controls.Add(_grid);
        card.Controls.Add(_emptyLabel);
        return card;
    }

    private void LoadFilters()
    {
        // Bâtiments
        var batiments = _batimentService.GetAll();
        _batimentCombo.DataSource = batiments;
        _batimentCombo.DisplayMember = nameof(Batiment.Nom);
        _batimentCombo.ValueMember = nameof(Batiment.Id);
        _batimentCombo.SelectedIndex = -1;

        // Équipements
        var equipements = _equipementService.GetAll();
        _equipementsList.DataSource = equipements;
        _equipementsList.DisplayMember = nameof(Equipement.Nom);
    }

    private void Search()
    {
        try
        {
            int capaciteMin = (int)_capaciteMin.Value;
            int? batimentId = _batimentCombo.SelectedValue is int id ? id : null;
            string? type = string.IsNullOrWhiteSpace(_type.Text) ? null : _type.Text.Trim();

            var equipementIds = new List<int>();
            for (int i = 0; i < _equipementsList.Items.Count; i++)
            {
                if (_equipementsList.GetItemChecked(i) && _equipementsList.Items[i] is Equipement eq)
                    equipementIds.Add(eq.Id);
            }

            DateTime? date = _date.Checked ? _date.Value.Date : null;
            TimeSpan? heureDebut = null;
            TimeSpan? heureFin = null;

            if (date.HasValue)
            {
                if (TimeSpan.TryParse(_heureDebut.Text, out var hd))
                    heureDebut = hd;
                if (TimeSpan.TryParse(_heureFin.Text, out var hf))
                    heureFin = hf;
            }

            _results = _salleService.Search(capaciteMin, batimentId, type, equipementIds.Count > 0 ? equipementIds : null, date, heureDebut, heureFin);
            DisplayResults();
        }
        catch (Exception ex)
        {
            AppLog.Error("Recherche salles", ex);
            Dialogs.Error($"Erreur lors de la recherche : {ex.Message}", this);
        }
    }

    private void DisplayResults()
    {
        // Ajouter une propriété calculée pour l'affichage des équipements
        var displayList = _results.Select(s => new SalleDisplay
        {
            Salle = s,
            EquipementsDisplay = string.Join(", ", s.Equipements.Select(eq => eq.Nom))
        }).ToList();

        _grid.DataSource = displayList;

        bool hasRows = displayList.Count > 0;
        _grid.Visible = hasRows;
        _emptyLabel.Visible = !hasRows;
        _emptyLabel.Text = hasRows ? string.Empty : "Aucune salle ne correspond à ces critères.";

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasSelection = _grid.CurrentRow?.DataBoundItem is SalleDisplay;
        _reserverButton.Enabled = hasSelection;
    }

    private Salle? SelectedSalle => (_grid.CurrentRow?.DataBoundItem as SalleDisplay)?.Salle;

    private void ClearFilters()
    {
        _capaciteMin.Value = 1;
        _batimentCombo.SelectedIndex = -1;
        _type.Clear();
        for (int i = 0; i < _equipementsList.Items.Count; i++)
            _equipementsList.SetItemChecked(i, false);
        _date.Checked = false;
        _date.Value = DateTime.Today;
        _heureDebut.Clear();
        _heureFin.Clear();
        _results.Clear();
        _grid.DataSource = null;
        _grid.Visible = false;
        _emptyLabel.Visible = true;
        _emptyLabel.Text = "Définissez vos critères et cliquez sur « Rechercher ».";
        UpdateButtons();
    }

    private void ReserverSelection()
    {
        var salle = SelectedSalle;
        if (salle is null) return;

        // Ouvrir le formulaire de réservation pré-rempli
        // On a besoin du ReservationService, SalleService, UtilisateurService
        // Pour l'instant, on affiche un message indicatif
        Dialogs.Info($"Salle sélectionnée : {salle.Numero} ({salle.Batiment?.Nom})\nCapacité : {salle.Capacite} places\n\nPour réserver, utilisez le menu « Réservations » → « Nouvelle réservation » et sélectionnez cette salle.", this);
    }

    private void ExportResults()
    {
        if (_results.Count == 0)
        {
            Dialogs.Warning("Aucun résultat à exporter.", this);
            return;
        }

        using var dialog = new ExportDialog(ExporterFactory.GetAll());
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        var exporter = dialog.SelectedExporter;
        using var saveDialog = new SaveFileDialog
        {
            Filter = exporter.FileFilter,
            DefaultExt = exporter.FileExtension,
            FileName = $"RechercheSalles_{DateTime.Now:yyyyMMdd}{exporter.FileExtension}"
        };

        if (saveDialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            // Exporter avec les équipements en texte
            var exportData = _results.Select(s => new
            {
                s.Numero,
                Batiment = s.Batiment?.Nom ?? "",
                s.Capacite,
                s.Type,
                Statut = s.Statut.ToString(),
                Equipements = string.Join(", ", s.Equipements.Select(eq => eq.Nom))
            }).ToList();

            // Utiliser une liste d'objets anonymes via reflection
            exporter.ExportAsync(exportData.Cast<object>(), saveDialog.FileName).GetAwaiter().GetResult();
            Dialogs.Info($"Export réussi :\n{saveDialog.FileName}", this);
        }
        catch (Exception ex)
        {
            AppLog.Error("Export recherche salles", ex);
            Dialogs.Error($"Erreur lors de l'export : {ex.Message}", this);
        }
    }

    private sealed class SalleDisplay
    {
        public Salle Salle { get; set; } = null!;
        public string EquipementsDisplay { get; set; } = string.Empty;
        public string Numero => Salle.Numero;
        public Batiment? Batiment => Salle.Batiment;
        public int Capacite => Salle.Capacite;
        public string? Type => Salle.Type;
        public StatutSalle Statut => Salle.Statut;
    }
}