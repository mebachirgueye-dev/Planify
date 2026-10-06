using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;
using System.Data;

namespace Planify.Pages;

/// <summary>
/// Vue planning : Jour / Semaine / Mois.
/// - Jour : liste des réservations du jour (existant)
/// - Semaine : grille 7 jours × créneaux horaires
/// - Mois : calendrier mensuel avec indicateurs d'occupation
/// </summary>
public sealed class PlanningPage : UserControl
{
    private readonly ReservationService _reservationService;
    private readonly SalleService _salleService;
    private readonly CoursEvenementService _coursService;

    private readonly ComboBox _viewMode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _dateFilter = new() { Format = DateTimePickerFormat.Short };
    private readonly Button _prevButton = new();
    private readonly Button _nextButton = new();
    private readonly Button _todayButton = new();
    private readonly ComboBox _salleFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid = new();
    private readonly Label _emptyLabel = new();

    private List<Salle> _salles = new();
    private DateTime _currentDate;
    private ViewMode _currentView = ViewMode.Jour;

    public PlanningPage(ReservationService reservationService, SalleService salleService, CoursEvenementService coursService)
    {
        _reservationService = reservationService;
        _salleService = salleService;
        _coursService = coursService;
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

    private enum ViewMode
    {
        Jour = 0,
        Semaine = 1,
        Mois = 2
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
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };

        // Mode de vue
        var viewLabel = new Label
        {
            Text = "Vue :",
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Margin = new Padding(0, Theme.Px(9), Theme.Px(8), Theme.Px(9))
        };
        panel.Controls.Add(viewLabel);

        _viewMode.Font = Theme.Body;
        _viewMode.Width = Theme.Px(130);
        _viewMode.Items.AddRange(new object[] { "Jour", "Semaine", "Mois" });
        _viewMode.SelectedIndex = 0;
        _viewMode.SelectedValueChanged += (_, _) =>
        {
            _currentView = (ViewMode)_viewMode.SelectedIndex;
            RefreshGrid();
        };
        _viewMode.Margin = new Padding(0, Theme.Px(9), Theme.Px(10), Theme.Px(9));
        panel.Controls.Add(_viewMode);

        // Date / période
        var dateLabel = new Label
        {
            Text = "Date :",
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Margin = new Padding(Theme.Px(20), Theme.Px(9), Theme.Px(10), Theme.Px(9))
        };
        panel.Controls.Add(dateLabel);

        _dateFilter.Font = Theme.Body;
        _dateFilter.Value = _currentDate;
        _dateFilter.ValueChanged += (_, _) =>
        {
            _currentDate = _dateFilter.Value.Date;
            RefreshGrid();
        };
        _dateFilter.Margin = new Padding(0, Theme.Px(9), Theme.Px(10), Theme.Px(9));
        panel.Controls.Add(_dateFilter);

        // Boutons de navigation (adaptés au mode)
        _prevButton.Font = Theme.BodyBold;
        _prevButton.Size = new Size(Theme.Px(130), Theme.Px(34));
        _prevButton.FlatStyle = FlatStyle.Flat;
        _prevButton.FlatAppearance.BorderSize = 1;
        _prevButton.FlatAppearance.BorderColor = Theme.Border;
        _prevButton.BackColor = Theme.Surface;
        _prevButton.ForeColor = Theme.Navy;
        _prevButton.Cursor = Cursors.Hand;
        _prevButton.Click += (_, _) => NavigatePrevious();
        _prevButton.Margin = new Padding(Theme.Px(10), Theme.Px(9), 0, Theme.Px(9));
        panel.Controls.Add(_prevButton);

        _nextButton.Font = Theme.BodyBold;
        _nextButton.Size = new Size(Theme.Px(130), Theme.Px(34));
        _nextButton.FlatStyle = FlatStyle.Flat;
        _nextButton.FlatAppearance.BorderSize = 1;
        _nextButton.FlatAppearance.BorderColor = Theme.Border;
        _nextButton.BackColor = Theme.Surface;
        _nextButton.ForeColor = Theme.Navy;
        _nextButton.Cursor = Cursors.Hand;
        _nextButton.Click += (_, _) => NavigateNext();
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

        // Filtre salle
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

        // Créer une liste avec l'option "Toutes les salles" en premier
        var items = new List<ComboBoxItem>
        {
            new ComboBoxItem { Text = "Toutes les salles", Value = 0 }
        };
        items.AddRange(_salles.Select(s => new ComboBoxItem { Text = s.Numero, Value = s.Id }));

        _salleFilter.DataSource = null;
        _salleFilter.Items.Clear();
        _salleFilter.Items.AddRange(items.Cast<object>().ToArray());
        _salleFilter.DisplayMember = "Text";
        _salleFilter.ValueMember = "Value";
        _salleFilter.SelectedIndex = 0;
        _salleFilter.DropDownWidth = Theme.Px(400);

        RefreshGrid();
    }

