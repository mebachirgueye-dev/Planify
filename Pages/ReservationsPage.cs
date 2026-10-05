using Planify.Controls;
using Planify.Forms;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;
using System.IO;

namespace Planify.Pages;

/// <summary>
/// Liste des réservations confirmées avec recherche, ajout, modification et annulation.
/// Affiche la salle, l'utilisateur, la date et la plage horaire.
/// </summary>
public sealed class ReservationsPage : UserControl
{
    private readonly ReservationService _service;
    private readonly SalleService _salleService;
    private readonly UtilisateurService _utilisateurService;

    private readonly TextBox _search = new();
    private readonly DateTimePicker _dateFilter = new() { Format = DateTimePickerFormat.Short };
    private readonly Button _filterButton = new();
    private readonly Button _clearFilterButton = new();
    private readonly DataGridView _grid = new();
    private readonly Label _emptyLabel = new();
    private readonly ThemedButton _addButton = new();
    private readonly ThemedButton _editButton = new();
    private readonly ThemedButton _cancelButton = new();
    private readonly ThemedButton _exportButton = new();

    private List<Reservation> _all = new();
    private DateTime? _filterDate;

    public ReservationsPage(ReservationService service, SalleService salleService, UtilisateurService utilisateurService)
    {
        _service = service;
        _salleService = salleService;
        _utilisateurService = utilisateurService;

        BackColor = Theme.Background;
        Padding = new Padding(Theme.Px(28), Theme.Px(8), Theme.Px(28), Theme.Px(28));

        var card = BuildGridCard();
        var toolbar = BuildToolbar();

        Controls.Add(card);
        Controls.Add(toolbar);
        card.BringToFront();

        LoadData();
    }

