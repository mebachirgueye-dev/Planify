using Planify.Controls;
using Planify.Forms;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;
using System.IO;

namespace Planify.Pages;

/// <summary>
/// Liste des équipements avec recherche, ajout, modification et suppression.
/// Basée sur le modèle BatimentsPage.
/// </summary>
public sealed class EquipementsPage : UserControl
{
    private readonly EquipementService _service;

    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly Label _emptyLabel = new();
    private readonly ThemedButton _addButton = new();
    private readonly ThemedButton _editButton = new();
    private readonly ThemedButton _deleteButton = new();
    private readonly ThemedButton _exportButton = new();

    private List<Equipement> _all = new();

    public EquipementsPage(EquipementService service)
    {
        _service = service;

        BackColor = Theme.Background;
        Padding = new Padding(Theme.Px(28), Theme.Px(8), Theme.Px(28), Theme.Px(28));

        InitializeButtons();

        var card = BuildGridCard();
        var toolbar = BuildToolbar();

        Controls.Add(card);
        Controls.Add(toolbar);
        card.BringToFront();

        LoadData();
    }

    private void InitializeButtons()
    {
        _search.TextChanged += (_, _) => ApplyFilter();

        _addButton.Text = "Ajouter un équipement";
        _addButton.Kind = ButtonKind.Primary;
        _addButton.Width = Theme.Px(210);
        _addButton.Click += (_, _) => AddEquipement();

        _editButton.Text = "Modifier";
        _editButton.Kind = ButtonKind.Secondary;
        _editButton.Width = Theme.Px(110);
        _editButton.Click += (_, _) => EditSelected();

        _deleteButton.Text = "Supprimer";
        _deleteButton.Kind = ButtonKind.Danger;
        _deleteButton.Width = Theme.Px(120);
        _deleteButton.Click += (_, _) => DeleteSelected();

        _exportButton.Text = "Exporter";
        _exportButton.Kind = ButtonKind.Secondary;
        _exportButton.Width = Theme.Px(110);
        _exportButton.Click += (_, _) => ExportData();
    }

    private Control BuildToolbar()
    {
        return ToolbarBuilder.Build(
            searchBox: _search,
            searchPlaceholder: "Rechercher un équipement…",
            actionButtons: new[] { _addButton, _editButton, _deleteButton, _exportButton });
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Equipement.Nom), HeaderText = "Nom", FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Equipement.Description), HeaderText = "Description", FillWeight = 60 });

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

    private Equipement? SelectedEquipement => _grid.CurrentRow?.DataBoundItem as Equipement;

    private void LoadData(int? selectId = null)
    {
        _all = _service.GetAll();
        ApplyFilter(selectId);
    }

    private void ApplyFilter(int? selectId = null)
    {
        string term = _search.Text.Trim();
        List<Equipement> rows = term.Length == 0
            ? _all
            : _all.Where(eq => Contains(eq.Nom, term) || Contains(eq.Description, term)).ToList();

        _grid.DataSource = rows;

        bool hasRows = rows.Count > 0;
        _grid.Visible = hasRows;
        _emptyLabel.Visible = !hasRows;
        _emptyLabel.Text = _all.Count == 0
            ? "Aucun équipement pour le moment." + Environment.NewLine + "Cliquez sur « Ajouter un équipement » pour créer le premier."
            : "Aucun équipement ne correspond à cette recherche.";

        if (selectId.HasValue && hasRows)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if ((row.DataBoundItem as Equipement)?.Id == selectId.Value)
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
        bool hasSelection = SelectedEquipement is not null;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
    }

    private void AddEquipement()
    {
        using var dialog = new EquipementEditForm(_service, null);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadData(dialog.SavedId);
    }

    private void EditSelected()
    {
        var selected = SelectedEquipement;
        if (selected is null) return;

        using var dialog = new EquipementEditForm(_service, selected);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadData(dialog.SavedId);
    }

    private void DeleteSelected()
    {
        var selected = SelectedEquipement;
        if (selected is null) return;

        string message = $"Voulez-vous vraiment supprimer l'équipement « {selected.Nom} » ?" +
                         Environment.NewLine + "Cette action est définitive.";
        if (!Dialogs.Confirm(message, FindForm())) return;

        try
        {
            _service.Delete(selected.Id);
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
                FileName = $"Equipements_{DateTime.Now:yyyyMMdd}{exporter.FileExtension}"
            };

            if (saveDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                var data = _grid.DataSource as List<Equipement> ?? _all;
                exporter.ExportAsync(data, saveDialog.FileName).GetAwaiter().GetResult();
                Dialogs.Info($"Export réussi :\n{saveDialog.FileName}", this);
            }
            catch (Exception ex)
            {
                AppLog.Error("Export équipements", ex);
                Dialogs.Error($"Erreur lors de l'export : {ex.Message}", this);
            }
        }
    }