    private void NavigatePrevious()
    {
        _currentDate = _currentView switch
        {
            ViewMode.Jour => _currentDate.AddDays(-1),
            ViewMode.Semaine => _currentDate.AddDays(-7),
            ViewMode.Mois => _currentDate.AddMonths(-1),
            _ => _currentDate.AddDays(-1)
        };
        _dateFilter.Value = _currentDate;
    }

    private void NavigateNext()
    {
        _currentDate = _currentView switch
        {
            ViewMode.Jour => _currentDate.AddDays(1),
            ViewMode.Semaine => _currentDate.AddDays(7),
            ViewMode.Mois => _currentDate.AddMonths(1),
            _ => _currentDate.AddDays(1)
        };
        _dateFilter.Value = _currentDate;
    }

    private void RefreshGrid()
    {
        try
        {
            // Mettre à jour les libellés des boutons selon le mode
            UpdateNavigationButtons();

            // Reconfigurer la grille selon le mode
            ConfigureGridForView();

            // Charger les données
            var data = GetDataForView();
            _grid.DataSource = data;

            bool hasRows;
            if (data is DataTable dt)
                hasRows = dt.Rows.Count > 0;
            else if (data is System.Collections.ICollection coll)
                hasRows = coll.Count > 0;
            else
                hasRows = false;

            _grid.Visible = hasRows;
            _emptyLabel.Visible = !hasRows;
            _emptyLabel.Text = GetEmptyMessage();
        }
        catch (Exception ex)
        {
            AppLog.Error("Actualisation planning", ex);
            Dialogs.Error($"Erreur lors de l'actualisation du planning :{Environment.NewLine}{ex.Message}", this);
            _emptyLabel.Visible = true;
            _emptyLabel.Text = "Erreur lors du chargement des données.";
            _grid.Visible = false;
        }
    }

    private void UpdateNavigationButtons()
    {
        _prevButton.Text = _currentView switch
        {
            ViewMode.Jour => "◀ Jour précédent",
            ViewMode.Semaine => "◀ Semaine précédente",
            ViewMode.Mois => "◀ Mois précédent",
            _ => "◀ Précédent"
        };
        _nextButton.Text = _currentView switch
        {
            ViewMode.Jour => "Jour suivant ▶",
            ViewMode.Semaine => "Semaine suivante ▶",
            ViewMode.Mois => "Mois suivant ▶",
            _ => "Suivant ▶"
        };
    }

    private void ConfigureGridForView()
    {
        _grid.Columns.Clear();

        switch (_currentView)
        {
            case ViewMode.Jour:
                ConfigureDayView();
                break;
            case ViewMode.Semaine:
                ConfigureWeekView();
                break;
            case ViewMode.Mois:
                ConfigureMonthView();
                break;
        }
    }

