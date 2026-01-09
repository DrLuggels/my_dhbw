using Microsoft.Extensions.Configuration;
using System.Text;

namespace DHBWAutomation.Backend.Shared.Helpers;

/// <summary>
/// Helper für Ver- und Entschlüsselung sensibler Daten (API Keys, Passwörter)
/// Verwendet XOR-Verschlüsselung mit konfigurierbarem Key
/// </summary>
public class EncryptionHelper
{
    private readonly string _encryptionKey;

    public EncryptionHelper(IConfiguration configuration)
    {
        _encryptionKey = configuration["Encryption:Key"] ?? "DefaultEncryptionKey123!";
    }

    /// <summary>
    /// Verschlüsselt einen String mit XOR-Verschlüsselung
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            var encrypted = new byte[plainBytes.Length];
            for (int i = 0; i < plainBytes.Length; i++)
            {
                encrypted[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(encrypted);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Entschlüsselt einen mit Encrypt() verschlüsselten String
    /// </summary>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
            var encryptedBytes = Convert.FromBase64String(encryptedText);

            var decrypted = new byte[encryptedBytes.Length];
            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decrypted[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
