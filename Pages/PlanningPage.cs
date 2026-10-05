using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Pages;

/// <summary>
/// Vue planning en tableau : affiche les réservations par jour et créneau horaire.
/// Format : Date | Salle | Utilisateur | Heure début - fin | Motif
/// Permet de visualiser rapidement les occupations et conflits.
/// </summary>
public sealed class PlanningPage : UserControl
{
    private readonly ReservationService _reservationService;
    private readonly SalleService _salleService;

    private readonly DateTimePicker _dateFilter = new() { Format = DateTimePickerFormat.Short };
    private readonly Button _prevButton = new();
    private readonly Button _nextButton = new();
    private readonly Button _todayButton = new();
    private readonly ComboBox _salleFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid = new();
    private readonly Label _emptyLabel = new();

    private List<Salle> _salles = new();
    private DateTime _currentDate;

    public PlanningPage(ReservationService reservationService, SalleService salleService)
    {
        _reservationService = reservationService;
        _salleService = salleService;
        _currentDate = DateTime.Now.Date;

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
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Theme.Background
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };

        var label1 = new Label
        {
            Text = "Date :",
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Margin = new Padding(0, Theme.Px(9), Theme.Px(10), Theme.Px(9))
        };
        panel.Controls.Add(label1);

        _dateFilter.Font = Theme.Body;
        _dateFilter.Value = _currentDate;
        _dateFilter.ValueChanged += (_, _) =>
        {
            _currentDate = _dateFilter.Value.Date;
            RefreshGrid();
        };
        _dateFilter.Margin = new Padding(0, Theme.Px(9), Theme.Px(10), Theme.Px(9));
        panel.Controls.Add(_dateFilter);

        // Boutons de navigation
        _prevButton.Text = "◀ Jour précédent";
        _prevButton.Font = Theme.BodyBold;
        _prevButton.Size = new Size(Theme.Px(130), Theme.Px(34));
        _prevButton.FlatStyle = FlatStyle.Flat;
        _prevButton.FlatAppearance.BorderSize = 1;
        _prevButton.FlatAppearance.BorderColor = Theme.Border;
        _prevButton.BackColor = Theme.Surface;
        _prevButton.ForeColor = Theme.Navy;
        _prevButton.Cursor = Cursors.Hand;
        _prevButton.Click += (_, _) =>
        {
            _currentDate = _currentDate.AddDays(-1);
            _dateFilter.Value = _currentDate;
        };
        _prevButton.Margin = new Padding(Theme.Px(10), Theme.Px(9), 0, Theme.Px(9));
        panel.Controls.Add(_prevButton);

        _nextButton.Text = "Jour suivant ▶";
        _nextButton.Font = Theme.BodyBold;
        _nextButton.Size = new Size(Theme.Px(130), Theme.Px(34));
        _nextButton.FlatStyle = FlatStyle.Flat;
        _nextButton.FlatAppearance.BorderSize = 1;
        _nextButton.FlatAppearance.BorderColor = Theme.Border;
        _nextButton.BackColor = Theme.Surface;
        _nextButton.ForeColor = Theme.Navy;
        _nextButton.Cursor = Cursors.Hand;
        _nextButton.Click += (_, _) =>
        {
            _currentDate = _currentDate.AddDays(1);
            _dateFilter.Value = _currentDate;
        };
        _nextButton.Margin = new Padding(Theme.Px(6), Theme.Px(9), 0, Theme.Px(9));
        panel.Controls.Add(_nextButton);

        _todayButton.Text = "Aujourd'hui";
        _todayButton.Font = Theme.BodyBold;
        _todayButton.Size = new Size(Theme.Px(90), Theme.Px(34));
        _todayButton.FlatStyle = FlatStyle.Flat;
        _todayButton.FlatAppearance.BorderSize = 1;
        _todayButton.FlatAppearance.BorderColor = Theme.Border;
        _todayButton.BackColor = Theme.Surface;
        _todayButton.ForeColor = Theme.Navy;
        _todayButton.Cursor = Cursors.Hand;
        _todayButton.Click += (_, _) =>
        {
            _currentDate = DateTime.Now.Date;
            _dateFilter.Value = _currentDate;
        };
        _todayButton.Margin = new Padding(Theme.Px(6), Theme.Px(9), 0, Theme.Px(9));
        panel.Controls.Add(_todayButton);

        var label2 = new Label
        {
            Text = "Salle :",
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Margin = new Padding(Theme.Px(20), Theme.Px(9), Theme.Px(10), Theme.Px(9))
        };
        panel.Controls.Add(label2);

        _salleFilter.Font = Theme.Body;
        _salleFilter.Width = Theme.Px(200);
        _salleFilter.Margin = new Padding(0, Theme.Px(9), 0, Theme.Px(9));
        _salleFilter.SelectedValueChanged += (_, _) => RefreshGrid();
        panel.Controls.Add(_salleFilter);

        toolbar.Controls.Add(panel, 0, 0);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.PlageHoraire), HeaderText = "Créneau horaire", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.Motif), HeaderText = "Motif", FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.Statut), HeaderText = "Statut", FillWeight = 12 });

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Font = Theme.Body;
        _emptyLabel.ForeColor = Theme.TextMuted;
        _emptyLabel.Visible = false;

        card.Controls.Add(_grid);
        card.Controls.Add(_emptyLabel);
        return card;
    }

    private void LoadData()
    {
        _salles = _salleService.GetAll();
        _salleFilter.DataSource = _salles;
        _salleFilter.DisplayMember = "Numero";
        _salleFilter.ValueMember = "Id";
        _salleFilter.Items.Insert(0, new ComboBoxItem { Text = "Toutes les salles", Value = 0 });
        _salleFilter.SelectedIndex = 0;
    }

    private void RefreshGrid()
    {
        List<Reservation> reservations = _reservationService.GetByDate(_currentDate);
        
        int selectedSalleId = _salleFilter.SelectedValue is int id ? id : 0;
        if (selectedSalleId > 0)
        {
            reservations = reservations.Where(r => r.SalleId == selectedSalleId).ToList();
        }

        _grid.DataSource = reservations;

        bool hasRows = reservations.Count > 0;
        _grid.Visible = hasRows;
        _emptyLabel.Visible = !hasRows;
        _emptyLabel.Text = $"Aucune réservation confirmée le {_currentDate:dd/MM/yyyy}";
    }

    /// <summary>Simple wrapper pour les items du ComboBox.</summary>
    private class ComboBoxItem
    {
        public string Text { get; set; } = string.Empty;
        public int Value { get; set; }

        public override string ToString() => Text;
    }
}
