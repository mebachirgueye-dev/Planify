using Planify.Controls;
using Planify.Helpers;

namespace Planify.Pages;

/// <summary>Écran provisoire pour les modules qui seront développés dans les phases suivantes.</summary>
public sealed class PlaceholderPage : UserControl
{
    public PlaceholderPage(string moduleName)
    {
        BackColor = Theme.Background;
        Padding = new Padding(Theme.Px(28), Theme.Px(8), Theme.Px(28), Theme.Px(28));

        var card = new CardPanel { Dock = DockStyle.Fill };
        var message = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            Text = $"Le module « {moduleName} » sera ajouté dans une prochaine phase du développement."
        };

        card.Controls.Add(message);
        Controls.Add(card);
    }
}