    private Control BuildToolbar()
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Theme.Px(56),
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Background
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Barre de recherche + filtres de date
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };

        _search.Width = Theme.Px(240);
        _search.Font = Theme.Body;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.PlaceholderText = "Rechercher une réservation…";
        _search.Anchor = AnchorStyles.Left;
        _search.TextChanged += (_, _) => ApplyFilter();
        _search.Margin = new Padding(0, Theme.Px(9), Theme.Px(10), Theme.Px(9));
        filterPanel.Controls.Add(_search);

        _dateFilter.Font = Theme.Body;
        _dateFilter.Value = DateTime.Now.Date;
        _dateFilter.Margin = new Padding(0, Theme.Px(9), Theme.Px(6), Theme.Px(9));
        filterPanel.Controls.Add(_dateFilter);

        _filterButton.Text = "Filtrer";
        _filterButton.Font = Theme.BodyBold;
        _filterButton.Size = new Size(Theme.Px(70), Theme.Px(34));
        _filterButton.FlatStyle = FlatStyle.Flat;
        _filterButton.FlatAppearance.BorderSize = 1;
        _filterButton.FlatAppearance.BorderColor = Theme.Border;
        _filterButton.BackColor = Theme.Surface;
        _filterButton.ForeColor = Theme.Navy;
        _filterButton.Cursor = Cursors.Hand;
        _filterButton.Click += (_, _) =>
        {
            _filterDate = _dateFilter.Value.Date;
            ApplyFilter();
        };
        _filterButton.Margin = new Padding(0, Theme.Px(9), Theme.Px(6), Theme.Px(9));
        filterPanel.Controls.Add(_filterButton);

        _clearFilterButton.Text = "Réinitialiser";
        _clearFilterButton.Font = Theme.BodyBold;
        _clearFilterButton.Size = new Size(Theme.Px(90), Theme.Px(34));
        _clearFilterButton.FlatStyle = FlatStyle.Flat;
        _clearFilterButton.FlatAppearance.BorderSize = 1;
        _clearFilterButton.FlatAppearance.BorderColor = Theme.Border;
        _clearFilterButton.BackColor = Theme.Surface;
        _clearFilterButton.ForeColor = Theme.Navy;
        _clearFilterButton.Cursor = Cursors.Hand;
        _clearFilterButton.Click += (_, _) =>
        {
            _filterDate = null;
            _search.Clear();
            ApplyFilter();
        };
        _clearFilterButton.Margin = new Padding(0, Theme.Px(9), 0, Theme.Px(9));
        filterPanel.Controls.Add(_clearFilterButton);

        // Boutons d'action
        _addButton.Text = "Nouvelle réservation";
        _addButton.Kind = ButtonKind.Primary;
        _addButton.Width = Theme.Px(200);
        _addButton.Click += (_, _) => AddReservation();

        _editButton.Text = "Modifier";
        _editButton.Kind = ButtonKind.Secondary;
        _editButton.Width = Theme.Px(110);
        _editButton.Click += (_, _) => EditSelected();

        _cancelButton.Text = "Annuler";
        _cancelButton.Kind = ButtonKind.Danger;
        _cancelButton.Width = Theme.Px(110);
        _cancelButton.Click += (_, _) => CancelSelected();

        _exportButton.Text = "Exporter";
        _exportButton.Kind = ButtonKind.Secondary;
        _exportButton.Width = Theme.Px(110);
        _exportButton.Click += (_, _) => ExportData();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        foreach (var button in new[] { _addButton, _editButton, _cancelButton, _exportButton })
        {
            button.Margin = new Padding(Theme.Px(10), Theme.Px(9), 0, Theme.Px(9));
            buttons.Controls.Add(button);
        }

        toolbar.Controls.Add(filterPanel, 0, 0);
        toolbar.Controls.Add(buttons, 1, 0);
        return toolbar;
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Salle.Numero", HeaderText = "Salle", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Utilisateur.NomComplet", HeaderText = "Utilisateur", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.Date), HeaderText = "Date", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.PlageHoraire), HeaderText = "Créneau", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.Motif), HeaderText = "Motif", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.Statut), HeaderText = "Statut", FillWeight = 12 });

        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditSelected();
        };

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Font = Theme.Body;
        _emptyLabel.ForeColor = Theme.TextMuted;
        _emptyLabel.Visible = false;

        card.Controls.Add(_grid);
        card.Controls.Add(_emptyLabel);
        return card;
    }

    private Reservation? SelectedReservation => _grid.CurrentRow?.DataBoundItem as Reservation;

    private void LoadData(int? selectId = null)
    {
        _all = _service.GetAll();
        ApplyFilter(selectId);
    }

    private void ApplyFilter(int? selectId = null)
    {
        string term = _search.Text.Trim();
        List<Reservation> rows = _all
            .Where(r =>
            {
                if (_filterDate.HasValue && r.Date != _filterDate.Value)
                    return false;
                if (term.Length > 0)
                {
                    if (!Contains(r.Salle?.Numero, term)
                        && !Contains(r.Utilisateur?.NomComplet, term)
                        && !Contains(r.Motif, term))
                        return false;
                }
                return true;
            })
            .ToList();

        _grid.DataSource = rows;

        bool hasRows = rows.Count > 0;
        _grid.Visible = hasRows;
        _emptyLabel.Visible = !hasRows;
        _emptyLabel.Text = _all.Count == 0
            ? "Aucune réservation pour le moment." + Environment.NewLine + "Cliquez sur « Nouvelle réservation » pour créer la première."
            : "Aucune réservation ne correspond à cette recherche.";

        if (selectId.HasValue && hasRows)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if ((row.DataBoundItem as Reservation)?.Id == selectId.Value)
                {
                    _grid.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }

        UpdateButtons();
    }

    private static bool Contains(string? text, string term) =>
        text?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false;

    private void UpdateButtons()
    {
        bool hasSelection = SelectedReservation is not null;
        _editButton.Enabled = hasSelection;
        _cancelButton.Enabled = hasSelection;
    }

    private void AddReservation()
    {
        using var dialog = new ReservationEditForm(_service, _salleService, _utilisateurService, null);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadData(dialog.SavedId);
    }

    private void EditSelected()
    {
        var selected = SelectedReservation;
        if (selected is null) return;

        using var dialog = new ReservationEditForm(_service, _salleService, _utilisateurService, selected);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadData(dialog.SavedId);
    }

    private void CancelSelected()
    {
        var selected = SelectedReservation;
        if (selected is null) return;

        string message = $"Voulez-vous vraiment annuler cette réservation ?" +
                         Environment.NewLine + $"Salle: {selected.Salle?.Numero}, {selected.Date:dd/MM/yyyy} {selected.PlageHoraire}";
        if (!Dialogs.Confirm(message, FindForm())) return;

        try
        {
            _service.Cancel(selected.Id);
            LoadData();
        }
catch (BusinessRuleException ex)
            {
                Dialogs.Warning(ex.Message, FindForm());
            }
        }

        private void ExportData()
        {
            using var dialog = new ExportDialog(ExporterFactory.GetAll());
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            var exporter = dialog.SelectedExporter;
            using var saveDialog = new SaveFileDialog
            {
                Filter = exporter.FileFilter,
                DefaultExt = exporter.FileExtension,
                FileName = $"Reservations_{DateTime.Now:yyyyMMdd}{exporter.FileExtension}"
            };

            if (saveDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                var data = _grid.DataSource as List<Reservation> ?? _all;
                exporter.ExportAsync(data, saveDialog.FileName).GetAwaiter().GetResult();
                Dialogs.Info($"Export réussi :\n{saveDialog.FileName}", this);
            }
            catch (Exception ex)
            {
                AppLog.Error("Export réservations", ex);
                Dialogs.Error($"Erreur lors de l'export : {ex.Message}", this);
            }
        }
    }
