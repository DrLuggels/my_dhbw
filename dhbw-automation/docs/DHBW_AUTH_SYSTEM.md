# DHBW Authentifizierungs-System

## Übersicht

Das DHBW Automation System verwendet intelligente Username-Konvertierung für verschiedene DHBW-Systeme.

## Automatische Username-Konvertierung

### 1. Moodle-Login

**Problem:** Moodle erwartet nur den Benutzernamen ohne Domain.

**Lösung:**
```
E-Mail:           student123@dhbw-ravensburg.de
Moodle-Username:  student123
```

**Verwendung im Code:**
```csharp
using DHBWAutomation.Backend.Shared.Helpers;

var email = "student123@dhbw-ravensburg.de";
var moodleUsername = DHBWAuthHelper.GetMoodleUsername(email);
// Result: "student123"
```

### 2. E-Mail-Login (IMAP/SMTP)

**Problem:** DHBW-Mail-Server erwarten nur den Username ohne @domain.

**Lösung:**
```
E-Mail:         s123456@dhbw-ravensburg.de
IMAP-Username:  s123456
```

**Verwendung im Code:**
```csharp
var email = "s123456@dhbw-ravensburg.de";
var mailUsername = DHBWAuthHelper.GetMailUsername(email);
// Result: "s123456"
```

### 3. Domain-Login (Active Directory)

**Problem:** Firmen-Systeme benötigen Domain\Username Format.

**Lösung:**
```
E-Mail:           frank.moder@dentsplysirona.com
Domain-Username:  domab\frank.moder
```

**Verwendung im Code:**
```csharp
var email = "frank.moder@dentsplysirona.com";
var domainUsername = DHBWAuthHelper.GetDomainUsername(email);
// Result: "domab\frank.moder"

// Optional: Custom Domain
var customDomain = DHBWAuthHelper.GetDomainUsername(email, "COMPANY");
// Result: "COMPANY\frank.moder"
```

## Weitere Helper-Funktionen

### E-Mail-Validierung

```csharp
bool isDHBW = DHBWAuthHelper.IsDHBWEmail("student@dhbw-ravensburg.de");
// Result: true
```

### Matrikelnummer extrahieren

```csharp
var matriculation = DHBWAuthHelper.ExtractMatriculationNumber("s123456@dhbw-ravensburg.de");
// Result: "123456"
```

### Authentifizierungstyp bestimmen

```csharp
var authType = DHBWAuthHelper.GetAuthType("student@dhbw-ravensburg.de");
// Result: DHBWAuthType.DHBW

var authType2 = DHBWAuthHelper.GetAuthType("user@dentsplysirona.com");
// Result: DHBWAuthType.Corporate
```

## Integration in Services

### Moodle-Service

```csharp
public class MoodleService
{
    private readonly MoodleApiClient _moodleClient;

    public async Task<MoodleUser> GetUserDataAsync(string userEmail)
    {
        // Automatische Konvertierung von E-Mail zu Moodle-Username
        var user = await _moodleClient.GetUserByEmailAsync(userEmail);
        return user;
    }
}
```

### Mail-Service

```csharp
public class MailService
{
    public void ConnectToIMAP(string email, string password)
    {
        // Konvertiere E-Mail zu IMAP-Username
        var username = DHBWAuthHelper.GetMailUsername(email);

        using var client = new ImapClient();
        client.Connect("imap.dhbw-ravensburg.de", 993, true);
        client.Authenticate(username, password); // Nutzt "student123" statt "student123@dhbw-ravensburg.de"
    }
}
```

## Konfiguration

Die Konfiguration erfolgt über die `.env`-Datei:

```env
# Moodle
MOODLE_BASE_URL=https://moodle.dhbw-ravensburg.de
MOODLE_TOKEN=your_token_here

# DHBW-Mail
MAIL_STUDY_EMAIL=s123456@dhbw-ravensburg.de
MAIL_STUDY_IMAP_HOST=imap.dhbw-ravensburg.de

# Firmen-Mail
MAIL_WORK_EMAIL=frank.moder@dentsplysirona.com
```

Das System konvertiert automatisch die E-Mails zum richtigen Format.

## Vorteile

1. **Konsistenz:** Benutzer können überall ihre E-Mail-Adresse verwenden
2. **Automatisch:** Keine manuelle Username-Konvertierung nötig
3. **Flexibel:** Unterstützt verschiedene Authentifizierungssysteme
4. **Wartbar:** Zentrale Logik in `DHBWAuthHelper`

## Testing

```csharp
[Test]
public void TestMoodleUsernameConversion()
{
    var email = "student123@dhbw-ravensburg.de";
    var username = DHBWAuthHelper.GetMoodleUsername(email);

    Assert.AreEqual("student123", username);
}

[Test]
public void TestDomainUsernameConversion()
{
    var email = "frank.moder@dentsplysirona.com";
    var domainUsername = DHBWAuthHelper.GetDomainUsername(email);

    Assert.AreEqual("domab\\frank.moder", domainUsername);
}
```

## Fehlerbehandlung

Der Helper ist robust und behandelt Fehler gracefully:

```csharp
// Null/Empty Strings
DHBWAuthHelper.GetMoodleUsername(null);      // Returns: null
DHBWAuthHelper.GetMoodleUsername("");        // Returns: ""

// Nicht-DHBW E-Mails
DHBWAuthHelper.GetMoodleUsername("user@gmail.com");  // Returns: "user@gmail.com"
```
