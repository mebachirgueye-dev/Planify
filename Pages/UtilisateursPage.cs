using Planify.Controls;
using Planify.Forms;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;
using System.IO;

namespace Planify.Pages;

/// <summary>
/// Liste des comptes utilisateurs avec recherche, ajout, modification et suppression.
/// Réservée aux administrateurs (le menu est filtré par rôle dans MainForm).
/// </summary>
public sealed class UtilisateursPage : UserControl
{
    private readonly UtilisateurService _service;

    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly Label _emptyLabel = new();
    private readonly ThemedButton _addButton = new();
    private readonly ThemedButton _editButton = new();
    private readonly ThemedButton _deleteButton = new();
    private readonly ThemedButton _exportButton = new();

    private List<Utilisateur> _all = new();

    public UtilisateursPage(UtilisateurService service)
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
        _search.PlaceholderText = "Rechercher un utilisateur…";
        _search.Anchor = AnchorStyles.Left;
        _search.TextChanged += (_, _) => ApplyFilter();

        _addButton.Text = "Ajouter un compte";
        _addButton.Kind = ButtonKind.Primary;
        _addButton.Width = Theme.Px(170);
        _addButton.Click += (_, _) => AddUser();

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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Utilisateur.NomComplet), HeaderText = "Nom", FillWeight = 24 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Utilisateur.Email), HeaderText = "Adresse e-mail", FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Utilisateur.Role), HeaderText = "Rôle", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Utilisateur.Statut), HeaderText = "Statut", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Utilisateur.DateCreation), HeaderText = "Créé le", FillWeight = 18 });

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

    private Utilisateur? SelectedUser => _grid.CurrentRow?.DataBoundItem as Utilisateur;

    private void LoadData(int? selectId = null)
    {
        _all = _service.GetAll();
        ApplyFilter(selectId);
    }

    private void ApplyFilter(int? selectId = null)
    {
        string term = _search.Text.Trim();
        List<Utilisateur> rows = term.Length == 0
            ? _all
            : _all.Where(u =>
                Contains(u.NomComplet, term) ||
                Contains(u.Email, term) ||
                u.Role.ToString().Contains(term, StringComparison.CurrentCultureIgnoreCase)).ToList();

        _grid.DataSource = rows;

        bool hasRows = rows.Count > 0;
        _grid.Visible = hasRows;
        _emptyLabel.Visible = !hasRows;
        _emptyLabel.Text = _all.Count == 0
            ? "Aucun compte pour le moment." + Environment.NewLine + "Cliquez sur « Ajouter un compte » pour créer le premier."
            : "Aucun utilisateur ne correspond à cette recherche.";

        if (selectId.HasValue && hasRows)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if ((row.DataBoundItem as Utilisateur)?.Id == selectId.Value)
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
        bool hasSelection = SelectedUser is not null;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
    }

    // ------------------------------------------------------------------ Actions

    private void AddUser()
    {
        using var dialog = new UtilisateurEditForm(_service, null);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadData(dialog.SavedId);
    }

    private void EditSelected()
    {
        var selected = SelectedUser;
        if (selected is null) return;

        using var dialog = new UtilisateurEditForm(_service, selected);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            LoadData(dialog.SavedId);
    }

    private void DeleteSelected()
    {
        var selected = SelectedUser;
        if (selected is null) return;

        string message = $"Voulez-vous vraiment supprimer le compte de {selected.NomComplet} ?" +
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
                FileName = $"Utilisateurs_{DateTime.Now:yyyyMMdd}{exporter.FileExtension}"
            };

            if (saveDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                var data = _grid.DataSource as List<Utilisateur> ?? _all;
                exporter.ExportAsync(data, saveDialog.FileName).GetAwaiter().GetResult();
                Dialogs.Info($"Export réussi :\n{saveDialog.FileName}", this);
            }
            catch (Exception ex)
            {
                AppLog.Error("Export utilisateurs", ex);
                Dialogs.Error($"Erreur lors de l'export : {ex.Message}", this);
            }
        }
    }
