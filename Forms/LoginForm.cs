using Planify.Controls;
using Planify.Helpers;
using Planify.Services;

namespace Planify.Forms;

/// <summary>
/// Écran de connexion : e-mail + mot de passe.
/// Le logo sert d'identité visuelle. La validation est déléguée à <see cref="AuthService"/>.
/// </summary>
public sealed class LoginForm : Form
{
    private readonly AuthService _auth;

    private readonly TextBox _email = new();
    private readonly TextBox _password = new();
    private readonly Label _errorLabel = new();

    public LoginForm(AuthService auth)
    {
        _auth = auth;

        Text = "Connexion à Planify";
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
        int width = Theme.Px(400);
        int margin = Theme.Px(36);
        int fieldWidth = width - 2 * margin;

        // En-tête : logo + titre
        var logo = AppResources.LoadImage("logo.png", Theme.Px(56));
        var logoBox = new PictureBox
        {
            Image = logo,
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point((width - logo.Width) / 2, Theme.Px(36)),
            BackColor = Theme.Surface
        };

        var title = new Label
        {
            Text = "Connexion",
            Font = Theme.Title,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Location = new Point((width - Theme.Px(150)) / 2, logoBox.Bottom + Theme.Px(14))
        };

        var subtitle = new Label
        {
            Text = "Saisissez vos identifiants pour accéder à Planify.",
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Location = new Point((width - Theme.Px(280)) / 2, title.Bottom + Theme.Px(6))
        };

        int y = subtitle.Bottom + Theme.Px(28);

        _email.Width = fieldWidth;
        _email.Font = Theme.Body;
        _email.BorderStyle = BorderStyle.FixedSingle;
        _email.PlaceholderText = "Adresse e-mail";
        AddField("Adresse e-mail", _email, fieldWidth, margin, ref y);

        _password.Width = fieldWidth;
        _password.Font = Theme.Body;
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.UseSystemPasswordChar = true;
        _password.PlaceholderText = "Mot de passe";
        AddField("Mot de passe", _password, fieldWidth, margin, ref y);

        _errorLabel.AutoSize = true;
        _errorLabel.Font = Theme.Small;
        _errorLabel.ForeColor = Theme.Danger;
        _errorLabel.Location = new Point(margin, y);
        _errorLabel.Visible = false;
        y += Theme.Px(20);

        var connect = new ThemedButton
        {
            Text = "Se connecter",
            Kind = ButtonKind.Primary,
            Width = fieldWidth,
            Location = new Point(margin, y)
        };
        connect.Click += OnConnect;
        y += connect.Height + Theme.Px(28);

        Controls.Add(logoBox);
        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(_email);
        Controls.Add(_password);
        Controls.Add(_errorLabel);
        Controls.Add(connect);

        AcceptButton = connect;
        CancelButton = new ThemedButton { DialogResult = DialogResult.Cancel };

        ClientSize = new Size(width, y);
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

    private void OnConnect(object? sender, EventArgs e)
    {
        _errorLabel.Visible = false;
        try
        {
            _auth.Login(_email.Text, _password.Text);
            DialogResult = DialogResult.OK;
        }
        catch (BusinessRuleException ex)
        {
            _errorLabel.Text = ex.Message;
            _errorLabel.Visible = true;
            _password.Focus();
        }
    }
}
