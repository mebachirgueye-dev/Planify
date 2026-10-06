using Planify.Controls;
using Planify.Helpers;
using Planify.Services;
using System.Drawing.Drawing2D;

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
        BackColor = Theme.Background;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildLayout();
    }

    private void BuildLayout()
    {
        int cardWidth = Theme.Px(420);
        int cardHeight = Theme.Px(520);
        int margin = Theme.Spacing(24);
        int fieldWidth = cardWidth - 2 * margin;

        // Centrer la carte dans la fenêtre
        ClientSize = new Size(cardWidth + Theme.Spacing(48), cardHeight + Theme.Spacing(48));

        // Carte centrale
        var card = new Panel
        {
            Size = new Size(cardWidth, cardHeight),
            Location = new Point(Theme.Spacing(24), Theme.Spacing(24)),
            BackColor = Theme.Surface
        };
        card.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
            var radius = Theme.Radius(Theme.RadiusLarge);

            // Ombre
            using (var shadowPath = GraphicsHelper.RoundedRectangle(
                new Rectangle(Theme.Radius(4), Theme.Radius(4), card.Width - Theme.Radius(8), card.Height - Theme.Radius(8)), radius))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, Theme.ShadowColor)))
                g.FillPath(shadowBrush, shadowPath);

            // Carte
            using (var path = GraphicsHelper.RoundedRectangle(rect, radius))
            using (var fill = new SolidBrush(Theme.Surface))
            using (var pen = new Pen(Theme.BorderLight))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
        };

        // Contenu de la carte
        int y = Theme.Spacing(32);

        // Logo
        var logo = AppResources.LoadImage("logo.png", Theme.Px(64));
        var logoBox = new PictureBox
        {
            Image = logo,
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point((cardWidth - logo.Width) / 2, y),
            BackColor = Color.Transparent
        };
        card.Controls.Add(logoBox);
        y += logo.Height + Theme.Spacing(16);

        // Titre
        var title = new Label
        {
            Text = "Connexion",
            Font = Theme.TitleLarge,
            ForeColor = Theme.Navy,
            AutoSize = true
        };
        title.Location = new Point((cardWidth - Theme.MeasureString(title.Text, title.Font).Width) / 2, y);
        card.Controls.Add(title);
        y += title.Height + Theme.Spacing(4);

        // Sous-titre
        var subtitle = new Label
        {
            Text = "Saisissez vos identifiants pour accéder à Planify.",
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            AutoSize = true
        };
        subtitle.Location = new Point((cardWidth - Theme.MeasureString(subtitle.Text, subtitle.Font).Width) / 2, y);
        card.Controls.Add(subtitle);
        y += subtitle.Height + Theme.Spacing(28);

        // Champ Email
        _email.Width = fieldWidth;
        _email.Font = Theme.Body;
        _email.BorderStyle = BorderStyle.FixedSingle;
        _email.PlaceholderText = "Adresse e-mail";
        AddField(card, "Adresse e-mail", _email, margin, ref y);

        // Champ Mot de passe
        _password.Width = fieldWidth;
        _password.Font = Theme.Body;
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.UseSystemPasswordChar = true;
        _password.PlaceholderText = "Mot de passe";
        AddField(card, "Mot de passe", _password, margin, ref y);

        // Label erreur
        _errorLabel.AutoSize = true;
        _errorLabel.Font = Theme.Small;
        _errorLabel.ForeColor = Theme.Danger;
        _errorLabel.Location = new Point(margin, y);
        _errorLabel.Visible = false;
        _errorLabel.MaximumSize = new Size(fieldWidth, 0);
        card.Controls.Add(_errorLabel);
        y += Theme.Spacing(24);

        // Bouton connexion
        var connect = new ThemedButton
        {
            Text = "Se connecter",
            Kind = ButtonKind.Primary,
            Width = fieldWidth,
            Height = Theme.Px(44),
            Location = new Point(margin, y)
        };
        connect.Click += OnConnect;
        card.Controls.Add(connect);
        y += connect.Height + Theme.Spacing(16);

        // Lien mot de passe oublié (placeholder)
        var forgotLink = new LinkLabel
        {
            Text = "Mot de passe oublié ?",
            Font = Theme.Small,
            LinkColor = Theme.Primary,
            ActiveLinkColor = Theme.PrimaryHover,
            VisitedLinkColor = Theme.Primary,
            AutoSize = true,
            Location = new Point((cardWidth - Theme.MeasureString("Mot de passe oublié ?", Theme.Small).Width) / 2, y)
        };
        card.Controls.Add(forgotLink);

        Controls.Add(card);

        AcceptButton = connect;
        CancelButton = new ThemedButton { DialogResult = DialogResult.Cancel };
    }

    /// <summary>Place un libellé suivi de son champ de saisie, puis descend le curseur vertical.</summary>
    private void AddField(Control parent, string label, Control input, int x, ref int y)
    {
        var caption = new Label
        {
            Text = label,
            Font = Theme.SmallBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Location = new Point(x, y)
        };
        parent.Controls.Add(caption);
        y += Theme.Px(22);

        input.Location = new Point(x, y);
        input.Width = input.Width;
        parent.Controls.Add(input);
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
