using Planify.Controls;
using Planify.Helpers;
using System.Windows.Forms;

namespace Planify.Controls;

/// <summary>
/// Constructeur de barre d'outils standardisée pour les pages Planify.
/// Gère la recherche, les boutons d'action et le responsive.
/// </summary>
public static class ToolbarBuilder
{
    /// <summary>Crée une barre d'outils avec zone de recherche à gauche et boutons à droite.</summary>
    /// <param name="searchBox">TextBox de recherche (peut être null).</param>
    /// <param name="searchPlaceholder">Texte indicatif si searchBox est fourni.</param>
    /// <param name="actionButtons">Boutons d'action principaux (Ajouter, Modifier, Supprimer, Exporter...).</param>
    /// <param name="extraControls">Contrôles supplémentaires (filtres, combo box...).</param>
    public static Control Build(
        TextBox? searchBox = null,
        string? searchPlaceholder = null,
        IEnumerable<ThemedButton>? actionButtons = null,
        IEnumerable<Control>? extraControls = null)
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true, // Permet le passage à la ligne si pas de place
            BackColor = Theme.Background,
            Padding = new Padding(Theme.Px(4), Theme.Px(8), Theme.Px(4), Theme.Px(8)),
            Margin = Padding.Empty
        };

        // --- Zone recherche (gauche) ---
        if (searchBox != null)
        {
            searchBox.Width = Theme.Px(300);
            searchBox.Font = Theme.Body;
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.PlaceholderText = searchPlaceholder ?? "Rechercher…";
            searchBox.Margin = new Padding(0, Theme.Px(4), Theme.Px(12), Theme.Px(4));
            toolbar.Controls.Add(searchBox);
        }

        // --- Contrôles supplémentaires (filtres, etc.) ---
        if (extraControls != null)
        {
            foreach (var ctrl in extraControls)
            {
                ctrl.Margin = new Padding(0, Theme.Px(4), Theme.Px(12), Theme.Px(4));
                toolbar.Controls.Add(ctrl);
            }
        }

        // --- Séparateur élastique (pousse les boutons à droite) ---
        var spacer = new Panel
        {
            Width = Theme.Px(20),
            Height = Theme.Px(1),
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        toolbar.Controls.Add(spacer);

        // --- Boutons d'action (droite) ---
        if (actionButtons != null)
        {
            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Theme.Background,
                Margin = new Padding(Theme.Px(8), Theme.Px(2), 0, Theme.Px(2))
            };

            foreach (var btn in actionButtons)
            {
                btn.Margin = new Padding(Theme.Px(6), Theme.Px(4), 0, Theme.Px(4));
                buttonPanel.Controls.Add(btn);
            }

            toolbar.Controls.Add(buttonPanel);
        }

        return toolbar;
    }

    /// <summary>Crée une barre d'outils à deux lignes : recherche/filtres en haut, boutons en bas.</summary>
    public static Control BuildTwoRow(
        IEnumerable<Control> topRowControls,
        IEnumerable<ThemedButton> bottomRowButtons)
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Background,
            Padding = new Padding(Theme.Px(4), Theme.Px(6), Theme.Px(4), Theme.Px(6))
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Ligne 1 : recherche + filtres
        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        foreach (var ctrl in topRowControls)
        {
            ctrl.Margin = new Padding(0, 0, Theme.Px(12), 0);
            topPanel.Controls.Add(ctrl);
        }
        toolbar.Controls.Add(topPanel, 0, 0);

        // Ligne 2 : boutons d'action (alignés à droite)
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft, // Boutons alignés à droite
            WrapContents = true,
            BackColor = Theme.Background,
            Margin = new Padding(0, Theme.Px(4), 0, 0)
        };
        foreach (var btn in bottomRowButtons.Reverse()) // Reverse car RightToLeft
        {
            btn.Margin = new Padding(Theme.Px(6), 0, 0, 0);
            bottomPanel.Controls.Add(btn);
        }
        toolbar.Controls.Add(bottomPanel, 0, 1);

        return toolbar;
    }

    /// <summary>Configure un TextBox de recherche standard.</summary>
    public static TextBox CreateSearchBox(string placeholder, int width = 300, EventHandler? onTextChanged = null)
    {
        var tb = new TextBox
        {
            Width = Theme.Px(width),
            Font = Theme.Body,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = placeholder
        };
        if (onTextChanged != null)
            tb.TextChanged += onTextChanged;
        return tb;
    }

    /// <summary>Configure un ThemedButton standard.</summary>
    public static ThemedButton CreateButton(string text, ButtonKind kind, int width, EventHandler onClick)
    {
        var btn = new ThemedButton
        {
            Text = text,
            Kind = kind,
            Width = Theme.Px(width)
        };
        btn.Click += onClick;
        return btn;
    }
}