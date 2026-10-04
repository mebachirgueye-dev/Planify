using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Forms;

/// <summary>Boîte de dialogue de création / modification d'un bâtiment.</summary>
public sealed class BatimentEditForm : Form
{
    private readonly BatimentService _service;
    private readonly Batiment _batiment;

    private readonly TextBox _nom = new() { MaxLength = 100 };
    private readonly TextBox _adresse = new() { MaxLength = 200 };
    private readonly NumericUpDown _etages = new() { Minimum = 1, Maximum = BatimentService.MaxEtages };
    private readonly TextBox _description = new() { MaxLength = 500, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical };

    /// <summary>Identifiant du bâtiment enregistré (valable après DialogResult.OK).</summary>
    public int SavedId { get; private set; }

    public BatimentEditForm(BatimentService service, Batiment? existing)
    {
        _service = service;

        // On travaille sur une copie : la liste affichée n'est pas modifiée tant que l'enregistrement n'a pas réussi.
        _batiment = existing is null
            ? new Batiment()
            : new Batiment
            {
                Id = existing.Id,
                Nom = existing.Nom,
                Adresse = existing.Adresse,
                NombreEtages = existing.NombreEtages,
                Description = existing.Description
            };

        Text = existing is null ? "Nouveau bâtiment" : "Modifier le bâtiment";
        Icon = AppResources.LoadIcon("planify.ico");
        Font = Theme.Body;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();

        _nom.Text = _batiment.Nom;
        _adresse.Text = _batiment.Adresse;
        _etages.Value = Math.Clamp(_batiment.NombreEtages, 1, BatimentService.MaxEtages);
        _description.Text = _batiment.Description;
    }

    private void BuildLayout()
    {
        int margin = Theme.Px(24);
        int width = Theme.Px(460);
        int fieldWidth = width - 2 * margin;
        int y = margin;

        AddField("Nom du bâtiment *", _nom, fieldWidth, margin, ref y);
        AddField("Adresse / localisation", _adresse, fieldWidth, margin, ref y);

        _etages.Width = Theme.Px(120);
        AddField("Nombre d'étages", _etages, _etages.Width, margin, ref y);

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

    /// <summary>Place un libellé suivi de son champ de saisie, puis descend le curseur vertical.</summary>
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
        _batiment.Nom = _nom.Text;
        _batiment.Adresse = _adresse.Text;
        _batiment.NombreEtages = (int)_etages.Value;
        _batiment.Description = _description.Text;

        try
        {
            _service.Save(_batiment);
            SavedId = _batiment.Id;
            DialogResult = DialogResult.OK;
        }
        catch (BusinessRuleException ex)
        {
            Dialogs.Warning(ex.Message, this);
            _nom.Focus();
        }
    }
}
