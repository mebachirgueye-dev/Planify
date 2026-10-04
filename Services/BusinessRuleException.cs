namespace Planify.Services;

/// <summary>
/// Erreur "métier" : la donnée saisie est refusée pour une raison que l'utilisateur peut comprendre et corriger
/// (champ manquant, doublon, plus tard : conflit horaire...). Le message est destiné à être affiché tel quel.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
