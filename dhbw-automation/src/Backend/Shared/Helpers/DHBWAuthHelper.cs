using System.Text.RegularExpressions;

namespace DHBWAutomation.Backend.Shared.Helpers;

/// <summary>
/// Helper-Klasse für DHBW-spezifische Authentifizierungslogik
/// </summary>
public static class DHBWAuthHelper
{
    /// <summary>
    /// Extrahiert den Benutzernamen aus einer DHBW-E-Mail-Adresse für Moodle-Login
    /// Beispiel: "student123@dhbw-ravensburg.de" -> "student123"
    /// </summary>
    /// <param name="email">Die E-Mail-Adresse des Benutzers</param>
    /// <returns>Der extrahierte Benutzername oder die Original-E-Mail falls keine DHBW-Adresse</returns>
    public static string GetMoodleUsername(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return email;

        // Prüfe ob es eine DHBW-Ravensburg E-Mail ist
        if (email.EndsWith("@dhbw-ravensburg.de", StringComparison.OrdinalIgnoreCase))
        {
            // Extrahiere nur den Teil vor dem @
            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                return email.Substring(0, atIndex);
            }
        }

        // Für andere E-Mail-Adressen, gebe die volle E-Mail zurück
        return email;
    }

    /// <summary>
    /// Generiert den Domain-Benutzernamen für DHBW-Systeme (z.B. Active Directory)
    /// Beispiel: "student123@dhbw-ravensburg.de" -> "domab\student123"
    /// </summary>
    /// <param name="email">Die E-Mail-Adresse des Benutzers</param>
    /// <param name="domain">Die zu verwendende Domain (Standard: "domab")</param>
    /// <returns>Der formatierte Domain-Benutzername</returns>
    public static string GetDomainUsername(string email, string domain = "domab")
    {
        if (string.IsNullOrWhiteSpace(email))
            return email;

        var username = GetMoodleUsername(email); // Nutze die gleiche Logik für den Benutzernamen
        return $"{domain}\\{username}";
    }

    /// <summary>
    /// Extrahiert den Benutzernamen aus einer E-Mail-Adresse für IMAP/SMTP-Login
    /// Bei DHBW-Adressen wird nur der Teil vor @ verwendet
    /// </summary>
    /// <param name="email">Die E-Mail-Adresse</param>
    /// <returns>Der IMAP/SMTP-Benutzername</returns>
    public static string GetMailUsername(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return email;

        // Für DHBW-E-Mail-Adressen: nur der Teil vor @
        if (email.EndsWith("@dhbw-ravensburg.de", StringComparison.OrdinalIgnoreCase))
        {
            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                return email.Substring(0, atIndex);
            }
        }

        // Für andere E-Mail-Provider (Gmail, Outlook, etc.): volle E-Mail-Adresse
        return email;
    }

    /// <summary>
    /// Validiert, ob eine E-Mail-Adresse eine gültige DHBW-Adresse ist
    /// </summary>
    /// <param name="email">Die zu prüfende E-Mail-Adresse</param>
    /// <returns>True wenn es eine DHBW-Adresse ist</returns>
    public static bool IsDHBWEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return email.EndsWith("@dhbw-ravensburg.de", StringComparison.OrdinalIgnoreCase) ||
               email.EndsWith("@dhbw.de", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extrahiert die Matrikelnummer aus einer DHBW-E-Mail-Adresse
    /// Beispiel: "s123456@dhbw-ravensburg.de" -> "123456"
    /// </summary>
    /// <param name="email">Die E-Mail-Adresse</param>
    /// <returns>Die Matrikelnummer oder null falls nicht extrahierbar</returns>
    public static string? ExtractMatriculationNumber(string email)
    {
        if (!IsDHBWEmail(email))
            return null;

        var username = GetMoodleUsername(email);

        // Typisches DHBW-Format: s123456 oder ähnlich
        var match = Regex.Match(username, @"s?(\d{6})", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    /// <summary>
    /// Bestimmt den passenden Authentifizierungstyp basierend auf der E-Mail
    /// </summary>
    /// <param name="email">Die E-Mail-Adresse</param>
    /// <returns>Der Authentifizierungstyp</returns>
    public static DHBWAuthType GetAuthType(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return DHBWAuthType.Standard;

        if (email.EndsWith("@dhbw-ravensburg.de", StringComparison.OrdinalIgnoreCase))
            return DHBWAuthType.DHBW;

        if (email.EndsWith("@dentsplysirona.com", StringComparison.OrdinalIgnoreCase))
            return DHBWAuthType.Corporate;

        return DHBWAuthType.Standard;
    }
}

/// <summary>
/// Authentifizierungstypen für verschiedene Systeme
/// </summary>
public enum DHBWAuthType
{
    /// <summary>Standard-Authentifizierung (volle E-Mail)</summary>
    Standard,

    /// <summary>DHBW-System (nur Username ohne Domain)</summary>
    DHBW,

    /// <summary>Corporate/Firmen-System (Domain\Username)</summary>
    Corporate
}
