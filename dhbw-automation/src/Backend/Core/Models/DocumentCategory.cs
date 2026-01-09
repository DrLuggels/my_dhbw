namespace DHBWAutomation.Backend.Core.Models;

// Enum für strukturierte Dokumenten-Kategorisierung
public enum DocumentCategory
{
    // Vom Dozenten
    UnterrichtsMaterial = 0,  // Folien, Skripte
    Protokoll = 1,            // Protokolle, Mitschriften vom Dozenten
    Aufgabenstellung = 2,     // Assignments, Projektbeschreibungen

    // Vom Studenten
    EigeneNotizen = 3,        // Handgeschriebene/getippte Notizen
    Mitschrieb = 4,           // Vorlesungsmitschrieb
    Lösung = 5,               // Lösungen zu Aufgaben

    // Projekte
    ProjektDokumentation = 6,
    ProjektCode = 7,
    ProjektIdee = 8,

    // Sonstiges
    Administrativ = 9,        // Anträge, Bescheinigungen
    Persönlich = 10,          // Persönliche Notizen
    Sonstiges = 11
}
