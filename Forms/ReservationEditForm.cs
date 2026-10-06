using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Forms;

/// <summary>Boîte de dialogue de création / modification d'une réservation.</summary>
public sealed class ReservationEditForm : Form
{
    private readonly ReservationService _service;
    private readonly SalleService _salleService;
    private readonly UtilisateurService _utilisateurService;
    private readonly Reservation _reservation;

    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short };
    private readonly TextBox _heureDebut = new() { MaxLength = 5 }; // HH:mm
    private readonly TextBox _heureFin = new() { MaxLength = 5 }; // HH:mm
    private readonly ComboBox _salle = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _utilisateur = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _motif = new() { MaxLength = 200 };

    /// <summary>Identifiant de la réservation enregistrée (valable après DialogResult.OK).</summary>
    public int SavedId { get; private set; }

    public ReservationEditForm(
        ReservationService service,
        SalleService salleService,
        UtilisateurService utilisateurService,
        Reservation? existing)
    {
        _service = service;
        _salleService = salleService;
        _utilisateurService = utilisateurService;

        _reservation = existing is null
            ? new Reservation { Date = DateTime.Now.Date, HeureDebut = new TimeSpan(14, 0, 0), HeureFin = new TimeSpan(16, 0, 0) }
            : new Reservation
            {
                Id = existing.Id,
                SalleId = existing.SalleId,
                UtilisateurId = existing.UtilisateurId,
                Date = existing.Date,
                HeureDebut = existing.HeureDebut,
                HeureFin = existing.HeureFin,
                Motif = existing.Motif,
                Statut = existing.Statut
            };

        Text = existing is null ? "Nouvelle réservation" : "Modifier la réservation";
        Icon = AppResources.LoadIcon("planify.ico");
        Font = Theme.Body;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadData();

        _date.Value = _reservation.Date;
        _heureDebut.Text = $"{_reservation.HeureDebut:HH:mm}";
        _heureFin.Text = $"{_reservation.HeureFin:HH:mm}";
        if (_salle.Items.Count > 0)
            _salle.SelectedValue = _reservation.SalleId;
        if (_utilisateur.Items.Count > 0)
            _utilisateur.SelectedValue = _reservation.UtilisateurId;
        _motif.Text = _reservation.Motif;
    }

    private void LoadData()
    {
        // Charger les salles disponibles
        var salles = _salleService.GetAll();
        _salle.DataSource = salles;
        _salle.DisplayMember = nameof(Salle.Numero);
        _salle.ValueMember = nameof(Salle.Id);
        _salle.DropDownWidth = Theme.Px(400);

        // Charger les utilisateurs
        _utilisateur.DataSource = _utilisateurService.GetAll();
        _utilisateur.DisplayMember = nameof(Utilisateur.NomComplet);
        _utilisateur.ValueMember = nameof(Utilisateur.Id);
        _utilisateur.DropDownWidth = Theme.Px(400);
    }

    private void BuildLayout()
    {
        int margin = Theme.Px(24);
        int width = Theme.Px(460);
        int fieldWidth = width - 2 * margin;
        int y = margin;

        AddField("Date *", _date, Theme.Px(160), margin, ref y);
        AddField("Heure de début (HH:mm) *", _heureDebut, Theme.Px(100), margin, ref y);
        AddField("Heure de fin (HH:mm) *", _heureFin, Theme.Px(100), margin, ref y);
        AddField("Salle *", _salle, fieldWidth, margin, ref y);
        AddField("Utilisateur *", _utilisateur, fieldWidth, margin, ref y);
        AddField("Motif *", _motif, fieldWidth, margin, ref y);

        var cancel = new ThemedButton
        {
            Text = "Annuler",
            Kind = ButtonKind.Secondary,
            Width = Theme.Px(110),
            DialogResult = DialogResult.Cancel
        };
        var save = new ThemedButton
        {
            Text = "Enregistrer",
            Kind = ButtonKind.Primary,
            Width = Theme.Px(130)
        };
        save.Click += OnSave;

        save.Location = new Point(width - margin - save.Width, y);
        cancel.Location = new Point(save.Left - Theme.Px(10) - cancel.Width, y);
        Controls.Add(cancel);
        Controls.Add(save);

        AcceptButton = save;
        CancelButton = cancel;

        ClientSize = new Size(width, y + save.Height + margin);
    }

    private void AddField(string label, Control input, int inputWidth, int x, ref int y)
    {
        var caption = new Label
        {
            Text = label,
            Font = Theme.SmallBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Location = new Point(x, y)
        };
        Controls.Add(caption);
        y += Theme.Px(22);

        input.Location = new Point(x, y);
        input.Width = inputWidth;
        Controls.Add(input);
        y += input.Height + Theme.Px(16);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _reservation.Date = _date.Value.Date;

        // Parser les heures
        if (!TimeSpan.TryParse(_heureDebut.Text, out var hd))
        {
            Dialogs.Warning("Format d'heure invalide pour l'heure de début. Utilisez HH:mm.", this);
            return;
        }
        if (!TimeSpan.TryParse(_heureFin.Text, out var hf))
        {
            Dialogs.Warning("Format d'heure invalide pour l'heure de fin. Utilisez HH:mm.", this);
            return;
        }

        _reservation.HeureDebut = hd;
        _reservation.HeureFin = hf;
        _reservation.SalleId = (int)(_salle.SelectedValue ?? 0);
        _reservation.UtilisateurId = (int)(_utilisateur.SelectedValue ?? 0);
        _reservation.Motif = _motif.Text;

        try
        {
            _service.Save(_reservation);
            SavedId = _reservation.Id;
            DialogResult = DialogResult.OK;
        }
        catch (BusinessRuleException ex)
        {
            Dialogs.Warning(ex.Message, this);
            _date.Focus();
        }
    }
}
