using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;

namespace Planify.Forms;

/// <summary>
/// Premier lancement de Planify : aucun compte n'existe encore.
/// Cette fenêtre crée le premier compte administrateur, indispensable pour la suite.
/// </summary>
public sealed class FirstRunForm : Form
{
    private readonly UtilisateurService _service;

    private readonly TextBox _nom = new() { MaxLength = 100 };
    private readonly TextBox _prenom = new() { MaxLength = 100 };
    private readonly TextBox _email = new();
    private readonly TextBox _password = new();
    private readonly TextBox _passwordConfirm = new();
    private readonly Label _errorLabel = new();

    public FirstRunForm(UtilisateurService service)
    {
        _service = service;

        Text = "Premier lancement de Planify";
        Icon = AppResources.LoadIcon("planify.ico");
        Font = Theme.Body;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
    }

    private void BuildLayout()
    {
        int width = Theme.Px(440);
        int margin = Theme.Px(36);
        int fieldWidth = width - 2 * margin;

        var logo = AppResources.LoadImage("logo.png", Theme.Px(48));
        var logoBox = new PictureBox
        {
            Image = logo,
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point((width - logo.Width) / 2, Theme.Px(32)),
            BackColor = Theme.Surface
        };

        var title = new Label
        {
            Text = "Création du compte administrateur",
            Font = Theme.Title,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Location = new Point((width - Theme.Px(360)) / 2, logoBox.Bottom + Theme.Px(12))
        };

        var subtitle = new Label
        {
            Text = "Aucun compte n'existe encore. Créez le premier administrateur pour commencer.",
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Location = new Point((width - Theme.Px(380)) / 2, title.Bottom + Theme.Px(6))
        };

        int y = subtitle.Bottom + Theme.Px(24);

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

        _password.Width = fieldWidth;
        _password.Font = Theme.Body;
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.UseSystemPasswordChar = true;
        _password.PlaceholderText = $"Au moins {UtilisateurService.MinPasswordLength} caractères";
        AddField("Mot de passe *", _password, fieldWidth, margin, ref y);

        _passwordConfirm.Width = fieldWidth;
        _passwordConfirm.Font = Theme.Body;
        _passwordConfirm.BorderStyle = BorderStyle.FixedSingle;
        _passwordConfirm.UseSystemPasswordChar = true;
        _passwordConfirm.PlaceholderText = "Retapez le mot de passe";
        AddField("Confirmer le mot de passe *", _passwordConfirm, fieldWidth, margin, ref y);

        _errorLabel.AutoSize = true;
        _errorLabel.Font = Theme.Small;
        _errorLabel.ForeColor = Theme.Danger;
        _errorLabel.Location = new Point(margin, y);
        _errorLabel.Visible = false;
        y += Theme.Px(20);

        var create = new ThemedButton
        {
            Text = "Créer le compte et continuer",
            Kind = ButtonKind.Primary,
            Width = fieldWidth,
            Location = new Point(margin, y)
        };
        create.Click += OnCreate;
        y += create.Height + Theme.Px(28);

        Controls.Add(logoBox);
        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(_nom);
        Controls.Add(_prenom);
        Controls.Add(_email);
        Controls.Add(_password);
        Controls.Add(_passwordConfirm);
        Controls.Add(_errorLabel);
        Controls.Add(create);

        AcceptButton = create;
        CancelButton = new ThemedButton { DialogResult = DialogResult.Cancel };

        ClientSize = new Size(width, y);
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

    private void OnCreate(object? sender, EventArgs e)
    {
        _errorLabel.Visible = false;

        if (_password.Text != _passwordConfirm.Text)
        {
            ShowError("Les deux mots de passe ne correspondent pas.");
            return;
        }

        var user = new Utilisateur
        {
            Nom = _nom.Text,
            Prenom = _prenom.Text,
            Email = _email.Text,
            Role = Role.Administrateur,
            Statut = StatutUtilisateur.Actif
        };

        try
        {
            _service.Save(user, _password.Text);
            DialogResult = DialogResult.OK;
        }
        catch (BusinessRuleException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
    }
}
