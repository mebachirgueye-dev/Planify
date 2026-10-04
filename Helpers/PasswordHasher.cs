using System.Security.Cryptography;

namespace Planify.Helpers;

/// <summary>
/// Hachage des mots de passe avec PBKDF2 (<see cref="Rfc2898DeriveBytes"/>), fourni par le
/// framework .NET : aucune dépendance externe. Le résultat combine le nombre d'itérations,
/// un sel aléatoire et le hachage, de sorte que deux mots de passe identiques produisent
/// des hachages différents et que le mot de passe d'origine ne peut pas être reconstitué.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;        // 128 bits
    private const int KeySize = 32;         // 256 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>Format du hachage stocké : <c>PBKDF2$itérations$sel$clé</c> (sel et clé en base 64).</summary>
    public const string Scheme = "PBKDF2";

    /// <summary>Calcule le hachage d'un mot de passe avec un sel aléatoire.</summary>
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    /// <summary>
    /// Vérifie un mot de passe contre un hachage stocké.
    /// Retourne <c>false</c> si le hachage est illisible ou si le mot de passe ne correspond pas.
    /// </summary>
    public static bool Verify(string password, string storedHash)
    {
        try
        {
            string[] parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != Scheme)
                return false;

            int iterations = int.Parse(parts[1]);
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
            // Comparaison à temps constant : évite les attaques par analyse du temps de réponse.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false; // hachage invalide : on refuse la connexion
        }
    }
}
