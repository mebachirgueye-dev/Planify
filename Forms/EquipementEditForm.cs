using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Forms;

/// <summary>Boîte de dialogue de création / modification d'un équipement.</summary>
public sealed class EquipementEditForm : Form
{
    private readonly EquipementService _service;
    private readonly Equipement _equipement;

    private readonly TextBox _nom = new() { MaxLength = 100 };
    private readonly TextBox _description = new() { MaxLength = 500, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical };

    /// <summary>Identifiant de l'équipement enregistré (valable après DialogResult.OK).</summary>
    public int SavedId { get; private set; }

    public EquipementEditForm(EquipementService service, Equipement? existing)
    {
        _service = service;

        _equipement = existing is null
            ? new Equipement()
            : new Equipement
            {
                Id = existing.Id,
                Nom = existing.Nom,
                Description = existing.Description
            };

        Text = existing is null ? "Nouvel équipement" : "Modifier l'équipement";
        Icon = AppResources.LoadIcon("planify.ico");
        Font = Theme.Body;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();

        _nom.Text = _equipement.Nom;
        _description.Text = _equipement.Description;
    }

    private void BuildLayout()
    {
        int margin = Theme.Px(24);
        int width = Theme.Px(460);
        int fieldWidth = width - 2 * margin;
        int y = margin;

        AddField("Nom de l'équipement *", _nom, fieldWidth, margin, ref y);

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
        _equipement.Nom = _nom.Text;
        _equipement.Description = _description.Text;

        try
        {
            _service.Save(_equipement);
            SavedId = _equipement.Id;
            DialogResult = DialogResult.OK;
        }
        catch (BusinessRuleException ex)
        {
            Dialogs.Warning(ex.Message, this);
            _nom.Focus();
        }
    }
}
