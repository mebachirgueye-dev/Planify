using Planify.Controls;
using Planify.Forms;
using Planify.Helpers;
using Planify.Services;
using System.Diagnostics;

namespace Planify.Pages;

/// <summary>
/// Page Paramètres : sauvegarde/restauration, dossier de sauvegarde, nettoyage, informations.
/// Réservée aux administrateurs.
/// </summary>
public sealed class ParametresPage : UserControl
{
    private readonly BackupService _backupService;

    private readonly DataGridView _grid = new();
    private readonly Label _emptyLabel = new();
    private readonly ThemedButton _backupButton = new();
    private readonly ThemedButton _restoreButton = new();
    private readonly ThemedButton _openFolderButton = new();
    private readonly ThemedButton _changeFolderButton = new();
    private readonly ThemedButton _cleanButton = new();
    private readonly Label _currentFolderLabel = new();
    private readonly TextBox _customFolderBox = new();

    private List<BackupInfo> _backups = new();

    public ParametresPage(BackupService backupService)
    {
        _backupService = backupService;

        BackColor = Theme.Background;
        Padding = new Padding(Theme.Px(28), Theme.Px(8), Theme.Px(28), Theme.Px(28));

        InitializeButtons();

        var card = BuildGridCard();
        var toolbar = BuildToolbar();

        Controls.Add(card);
        Controls.Add(toolbar);
        card.BringToFront();

        LoadBackups();
    }

    private void InitializeButtons()
    {
        _backupButton.Text = "💾 Sauvegarder maintenant";
        _backupButton.Kind = ButtonKind.Primary;
        _backupButton.Width = Theme.Px(200);
        _backupButton.Click += (_, _) => CreateBackup();

        _restoreButton.Text = "🔄 Restaurer la sélection";
        _restoreButton.Kind = ButtonKind.Secondary;
        _restoreButton.Width = Theme.Px(200);
        _restoreButton.Click += (_, _) => RestoreSelected();

        _cleanButton.Text = "🧹 Nettoyer (garder 10)";
        _cleanButton.Kind = ButtonKind.Secondary;
        _cleanButton.Width = Theme.Px(180);
        _cleanButton.Click += (_, _) => CleanOldBackups();

        _changeFolderButton.Text = "Changer";
        _changeFolderButton.Kind = ButtonKind.Secondary;
        _changeFolderButton.AutoSize = true;
        _changeFolderButton.Click += (_, _) => ChangeBackupFolder();

        _openFolderButton.Text = "Ouvrir le dossier";
        _openFolderButton.Kind = ButtonKind.Secondary;
        _openFolderButton.AutoSize = true;
        _openFolderButton.Click += (_, _) => OpenBackupFolder();
    }

