using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Forms;

/// <summary>Boîte de dialogue de création / modification d'une salle.</summary>
public sealed class SalleEditForm : Form
{
    private readonly SalleService _service;
    private readonly BatimentService _batimentService;
    private readonly EquipementService _equipementService;
    private readonly Salle _salle;

    private readonly ComboBox _batiment = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _numero = new() { MaxLength = 50 };
    private readonly NumericUpDown _capacite = new() { Minimum = 1, Maximum = SalleService.MaxCapacite };
    private readonly TextBox _type = new() { MaxLength = 50 };
    private readonly TextBox _description = new() { MaxLength = 500, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical };
    private readonly CheckedListBox _equipements = new();
    private readonly ComboBox _statut = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    /// <summary>Identifiant de la salle enregistrée (valable après DialogResult.OK).</summary>
    public int SavedId { get; private set; }

    public SalleEditForm(SalleService service, BatimentService batimentService, EquipementService equipementService, Salle? existing)
    {
        _service = service;
        _batimentService = batimentService;
        _equipementService = equipementService;

        _salle = existing is null
            ? new Salle()
            : new Salle
            {
                Id = existing.Id,
                Numero = existing.Numero,
                BatimentId = existing.BatimentId,
                Capacite = existing.Capacite,
                Type = existing.Type,
                Description = existing.Description,
                Statut = existing.Statut
            };

        Text = existing is null ? "Nouvelle salle" : "Modifier la salle";
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

        _numero.Text = _salle.Numero;
        _batiment.SelectedValue = _salle.BatimentId;
        _capacite.Value = Math.Clamp(_salle.Capacite, 1, SalleService.MaxCapacite);
        _type.Text = _salle.Type;
        _description.Text = _salle.Description;
        _statut.SelectedItem = _salle.Statut;

        // Cocher les équipements de la salle existante
        if (existing?.Equipements is not null)
        {
            var existingIds = new HashSet<int>(existing.Equipements.Select(eq => eq.Id));
            for (int i = 0; i < _equipements.Items.Count; i++)
            {
                var item = _equipements.Items[i] as Equipement;
                if (item is not null && existingIds.Contains(item.Id))
                    _equipements.SetItemChecked(i, true);
            }
        }
    }

    private void LoadData()
    {
        // Charger les bâtiments
        _batiment.DataSource = _batimentService.GetAll();
        _batiment.DisplayMember = nameof(Batiment.Nom);
        _batiment.ValueMember = nameof(Batiment.Id);

        // Charger les équipements
        _equipements.DataSource = _equipementService.GetAll();
        _equipements.DisplayMember = nameof(Equipement.Nom);

        // Charger les statuts
        _statut.DataSource = Enum.GetValues(typeof(StatutSalle));
    }

    private void BuildLayout()
    {
        int margin = Theme.Px(24);
        int width = Theme.Px(520);
        int fieldWidth = width - 2 * margin;
        int y = margin;

        AddField("Bâtiment *", _batiment, fieldWidth, margin, ref y);
        AddField("Numéro / Nom de la salle *", _numero, fieldWidth, margin, ref y);
        AddField("Capacité (places) *", _capacite, Theme.Px(140), margin, ref y);
        AddField("Type (ex. : Salle, Amphi, Labo)", _type, fieldWidth, margin, ref y);

        _description.Height = Theme.Px(70);
        AddField("Description", _description, fieldWidth, margin, ref y);

        _equipements.Height = Theme.Px(100);
        AddField("Équipements disponibles", _equipements, fieldWidth, margin, ref y);

        AddField("Statut", _statut, Theme.Px(160), margin, ref y);

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
        _salle.Numero = _numero.Text;
        _salle.BatimentId = (int)(_batiment.SelectedValue ?? 0);
        _salle.Capacite = (int)_capacite.Value;
        _salle.Type = _type.Text;
        _salle.Description = _description.Text;
        _salle.Statut = (StatutSalle)(_statut.SelectedItem ?? StatutSalle.Disponible);

        // Récupérer les équipements cochés
        var equipementIds = new List<int>();
        for (int i = 0; i < _equipements.Items.Count; i++)
        {
            if (_equipements.GetItemChecked(i) && _equipements.Items[i] is Equipement eq)
                equipementIds.Add(eq.Id);
        }

        try
        {
            _service.Save(_salle, equipementIds);
            SavedId = _salle.Id;
            DialogResult = DialogResult.OK;
        }
        catch (BusinessRuleException ex)
        {
            Dialogs.Warning(ex.Message, this);
            _numero.Focus();
        }
    }
}