    private void ConfigureDayView()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Salle.Numero", HeaderText = "Salle", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Utilisateur.NomComplet", HeaderText = "Utilisateur", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.PlageHoraire), HeaderText = "Créneau horaire", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.Motif), HeaderText = "Motif", FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Reservation.Statut), HeaderText = "Statut", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Type", HeaderText = "Type", FillWeight = 13 });
    }

    private void ConfigureWeekView()
    {
        // Colonnes : Salle + 7 jours (Lun-Dim)
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Salle", HeaderText = "Salle", FillWeight = 12, Frozen = true });
        var days = new[] { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
        foreach (var day in days)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = day, HeaderText = day, FillWeight = 12 });
        }
    }

    private void ConfigureMonthView()
    {
        // Vue mois : une ligne par salle, colonnes = jours du mois (1-31)
        // Pour simplifier, on affiche un résumé par semaine
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Salle", HeaderText = "Salle", FillWeight = 12, Frozen = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Semaine1", HeaderText = "Sem 1", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Semaine2", HeaderText = "Sem 2", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Semaine3", HeaderText = "Sem 3", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Semaine4", HeaderText = "Sem 4", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Semaine5", HeaderText = "Sem 5", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "Total", FillWeight = 10 });
    }

    private object GetDataForView()
    {
        int selectedSalleId = _salleFilter.SelectedValue is int id ? id : 0;

        return _currentView switch
        {
            ViewMode.Jour => GetDayData(selectedSalleId),
            ViewMode.Semaine => GetWeekData(selectedSalleId),
            ViewMode.Mois => GetMonthData(selectedSalleId),
            _ => new List<object>()
        };
    }

    private object GetDayData(int salleId)
    {
        var reservations = _reservationService.GetByDate(_currentDate);
        var cours = _coursService.GetByDate(_currentDate);

        if (salleId > 0)
        {
            reservations = reservations.Where(r => r.SalleId == salleId).ToList();
            cours = cours.Where(c => c.SalleId == salleId).ToList();
        }

        var result = new List<object>();

        foreach (var r in reservations)
        {
            result.Add(new
            {
                Salle = r.Salle?.Numero ?? "",
                Utilisateur = r.Utilisateur?.NomComplet ?? "",
                r.PlageHoraire,
                r.Motif,
                r.Statut,
                Type = "Réservation"
            });
        }

        foreach (var c in cours)
        {
            result.Add(new
            {
                Salle = c.Salle?.Numero ?? "",
                Utilisateur = c.Responsable?.NomComplet ?? "",
                PlageHoraire = c.PlageHoraire,
                Motif = c.Nom,
                Statut = c.Statut,
                Type = "Cours/Événement"
            });
        }

        // Trier par heure de début
        result = result.OrderBy(x =>
        {
            var ph = x.GetType().GetProperty("PlageHoraire")?.GetValue(x)?.ToString() ?? "";
            var parts = ph.Split('-');
            return TimeSpan.TryParse(parts[0].Trim(), out var t) ? t : TimeSpan.Zero;
        }).ToList();

        return result.Cast<object>().ToList();
    }

    private DataTable GetWeekData(int salleId)
    {
        // Trouver le lundi de la semaine
        var startOfWeek = _currentDate.AddDays(-(int)_currentDate.DayOfWeek + (int)DayOfWeek.Monday);
        if (_currentDate.DayOfWeek == DayOfWeek.Sunday)
            startOfWeek = _currentDate.AddDays(-6);

        var reservations = _reservationService.GetAll()
            .Where(r => r.Date >= startOfWeek && r.Date < startOfWeek.AddDays(7) && r.Statut == StatutReservation.Confirmee)
            .ToList();
        var cours = _coursService.GetAll()
            .Where(c => c.Date >= startOfWeek && c.Date < startOfWeek.AddDays(7) && c.Statut != StatutEvenement.Annule)
            .ToList();

        if (salleId > 0)
        {
            reservations = reservations.Where(r => r.SalleId == salleId).ToList();
            cours = cours.Where(c => c.SalleId == salleId).ToList();
        }

        var targetSalles = salleId > 0 ? _salles.Where(s => s.Id == salleId).ToList() : _salles;

        var dt = new DataTable();
        dt.Columns.Add("Salle", typeof(string));
        var days = new[] { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
        foreach (var day in days)
            dt.Columns.Add(day, typeof(string));

        foreach (var salle in targetSalles)
        {
            var row = dt.NewRow();
            row["Salle"] = salle.Numero;

            foreach (var day in days)
            {
                var dayDate = startOfWeek.AddDays(Array.IndexOf(days, day));
                var dayReservations = reservations.Where(r => r.SalleId == salle.Id && r.Date == dayDate).ToList();
                var dayCours = cours.Where(c => c.SalleId == salle.Id && c.Date == dayDate).ToList();

                var items = new List<string>();
                foreach (var r in dayReservations)
                    items.Add($"{r.PlageHoraire} {r.Motif} ({r.Utilisateur?.NomComplet})");
                foreach (var c in dayCours)
                    items.Add($"{c.PlageHoraire} {c.Nom} ({c.Responsable?.NomComplet})");

                row[day] = items.Count > 0 ? string.Join("; ", items) : "—";
            }
            dt.Rows.Add(row);
        }

        return dt;
    }

    private DataTable GetMonthData(int salleId)
    {
        var startOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        var weeksInMonth = (int)Math.Ceiling((endOfMonth.Day + (int)startOfMonth.DayOfWeek) / 7.0);
        weeksInMonth = Math.Max(weeksInMonth, 4); // au moins 4 semaines

        var reservations = _reservationService.GetAll()
            .Where(r => r.Date >= startOfMonth && r.Date <= endOfMonth && r.Statut == StatutReservation.Confirmee)
            .ToList();
        var cours = _coursService.GetAll()
            .Where(c => c.Date >= startOfMonth && c.Date <= endOfMonth && c.Statut != StatutEvenement.Annule)
            .ToList();

        if (salleId > 0)
        {
            reservations = reservations.Where(r => r.SalleId == salleId).ToList();
            cours = cours.Where(c => c.SalleId == salleId).ToList();
        }

        var targetSalles = salleId > 0 ? _salles.Where(s => s.Id == salleId).ToList() : _salles;

        var dt = new DataTable();
        dt.Columns.Add("Salle", typeof(string));
        for (int w = 1; w <= 5; w++)
            dt.Columns.Add($"Semaine{w}", typeof(string));
        dt.Columns.Add("Total", typeof(string));

        foreach (var salle in targetSalles)
        {
            var row = dt.NewRow();
            row["Salle"] = salle.Numero;
            int total = 0;

            for (int w = 1; w <= 5; w++)
            {
                var weekStart = startOfMonth.AddDays((w - 1) * 7);
                var weekEnd = weekStart.AddDays(6);
                if (weekStart > endOfMonth) { row[$"Semaine{w}"] = "—"; continue; }
                if (weekEnd > endOfMonth) weekEnd = endOfMonth;

                var weekReservations = reservations.Where(r => r.SalleId == salle.Id && r.Date >= weekStart && r.Date <= weekEnd).Count();
                var weekCours = cours.Where(c => c.SalleId == salle.Id && c.Date >= weekStart && c.Date <= weekEnd).Count();
                int weekTotal = weekReservations + weekCours;
                total += weekTotal;

                row[$"Semaine{w}"] = weekTotal > 0 ? $"{weekTotal} créneaux" : "—";
            }
            row["Total"] = total > 0 ? $"{total} créneaux" : "—";
            dt.Rows.Add(row);
        }

        return dt;
    }

    private string GetEmptyMessage()
    {
        return _currentView switch
        {
            ViewMode.Jour => $"Aucune réservation confirmée le {_currentDate:dd/MM/yyyy}",
            ViewMode.Semaine => $"Aucune réservation cette semaine (du {GetWeekStart():dd/MM} au {GetWeekStart().AddDays(6):dd/MM})",
            ViewMode.Mois => $"Aucune réservation en {_currentDate:MMMM yyyy}",
            _ => "Aucune donnée"
        };
    }

    private DateTime GetWeekStart()
    {
        var start = _currentDate.AddDays(-(int)_currentDate.DayOfWeek + (int)DayOfWeek.Monday);
        if (_currentDate.DayOfWeek == DayOfWeek.Sunday)
            start = _currentDate.AddDays(-6);
        return start;
    }

    private sealed class ComboBoxItem
    {
        public string Text { get; set; } = string.Empty;
        public int Value { get; set; }
        public override string ToString() => Text;
    }
}