using Planify.Controls;
using Planify.Helpers;
using Planify.Models;
using Planify.Services;
using System.Drawing.Drawing2D;

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
        int cardWidth = Theme.Px(460);
        int margin = Theme.Spacing(24);
        int fieldWidth = cardWidth - 2 * margin;

        // Estimer la hauteur nécessaire
        int estimatedHeight = Theme.Px(700);

        ClientSize = new Size(cardWidth + Theme.Spacing(48), estimatedHeight + Theme.Spacing(48));

        // Carte centrale
        var card = new Panel
        {
            Size = new Size(cardWidth, estimatedHeight),
            Location = new Point(Theme.Spacing(24), Theme.Spacing(24)),
            BackColor = Theme.Surface,
            AutoScroll = true
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
        var logo = AppResources.LoadImage("logo.png", Theme.Px(56));
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
            Text = "Création du compte administrateur",
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
            Text = "Aucun compte n'existe encore. Créez le premier administrateur pour commencer.",
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            AutoSize = true
        };
        subtitle.Location = new Point((cardWidth - Theme.MeasureString(subtitle.Text, subtitle.Font).Width) / 2, y);
        card.Controls.Add(subtitle);
        y += subtitle.Height + Theme.Spacing(28);

// Champs
        _nom.Width = fieldWidth;
        _nom.Font = Theme.Body;
        _nom.BorderStyle = BorderStyle.FixedSingle;
        _nom.PlaceholderText = "Dupont";
        AddField(card, "Nom *", _nom, margin, ref y);

        _prenom.Width = fieldWidth;
        _prenom.Font = Theme.Body;
        _prenom.BorderStyle = BorderStyle.FixedSingle;
        _prenom.PlaceholderText = "Jean";
        AddField(card, "Prénom *", _prenom, margin, ref y);

        _email.Width = fieldWidth;
        _email.Font = Theme.Body;
        _email.BorderStyle = BorderStyle.FixedSingle;
        _email.PlaceholderText = "jean.dupont@exemple.fr";
        AddField(card, "Adresse e-mail *", _email, margin, ref y);

        _password.Width = fieldWidth;
        _password.Font = Theme.Body;
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.UseSystemPasswordChar = true;
        _password.PlaceholderText = $"Au moins {UtilisateurService.MinPasswordLength} caractères";
        AddField(card, "Mot de passe *", _password, margin, ref y);

        _passwordConfirm.Width = fieldWidth;
        _passwordConfirm.Font = Theme.Body;
        _passwordConfirm.BorderStyle = BorderStyle.FixedSingle;
        _passwordConfirm.UseSystemPasswordChar = true;
        _passwordConfirm.PlaceholderText = "Retapez le mot de passe";
        AddField(card, "Confirmer le mot de passe *", _passwordConfirm, margin, ref y);

        _errorLabel.AutoSize = true;
        _errorLabel.Font = Theme.Small;
        _errorLabel.ForeColor = Theme.Danger;
        _errorLabel.Location = new Point(margin, y);
        _errorLabel.Visible = false;
        _errorLabel.MaximumSize = new Size(fieldWidth, 0);
        card.Controls.Add(_errorLabel);
        y += Theme.Spacing(24);

        var create = new ThemedButton
        {
            Text = "Créer le compte et continuer",
            Kind = ButtonKind.Primary,
            Width = fieldWidth,
            Height = Theme.Px(44),
            Location = new Point(margin, y)
        };
        create.Click += OnCreate;
        card.Controls.Add(create);
        y += create.Height + Theme.Spacing(32);

        // Ajustement hauteur carte
        card.Height = y + Theme.Spacing(32);
        ClientSize = new Size(cardWidth + Theme.Spacing(48), card.Height + Theme.Spacing(48));
        card.Location = new Point(Theme.Spacing(24), Theme.Spacing(24));

        Controls.Add(card);

        AcceptButton = create;
        CancelButton = new ThemedButton { DialogResult = DialogResult.Cancel };
    }

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
