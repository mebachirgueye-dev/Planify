using Planify.Controls;
using Planify.Forms;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;
using System.IO;

namespace Planify.Pages;

/// <summary>
/// Liste des bâtiments avec recherche, ajout, modification et suppression.
/// C'est le modèle à suivre pour les prochaines pages de gestion (salles, utilisateurs...).
/// </summary>
public sealed class BatimentsPage : UserControl
{
    private readonly BatimentService _service;

    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly Label _emptyLabel = new();
    private readonly ThemedButton _addButton = new();
    private readonly ThemedButton _editButton = new();
    private readonly ThemedButton _deleteButton = new();
    private readonly ThemedButton _exportButton = new();

    private List<Batiment> _all = new();

    public BatimentsPage(BatimentService service)
    {
        _service = service;

        BackColor = Theme.Background;
        Padding = new Padding(Theme.Px(28), Theme.Px(8), Theme.Px(28), Theme.Px(28));

        var card = BuildGridCard();
        var toolbar = BuildToolbar();

        // Le contrôle "Fill" est ajouté en premier, la barre d'outils (Top) ensuite.
        Controls.Add(card);
        Controls.Add(toolbar);
        card.BringToFront();

        LoadData();
    }

    // ------------------------------------------------------------------ Construction de l'interface

    private Control BuildToolbar()
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Theme.Px(56),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _search.Width = Theme.Px(320);
        _search.Font = Theme.Body;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.PlaceholderText = "Rechercher un bâtiment…";
        _search.Anchor = AnchorStyles.Left;
        _search.TextChanged += (_, _) => ApplyFilter();

        _addButton.Text = "Ajouter un bâtiment";
        _addButton.Kind = ButtonKind.Primary;
        _addButton.Width = Theme.Px(180);
        _addButton.Click += (_, _) => AddBatiment();

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

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        foreach (var button in new[] { _addButton, _editButton, _deleteButton, _exportButton })
        {
            button.Margin = new Padding(Theme.Px(10), Theme.Px(9), 0, Theme.Px(9));
            buttons.Controls.Add(button);
        }

        toolbar.Controls.Add(_search, 0, 0);
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Batiment.Nom), HeaderText = "Nom", FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Batiment.Adresse), HeaderText = "Adresse", FillWeight = 33 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Batiment.NombreEtages), HeaderText = "Étages", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Batiment.Description), HeaderText = "Description", FillWeight = 32 });

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

    // ------------------------------------------------------------------ Données

    private Batiment? SelectedBatiment => _grid.CurrentRow?.DataBoundItem as Batiment;

    private void LoadData(int? selectId = null)
    {
        _all = _service.GetAll();
        ApplyFilter(selectId);
    }

    private void ApplyFilter(int? selectId = null)
    {
        string term = _search.Text.Trim();
        List<Batiment> rows = term.Length == 0
            ? _all
            : _all.Where(b => Contains(b.Nom, term) || Contains(b.Adresse, term) || Contains(b.Description, term)).ToList();

        _grid.DataSource = rows;

        bool hasRows = rows.Count > 0;
        _grid.Visible = hasRows;
        _emptyLabel.Visible = !hasRows;
        _emptyLabel.Text = _all.Count == 0
            ? "Aucun bâtiment pour le moment." + Environment.NewLine + "Cliquez sur « Ajouter un bâtiment » pour créer le premier."
            : "Aucun bâtiment ne correspond à cette recherche.";

        if (selectId.HasValue && hasRows)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if ((row.DataBoundItem as Batiment)?.Id == selectId.Value)
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
        bool hasSelection = SelectedBatiment is not null;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
    }

    // ------------------------------------------------------------------ Actions

    private void AddBatiment()
    {
        using var dialog = new BatimentEditForm(_service, null);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadData(dialog.SavedId);
    }

    private void EditSelected()
    {
        var selected = SelectedBatiment;
        if (selected is null) return;

        using var dialog = new BatimentEditForm(_service, selected);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadData(dialog.SavedId);
    }

    private void DeleteSelected()
    {
        var selected = SelectedBatiment;
        if (selected is null) return;

        string message = $"Voulez-vous vraiment supprimer le bâtiment « {selected.Nom} » ?" +
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
                FileName = $"Batiments_{DateTime.Now:yyyyMMdd}{exporter.FileExtension}"
            };

            if (saveDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                var data = _grid.DataSource as List<Batiment> ?? _all;
                exporter.ExportAsync(data, saveDialog.FileName).GetAwaiter().GetResult();
                Dialogs.Info($"Export réussi :\n{saveDialog.FileName}", this);
            }
            catch (Exception ex)
            {
                AppLog.Error("Export bâtiments", ex);
                Dialogs.Error($"Erreur lors de l'export : {ex.Message}", this);
            }
        }
    }
