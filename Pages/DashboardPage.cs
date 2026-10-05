using Planify.Controls;
using Planify.Helpers;
using Planify.Services;

namespace Planify.Pages;

/// <summary>
/// Tableau de bord. Affiche des statistiques en temps réel :
/// - Nombre de bâtiments
/// - Nombre de salles
/// - Réservations du jour
/// - État de la base SQLite
/// </summary>
public sealed class DashboardPage : UserControl
{
    public DashboardPage(BatimentService batiments, DatabaseInfoService databaseInfo, SalleService? salles = null, ReservationService? reservations = null)
    {
        BackColor = Theme.Background;
        Padding = new Padding(Theme.Px(28), Theme.Px(8), Theme.Px(28), Theme.Px(28));

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Px(124)));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // --- Cartes statistiques ---
        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Padding.Empty,
            BackColor = Theme.Background
        };
        for (int i = 0; i < 4; i++)
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        int batimentCount = batiments.Count();
        int salleCount = salles?.Count() ?? 0;
        int reservationTodayCount = reservations?.GetByDate(DateTime.Now).Count ?? 0;

        AddCard(cards, 0, "Bâtiments", batimentCount.ToString(), "Enregistrés dans la base", Theme.Primary);
        AddCard(cards, 1, "Salles", salleCount.ToString(), "Disponibles", Theme.Accent);
        AddCard(cards, 2, "Réservations du jour", reservationTodayCount.ToString(), "En cours", Theme.PrimaryLight);
        AddCard(cards, 3, "Événements à venir", "—", "Bientôt disponible", Theme.Success);

        // --- État de la base de données ---
        var dbCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, Theme.Px(16), 0, 0)
        };
        dbCard.Controls.Add(BuildDatabaseInfo(databaseInfo.GetStatus()));

        root.Controls.Add(cards, 0, 0);
        root.Controls.Add(dbCard, 0, 1);
        Controls.Add(root);
    }

    private static void AddCard(TableLayoutPanel table, int column, string title, string value, string caption, Color accent)
    {
        var card = new StatCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, column < 3 ? Theme.Px(16) : 0, 0),
            Title = title,
            Value = value,
            Caption = caption,
            AccentColor = accent
        };
        table.Controls.Add(card, column, 0);
    }

    private static Control BuildDatabaseInfo(DatabaseStatus status)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Surface
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.Px(220)));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        var heading = new Label
        {
            Text = "Base de données locale",
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            Dock = DockStyle.Fill,
            Height = Theme.Px(36),
            TextAlign = ContentAlignment.MiddleLeft
        };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Px(40)));
        table.Controls.Add(heading, 0, row);
        table.SetColumnSpan(heading, 2);
        row++;

        AddInfoRow(table, ref row, "État", status.CanConnect ? "Connectée" : "Indisponible",
            status.CanConnect ? Theme.Success : Theme.Danger);
        AddInfoRow(table, ref row, "Fichier", status.FilePath);
        AddInfoRow(table, ref row, "Taille", FormatSize(status.SizeBytes));
        AddInfoRow(table, ref row, "Migrations appliquées",
            status.AppliedMigrations + (status.PendingMigrations > 0 ? $" ({status.PendingMigrations} en attente)" : string.Empty));
        AddInfoRow(table, ref row, "Dernière migration", status.LastMigration ?? "—");

        if (status.Error is not null)
            AddInfoRow(table, ref row, "Erreur", status.Error, Theme.Danger);

        table.RowCount = row;
        return table;
    }

    private static void AddInfoRow(TableLayoutPanel table, ref int row, string label, string value, Color? valueColor = null)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Px(30)));

        table.Controls.Add(new Label
        {
            Text = label,
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        table.Controls.Add(new Label
        {
            Text = value,
            Font = Theme.Body,
            ForeColor = valueColor ?? Theme.Navy,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        }, 1, row);

        row++;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} o";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} Ko";
        return $"{bytes / (1024.0 * 1024.0):0.#} Mo";
    }
}
