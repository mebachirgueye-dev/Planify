using Planify.Data;
using Planify.Forms;
using Planify.Helpers;
using Planify.Services;

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

        RunApplication(dbFactory);
    }

    /// <summary>
    /// Boucle de connexion :
    /// - premier lancement (aucun compte) : création du compte administrateur ;
    /// - ensuite : écran de connexion ;
    /// - à chaque déconnexion : retour à l'écran de connexion.
    /// </summary>
    private static void RunApplication(PlanifyDbContextFactory dbFactory)
    {
        var auth = new AuthService(dbFactory);
        var users = new UtilisateurService(dbFactory);

        while (true)
        {
            if (!auth.HasAnyUser())
            {
                // Premier lancement : on crée le premier administrateur.
                using var firstRun = new FirstRunForm(users);
                if (firstRun.ShowDialog() != DialogResult.OK)
                    return; // l'utilisateur a annulé : on quitte le logiciel
            }
            else
            {
                using var login = new LoginForm(auth);
                if (login.ShowDialog() != DialogResult.OK)
                    return; // annulation ou fermeture : on quitte le logiciel
            }

            // Session ouverte : on lance la fenêtre principale.
            using var main = new MainForm(dbFactory);
            main.ShowDialog();

            // La fenêtre principale s'est fermée (déconnexion ou fermeture) :
            // on termine la session et on revient à l'écran de connexion.
            Session.Logout();
        }
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
