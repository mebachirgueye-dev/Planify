using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Forms;

/// <summary>
/// Boîte de dialogue de création / modification d'un compte utilisateur.
/// Pour un nouveau compte, le mot de passe est obligatoire ; pour un compte existant,
/// laisser les champs vides conserve le mot de passe actuel.
/// </summary>
public sealed class UtilisateurEditForm : Form
{
    private readonly UtilisateurService _service;
    private readonly Utilisateur _user;
    private readonly bool _isNew;

    private readonly TextBox _nom = new() { MaxLength = 100 };
    private readonly TextBox _prenom = new() { MaxLength = 100 };
    private readonly TextBox _email = new();
    private readonly ComboBox _role = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _statut = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _password = new();
    private readonly TextBox _passwordConfirm = new();
    private readonly Label _passwordHint = new();
    private readonly Label _errorLabel = new();

    /// <summary>Identifiant du compte enregistré (valable après DialogResult.OK).</summary>
    public int SavedId { get; private set; }

    public UtilisateurEditForm(UtilisateurService service, Utilisateur? existing)
    {
        _service = service;
        _isNew = existing is null;

        _user = existing is null
            ? new Utilisateur()
            : new Utilisateur
            {
                Id = existing.Id,
                Nom = existing.Nom,
                Prenom = existing.Prenom,
                Email = existing.Email,
                Role = existing.Role,
                Statut = existing.Statut
            };

        Text = _isNew ? "Nouveau compte" : "Modifier le compte";
        Icon = AppResources.LoadIcon("planify.ico");
        Font = Theme.Body;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        _role.Items.AddRange(new object[]
        {
            "Administrateur", "Gestionnaire", "Utilisateur"
        });
        _statut.Items.AddRange(new object[]
        {
            "Actif", "Désactivé"
        });
        _role.DropDownWidth = Theme.Px(200);
        _statut.DropDownWidth = Theme.Px(150);

        BuildLayout();

        _nom.Text = _user.Nom;
        _prenom.Text = _user.Prenom;
        _email.Text = _user.Email;
        _role.SelectedIndex = (int)_user.Role;
        _statut.SelectedIndex = (int)_user.Statut;

        _passwordHint.Text = _isNew
            ? $"Au moins {UtilisateurService.MinPasswordLength} caractères."
            : "Laisser vide pour conserver le mot de passe actuel.";
    }

    private void BuildLayout()
    {
        int margin = Theme.Px(24);
        int width = Theme.Px(480);
        int fieldWidth = width - 2 * margin;
        int y = margin;

        _nom.Width = fieldWidth;
        _nom.Font = Theme.Body;
        _nom.BorderStyle = BorderStyle.FixedSingle;
        _nom.PlaceholderText = "Dupont";
        AddField("Nom *", _nom, fieldWidth, margin, ref y);

        _prenom.Width = fieldWidth;
        _prenom.Font = Theme.Body;
        _prenom.BorderStyle = BorderStyle.FixedSingle;
        _prenom.PlaceholderText = "Jean";
        AddField("Prénom *", _prenom, fieldWidth, margin, ref y);

        _email.Width = fieldWidth;
        _email.Font = Theme.Body;
        _email.BorderStyle = BorderStyle.FixedSingle;
        _email.PlaceholderText = "jean.dupont@exemple.fr";
        AddField("Adresse e-mail *", _email, fieldWidth, margin, ref y);

        _role.Width = fieldWidth;
        _role.Font = Theme.Body;
        AddField("Rôle", _role, fieldWidth, margin, ref y);

        _statut.Width = fieldWidth;
        _statut.Font = Theme.Body;
        AddField("Statut", _statut, fieldWidth, margin, ref y);

        _password.Width = fieldWidth;
        _password.Font = Theme.Body;
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.UseSystemPasswordChar = true;
        _password.PlaceholderText = _isNew ? "Mot de passe" : "Nouveau mot de passe (optionnel)";
        AddField(_isNew ? "Mot de passe *" : "Nouveau mot de passe", _password, fieldWidth, margin, ref y);

        _passwordConfirm.Width = fieldWidth;
        _passwordConfirm.Font = Theme.Body;
        _passwordConfirm.BorderStyle = BorderStyle.FixedSingle;
        _passwordConfirm.UseSystemPasswordChar = true;
        _passwordConfirm.PlaceholderText = "Confirmer le mot de passe";
        AddField("Confirmer le mot de passe", _passwordConfirm, fieldWidth, margin, ref y);

        _passwordHint.AutoSize = true;
        _passwordHint.Font = Theme.Small;
        _passwordHint.ForeColor = Theme.TextMuted;
        _passwordHint.Location = new Point(margin, y);
        y += Theme.Px(18);

        _errorLabel.AutoSize = true;
        _errorLabel.Font = Theme.Small;
        _errorLabel.ForeColor = Theme.Danger;
        _errorLabel.Location = new Point(margin, y);
        _errorLabel.Visible = false;
        y += Theme.Px(20);

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
        y += input.Height + Theme.Px(14);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _errorLabel.Visible = false;

        string password = _password.Text;
        string confirm = _passwordConfirm.Text;
        if (password != confirm)
        {
            _errorLabel.Text = "Les deux mots de passe ne correspondent pas.";
            _errorLabel.Visible = true;
            _password.Focus();
            return;
        }

        _user.Nom = _nom.Text;
        _user.Prenom = _prenom.Text;
        _user.Email = _email.Text;
        _user.Role = (Role)_role.SelectedIndex;
        _user.Statut = (StatutUtilisateur)_statut.SelectedIndex;

        try
        {
            _service.Save(_user, password.Length > 0 ? password : null);
            SavedId = _user.Id;
            DialogResult = DialogResult.OK;
        }
        catch (BusinessRuleException ex)
        {
            _errorLabel.Text = ex.Message;
            _errorLabel.Visible = true;
        }
    }
}
