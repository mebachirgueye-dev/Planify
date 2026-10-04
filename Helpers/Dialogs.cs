namespace Planify.Helpers;

/// <summary>Boîtes de message cohérentes dans toute l'application.</summary>
public static class Dialogs
{
    private const string Caption = "Planify";

    public static void Info(string message, IWin32Window? owner = null) =>
        MessageBox.Show(owner, message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Warning(string message, IWin32Window? owner = null) =>
        MessageBox.Show(owner, message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Error(string message, IWin32Window? owner = null) =>
        MessageBox.Show(owner, message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);

    /// <summary>Demande une confirmation Oui/Non (Non est le choix par défaut).</summary>
    public static bool Confirm(string message, IWin32Window? owner = null) =>
        MessageBox.Show(owner, message, Caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
}
