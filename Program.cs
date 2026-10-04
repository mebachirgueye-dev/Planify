using Planify.Data;
using Planify.Forms;
using Planify.Helpers;

namespace Planify;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Lit les paramètres du .csproj (DPI, styles visuels...).
        ApplicationConfiguration.Initialize();

        // Aucune erreur ne doit fermer le logiciel sans explication.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportUnexpectedError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ReportUnexpectedError(e.ExceptionObject as Exception ?? new Exception("Erreur inconnue"));

        var dbFactory = new PlanifyDbContextFactory();

        try
        {
            // Crée la base au premier lancement, applique les migrations ensuite.
            DatabaseInitializer.Initialize(dbFactory);
        }
        catch (Exception ex)
        {
            AppLog.Error("Initialisation de la base de données", ex);
            Dialogs.Error(
                "Planify n'a pas pu ouvrir sa base de données." + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "Fichier : " + AppPaths.DatabaseFile + Environment.NewLine +
                "Détails : " + AppLog.LogFile);
            return;
        }

        Application.Run(new MainForm(dbFactory));
    }

    private static void ReportUnexpectedError(Exception ex)
    {
        AppLog.Error("Erreur inattendue", ex);
        Dialogs.Error(
            "Une erreur inattendue s'est produite." + Environment.NewLine + Environment.NewLine +
            ex.Message + Environment.NewLine + Environment.NewLine +
            "Détails enregistrés dans : " + AppLog.LogFile);
    }
}
