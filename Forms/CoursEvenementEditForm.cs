using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Forms;

/// <summary>Boîte de dialogue de création / modification d'un cours ou événement.</summary>
public sealed class CoursEvenementEditForm : Form
{
    private readonly CoursEvenementService _service;
    private readonly SalleService _salleService;
    private readonly UtilisateurService _utilisateurService;
    private readonly CoursEvenement _evenement;

    private readonly TextBox _nom = new() { MaxLength = 120 };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short };
    private readonly TextBox _heureDebut = new() { MaxLength = 5 };
    private readonly TextBox _heureFin = new() { MaxLength = 5 };
    private readonly ComboBox _salleCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _responsableCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _statutCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _description = new() { MaxLength = 500, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical };

    public int SavedId { get; private set; }

    public CoursEvenementEditForm(CoursEvenementService service, SalleService salleService, UtilisateurService utilisateurService, CoursEvenement? existing)
    {
        _service = service;
        _salleService = salleService;
        _utilisateurService = utilisateurService;

        _evenement = existing is null ? new CoursEvenement() : new CoursEvenement
        {
            Id = existing.Id,
            Nom = existing.Nom,
            Description = existing.Description,
            Date = existing.Date,
            HeureDebut = existing.HeureDebut,
            HeureFin = existing.HeureFin,
            SalleId = existing.SalleId,
            ResponsableId = existing.ResponsableId,
            Statut = existing.Statut
        };

        Text = existing is null ? "Nouveau cours / événement" : "Modifier le cours / événement";
        Icon = AppResources.LoadIcon("planify.ico");
        Font = Theme.Body;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        LoadLookupData();
        BuildLayout();

        _nom.Text = _evenement.Nom;
        _date.Value = _evenement.Date == default ? DateTime.Today : _evenement.Date;
        _heureDebut.Text = _evenement.HeureDebut == default ? "09:00" : _evenement.HeureDebut.ToString(@"hh\:mm");
        _heureFin.Text = _evenement.HeureFin == default ? "10:30" : _evenement.HeureFin.ToString(@"hh\:mm");
        _description.Text = _evenement.Description ?? string.Empty;
        _statutCombo.SelectedItem = _evenement.Statut;

        SetSelectedValue(_salleCombo, _evenement.SalleId);
        SetSelectedValue(_responsableCombo, _evenement.ResponsableId);
    }

    private void LoadLookupData()
    {
        foreach (var salle in _salleService.GetAll())
            _salleCombo.Items.Add(new ComboBoxItem(salle.Id, salle.Numero + " - " + (salle.Batiment?.Nom ?? "Bâtiment")));

        foreach (var utilisateur in _utilisateurService.GetAll())
            _responsableCombo.Items.Add(new ComboBoxItem(utilisateur.Id, utilisateur.NomComplet));

        foreach (var value in Enum.GetValues<StatutEvenement>())
            _statutCombo.Items.Add(value);

        if (_salleCombo.Items.Count > 0)
            _salleCombo.SelectedIndex = 0;
        if (_responsableCombo.Items.Count > 0)
            _responsableCombo.SelectedIndex = 0;
        if (_statutCombo.Items.Count > 0)
            _statutCombo.SelectedIndex = 0;
    }

    private void BuildLayout()
    {
        int margin = Theme.Px(24);
        int width = Theme.Px(520);
        int fieldWidth = width - 2 * margin;
        int y = margin;

        AddField("Nom du cours / événement *", _nom, fieldWidth, margin, ref y);
        AddField("Date *", _date, fieldWidth, margin, ref y);

        var hoursPanel = new FlowLayoutPanel { Width = fieldWidth, Height = Theme.Px(52), FlowDirection = FlowDirection.LeftToRight, AutoSize = false };
        var beginLabel = new Label { Text = "Heure début", Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(0, 0, Theme.Px(8), 0) };
        var endLabel = new Label { Text = "Heure fin", Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Margin = new Padding(Theme.Px(18), 0, Theme.Px(8), 0) };
        _heureDebut.Width = Theme.Px(100);
        _heureFin.Width = Theme.Px(100);
        hoursPanel.Controls.Add(beginLabel);
        hoursPanel.Controls.Add(_heureDebut);
        hoursPanel.Controls.Add(endLabel);
        hoursPanel.Controls.Add(_heureFin);
        hoursPanel.Location = new Point(margin, y);
        Controls.Add(hoursPanel);
        y += Theme.Px(60);

        AddField("Salle *", _salleCombo, fieldWidth, margin, ref y);
        AddField("Responsable *", _responsableCombo, fieldWidth, margin, ref y);
        AddField("Statut", _statutCombo, fieldWidth, margin, ref y);
        _description.Height = Theme.Px(90);
        AddField("Description", _description, fieldWidth, margin, ref y);

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
        var caption = new Label { Text = label, Font = Theme.SmallBold, ForeColor = Theme.Navy, AutoSize = true, Location = new Point(x, y) };
        Controls.Add(caption);
        y += Theme.Px(22);

        input.Location = new Point(x, y);
        input.Width = inputWidth;
        Controls.Add(input);
        y += input.Height + Theme.Px(16);
    }

    private void SetSelectedValue(ComboBox combo, int value)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item && item.Id == value)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _evenement.Nom = _nom.Text;
        _evenement.Date = _date.Value.Date;
        _evenement.Description = _description.Text.Trim();

        if (!TimeSpan.TryParse(_heureDebut.Text, out var heureDebut))
        {
            Dialogs.Warning("L'heure de début est invalide. Utilisez le format HH:mm.", this);
            _heureDebut.Focus();
            return;
        }

        if (!TimeSpan.TryParse(_heureFin.Text, out var heureFin))
        {
            Dialogs.Warning("L'heure de fin est invalide. Utilisez le format HH:mm.", this);
            _heureFin.Focus();
            return;
        }

        _evenement.HeureDebut = heureDebut;
        _evenement.HeureFin = heureFin;

        if (_salleCombo.SelectedItem is not ComboBoxItem salleSelection)
        {
            Dialogs.Warning("Sélectionnez une salle pour ce cours ou événement.", this);
            _salleCombo.Focus();
            return;
        }

        if (_responsableCombo.SelectedItem is not ComboBoxItem responsableSelection)
        {
            Dialogs.Warning("Sélectionnez un responsable pour ce cours ou événement.", this);
            _responsableCombo.Focus();
            return;
        }

        _evenement.SalleId = salleSelection.Id;
        _evenement.ResponsableId = responsableSelection.Id;
        _evenement.Statut = _statutCombo.SelectedItem is StatutEvenement statut ? statut : StatutEvenement.Planifie;

        try
        {
            _service.Save(_evenement);
            SavedId = _evenement.Id;
            DialogResult = DialogResult.OK;
        }
        catch (BusinessRuleException ex)
        {
            Dialogs.Warning(ex.Message, this);
        }
    }

    private sealed class ComboBoxItem
    {
        public ComboBoxItem(int id, string text)
        {
            Id = id;
            Text = text;
        }

        public int Id { get; }
        public string Text { get; }

        public override string ToString() => Text;
    }
}