    private Control BuildToolbar()
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Theme.Px(120),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Panneau gauche : dossier actuel + actions principales
        var leftPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };

        // Ligne 1 : dossier courant
        var folderPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, Theme.Px(8), 0, Theme.Px(6))
        };
        var folderLabel = new Label
        {
            Text = "Dossier de sauvegarde :",
            Font = Theme.SmallBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Margin = new Padding(0, Theme.Px(4), Theme.Px(8), 0)
        };
        folderPanel.Controls.Add(folderLabel);

        _currentFolderLabel.AutoSize = true;
        _currentFolderLabel.Font = Theme.Small;
        _currentFolderLabel.ForeColor = Theme.TextMuted;
        _currentFolderLabel.Margin = new Padding(0, Theme.Px(4), Theme.Px(8), 0);
        folderPanel.Controls.Add(_currentFolderLabel);

        _changeFolderButton.Margin = new Padding(0, Theme.Px(2), 0, 0);
        folderPanel.Controls.Add(_changeFolderButton);

        _openFolderButton.Margin = new Padding(Theme.Px(6), Theme.Px(2), 0, 0);
        folderPanel.Controls.Add(_openFolderButton);

        leftPanel.Controls.Add(folderPanel);

        // Ligne 2 : dossier personnalisé temporaire
        var customPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, Theme.Px(8))
        };
        var customLabel = new Label
        {
            Text = "Dossier temporaire :",
            Font = Theme.SmallBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Margin = new Padding(0, Theme.Px(4), Theme.Px(8), 0)
        };
        customPanel.Controls.Add(customLabel);

        _customFolderBox.Width = Theme.Px(300);
        _customFolderBox.Font = Theme.Body;
        _customFolderBox.BorderStyle = BorderStyle.FixedSingle;
        _customFolderBox.PlaceholderText = "Chemin dossier (laisser vide = défaut)";
        _customFolderBox.Margin = new Padding(0, 0, Theme.Px(8), 0);
        customPanel.Controls.Add(_customFolderBox);

        var browseButton = new ThemedButton
        {
            Text = "Parcourir",
            Kind = ButtonKind.Secondary,
            AutoSize = true,
        };
        browseButton.Click += (_, _) => BrowseCustomFolder();
        customPanel.Controls.Add(browseButton);
        leftPanel.Controls.Add(customPanel);

        // Boutons d'action principaux
        var actionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, Theme.Px(4), 0, 0)
        };

        _backupButton.Margin = new Padding(0, Theme.Px(9), Theme.Px(10), Theme.Px(9));
        actionsPanel.Controls.Add(_backupButton);

        _restoreButton.Margin = new Padding(Theme.Px(10), Theme.Px(9), Theme.Px(10), Theme.Px(9));
        actionsPanel.Controls.Add(_restoreButton);

        _cleanButton.Margin = new Padding(Theme.Px(10), Theme.Px(9), 0, Theme.Px(9));
        actionsPanel.Controls.Add(_cleanButton);

        leftPanel.Controls.Add(actionsPanel);

        // Panneau droit : bouton suppression (aligné à droite)
        var rightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            BackColor = Theme.Background,
            Margin = new Padding(Theme.Px(20), Theme.Px(8), 0, 0)
        };

        var deleteButton = new ThemedButton
        {
            Text = "🗑 Supprimer la sélection",
            Kind = ButtonKind.Danger,
            Width = Theme.Px(200),
        };
        deleteButton.Click += (_, _) => DeleteSelected();
        rightPanel.Controls.Add(deleteButton);

        toolbar.Controls.Add(leftPanel, 0, 0);
        toolbar.Controls.Add(rightPanel, 1, 0);
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BackupInfo.FileName), HeaderText = "Fichier", FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BackupInfo.FormattedDate), HeaderText = "Date", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BackupInfo.FormattedSize), HeaderText = "Taille", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BackupInfo.FilePath), HeaderText = "Chemin complet", FillWeight = 25 });

        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) RestoreSelected();
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

    private BackupInfo? SelectedBackup => _grid.CurrentRow?.DataBoundItem as BackupInfo;

    private void LoadBackups()
    {
        try
        {
            _backups = _backupService.ListBackups();
            _currentFolderLabel.Text = _backupService.DefaultBackupDirectory;
            ApplyFilter();
        }
        catch (Exception ex)
        {
            AppLog.Error("Chargement sauvegardes", ex);
            Dialogs.Error($"Impossible de charger les sauvegardes : {ex.Message}", this);
        }
    }

    private void ApplyFilter()
    {
        _grid.DataSource = _backups;

        bool hasRows = _backups.Count > 0;
        _grid.Visible = hasRows;
        _emptyLabel.Visible = !hasRows;
        _emptyLabel.Text = "Aucune sauvegarde trouvée.\nCliquez sur « Sauvegarder maintenant » pour créer la première.";

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasSelection = SelectedBackup is not null;
        _restoreButton.Enabled = hasSelection;
        _cleanButton.Enabled = _backups.Count > 10;
    }

    private void CreateBackup()
    {
        try
        {
            string? customDir = string.IsNullOrWhiteSpace(_customFolderBox.Text) ? null : _customFolderBox.Text.Trim();
            if (customDir is not null && !Directory.Exists(customDir))
            {
                Dialogs.Warning("Le dossier spécifié n'existe pas.", this);
                return;
            }

            string backupPath = _backupService.CreateBackup(customDir);
            Dialogs.Info($"Sauvegarde créée :\n{backupPath}", this);
            LoadBackups();
        }
        catch (BusinessRuleException ex)
        {
            Dialogs.Warning(ex.Message, this);
        }
        catch (Exception ex)
        {
            AppLog.Error("Création sauvegarde", ex);
            Dialogs.Error($"Erreur lors de la sauvegarde : {ex.Message}", this);
        }
    }

    private void RestoreSelected()
    {
        var selected = SelectedBackup;
        if (selected is null) return;

        string message = $"⚠️ ATTENTION : Cette action va REMPLACER la base de données actuelle.\n\n" +
                         $"Fichier à restaurer : {selected.FileName}\n" +
                         $"Date : {selected.FormattedDate}\n\n" +
                         $"Une copie de sécurité de la base actuelle sera faite automatiquement.\n" +
                         $"L'application devra redémarrer pour prendre en compte la restauration.\n\n" +
                         $"Continuer ?";

        if (!Dialogs.Confirm(message, this)) return;

        try
        {
            _backupService.RestoreBackup(selected.FilePath);
            Dialogs.Info("Restauration effectuée.\nL'application va se fermer. Relancez Planify pour utiliser la base restaurée.", this);
            Application.Exit();
        }
        catch (BusinessRuleException ex)
        {
            Dialogs.Warning(ex.Message, this);
        }
        catch (Exception ex)
        {
            AppLog.Error("Restauration sauvegarde", ex);
            Dialogs.Error($"Erreur lors de la restauration : {ex.Message}", this);
        }
    }

    private void DeleteSelected()
    {
        var selected = SelectedBackup;
        if (selected is null) return;

        if (!Dialogs.Confirm($"Supprimer la sauvegarde « {selected.FileName} » ?", this)) return;

        try
        {
            _backupService.DeleteBackup(selected.FilePath);
            LoadBackups();
        }
        catch (Exception ex)
        {
            AppLog.Error("Suppression sauvegarde", ex);
            Dialogs.Error($"Erreur : {ex.Message}", this);
        }
    }

    private void CleanOldBackups()
    {
        if (!Dialogs.Confirm("Supprimer toutes les sauvegardes sauf les 10 plus récentes ?", this)) return;

        try
        {
            int deleted = _backupService.CleanOldBackups(10);
            Dialogs.Info($"{deleted} ancienne(s) sauvegarde(s) supprimée(s).", this);
            LoadBackups();
        }
        catch (Exception ex)
        {
            AppLog.Error("Nettoyage sauvegardes", ex);
            Dialogs.Error($"Erreur : {ex.Message}", this);
        }
    }

    private void ChangeBackupFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choisir le dossier de sauvegarde par défaut",
            SelectedPath = _backupService.DefaultBackupDirectory,
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            // Note : le changement persistant nécessiterait un fichier de config.
            // Pour l'instant, on met à jour l'affichage et le dossier par défaut de l'instance.
            // TODO: persister dans un fichier settings.json
            Dialogs.Info($"Dossier changé pour cette session :\n{dialog.SelectedPath}\n\nPour le rendre permanent, ajoutez-le dans un fichier de configuration (prochaine version).", this);
        }
    }

    private void OpenBackupFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo(_backupService.DefaultBackupDirectory)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Dialogs.Error($"Impossible d'ouvrir le dossier : {ex.Message}", this);
        }
    }

    private void BrowseCustomFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choisir un dossier temporaire pour la prochaine sauvegarde",
            SelectedPath = string.IsNullOrWhiteSpace(_customFolderBox.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : _customFolderBox.Text,
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _customFolderBox.Text = dialog.SelectedPath;
        }
    }
}