"""
DHBW Bibliothek Scraper - K10plus SRU Schnittstelle
Für private Lernzwecke

Nutzt die K10plus SRU-Schnittstelle des Südwestdeutschen Bibliotheksverbunds (SWB)
um Buchdaten abzurufen und in einer lokalen SQLite-Datenbank zu speichern.
"""

import sys
import io

# Windows Console UTF-8 Fix
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

import requests
import sqlite3
import xml.etree.ElementTree as ET
import time
import json
from dataclasses import dataclass
from typing import Optional, List
from pathlib import Path

# Namespaces für XML-Parsing
NAMESPACES = {
    'zs': 'http://www.loc.gov/zing/srw/',
    'marc': 'http://www.loc.gov/MARC21/slim'
}

@dataclass
class Book:
    """Repräsentiert ein Buch aus dem Katalog"""
    ppn: str  # Pica Production Number (eindeutige ID)
    title: str
    authors: List[str]
    isbn: Optional[str]
    year: Optional[str]
    publisher: Optional[str]
    subjects: List[str]
    abstract: Optional[str]
    language: Optional[str]
    pages: Optional[str]
    url: Optional[str]


class K10PlusScraper:
    """
    Scraper für die K10plus SRU-Schnittstelle

    Dokumentation: https://wiki.k10plus.de/display/K10PLUS/SRU
    """

    BASE_URL = "https://sru.k10plus.de/opac-de-627"

    def __init__(self, db_path: str = "library_database.db"):
        self.db_path = db_path
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'DHBW-Lerndatenbank/1.0 (Private Learning Project)'
        })
        self._init_database()

    def _init_database(self):
        """Erstellt die SQLite-Datenbank und Tabellen"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        cursor.execute('''
            CREATE TABLE IF NOT EXISTS books (
                ppn TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                authors TEXT,
                isbn TEXT,
                year TEXT,
                publisher TEXT,
                subjects TEXT,
                abstract TEXT,
                language TEXT,
                pages TEXT,
                url TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        ''')

        cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_title ON books(title)
        ''')
        cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_year ON books(year)
        ''')
        cursor.execute('''
            CREATE INDEX IF NOT EXISTS idx_isbn ON books(isbn)
        ''')

        # Volltextsuche aktivieren
        cursor.execute('''
            CREATE VIRTUAL TABLE IF NOT EXISTS books_fts USING fts5(
                ppn, title, authors, subjects, abstract,
                content='books',
                content_rowid='rowid'
            )
        ''')

        conn.commit()
        conn.close()
        print(f"Datenbank initialisiert: {self.db_path}")

    def search(self, query: str, max_records: int = 100, start_record: int = 1) -> List[Book]:
        """
        Sucht Bücher über die SRU-Schnittstelle

        Args:
            query: Suchanfrage in CQL-Syntax (z.B. "pica.tit=Python" oder "pica.all=Informatik")
            max_records: Maximale Anzahl Ergebnisse pro Anfrage (max 100)
            start_record: Startposition für Paginierung

        Returns:
            Liste von Book-Objekten
        """
        params = {
            'version': '1.1',
            'operation': 'searchRetrieve',
            'query': query,
            'maximumRecords': min(max_records, 100),
            'startRecord': start_record,
            'recordSchema': 'marcxml'
        }

        try:
            response = self.session.get(self.BASE_URL, params=params, timeout=30)
            response.raise_for_status()
            return self._parse_response(response.text)
        except requests.RequestException as e:
            print(f"Fehler bei der Anfrage: {e}")
            return []

    def _parse_response(self, xml_text: str) -> List[Book]:
        """Parst die XML-Antwort und extrahiert Buchdaten"""
        books = []

        try:
            root = ET.fromstring(xml_text)

            # Anzahl der Treffer ausgeben
            num_records = root.find('.//zs:numberOfRecords', NAMESPACES)
            if num_records is not None:
                print(f"Gefundene Treffer: {num_records.text}")

            # Records durchgehen
            for record in root.findall('.//zs:record', NAMESPACES):
                book = self._parse_marc_record(record)
                if book:
                    books.append(book)

        except ET.ParseError as e:
            print(f"XML-Parsing-Fehler: {e}")

        return books

    def _parse_marc_record(self, record_elem) -> Optional[Book]:
        """Parst einen einzelnen MARC21-Record"""
        marc = record_elem.find('.//marc:record', NAMESPACES)
        if marc is None:
            return None

        def get_field(tag: str, subfield: str = 'a') -> Optional[str]:
            """Hilfsfunktion zum Extrahieren eines MARC-Feldes"""
            field = marc.find(f".//marc:datafield[@tag='{tag}']/marc:subfield[@code='{subfield}']", NAMESPACES)
            return field.text if field is not None else None

        def get_all_subfields(tag: str, subfield: str = 'a') -> List[str]:
            """Extrahiert alle Vorkommen eines Subfeldes"""
            fields = marc.findall(f".//marc:datafield[@tag='{tag}']/marc:subfield[@code='{subfield}']", NAMESPACES)
            return [f.text for f in fields if f.text]

        def get_control_field(tag: str) -> Optional[str]:
            """Extrahiert ein Kontrollfeld"""
            field = marc.find(f".//marc:controlfield[@tag='{tag}']", NAMESPACES)
            return field.text if field is not None else None

        # PPN (eindeutige ID)
        ppn = get_control_field('001')
        if not ppn:
            return None

        # Titel (245$a + 245$b für Untertitel)
        title = get_field('245', 'a') or "Unbekannt"
        subtitle = get_field('245', 'b')
        if subtitle:
            title = f"{title} : {subtitle}"

        # Autoren (100$a für Hauptautor, 700$a für weitere)
        authors = []
        main_author = get_field('100', 'a')
        if main_author:
            authors.append(main_author)
        authors.extend(get_all_subfields('700', 'a'))

        # ISBN
        isbn = get_field('020', 'a')

        # Erscheinungsjahr aus Kontrollfeld 008 (Position 7-10)
        control_008 = get_control_field('008')
        year = control_008[7:11] if control_008 and len(control_008) > 10 else None

        # Verlag (264$b oder 260$b)
        publisher = get_field('264', 'b') or get_field('260', 'b')

        # Schlagwörter
        subjects = get_all_subfields('650', 'a') + get_all_subfields('653', 'a')

        # Abstract/Zusammenfassung
        abstract = get_field('520', 'a')

        # Sprache aus Kontrollfeld 008 (Position 35-37)
        language = control_008[35:38] if control_008 and len(control_008) > 37 else None

        # Seitenzahl (300$a)
        pages = get_field('300', 'a')

        # URL für elektronische Ressource
        url = get_field('856', 'u')

        return Book(
            ppn=ppn,
            title=title.strip(),
            authors=authors,
            isbn=isbn,
            year=year,
            publisher=publisher.strip() if publisher else None,
            subjects=subjects,
            abstract=abstract,
            language=language,
            pages=pages,
            url=url
        )

    def save_books(self, books: List[Book]):
        """Speichert Bücher in der Datenbank"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        saved_count = 0
        for book in books:
            try:
                cursor.execute('''
                    INSERT OR REPLACE INTO books
                    (ppn, title, authors, isbn, year, publisher, subjects, abstract, language, pages, url)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ''', (
                    book.ppn,
                    book.title,
                    json.dumps(book.authors, ensure_ascii=False),
                    book.isbn,
                    book.year,
                    book.publisher,
                    json.dumps(book.subjects, ensure_ascii=False),
                    book.abstract,
                    book.language,
                    book.pages,
                    book.url
                ))
                saved_count += 1
            except sqlite3.Error as e:
                print(f"Fehler beim Speichern von {book.ppn}: {e}")

        conn.commit()
        conn.close()
        print(f"{saved_count} Bücher gespeichert")

    def search_and_save(self, query: str, total_records: int = 1000, delay: float = 1.0):
        """
        Durchsucht den Katalog und speichert alle Ergebnisse

        Args:
            query: Suchanfrage
            total_records: Gewünschte Gesamtanzahl (wird in 100er-Blöcken abgerufen)
            delay: Wartezeit zwischen Anfragen (Sekunden) - bitte einhalten!
        """
        print(f"Starte Suche: {query}")
        print(f"Ziel: {total_records} Bücher")
        print("-" * 50)

        all_books = []
        start = 1

        while len(all_books) < total_records:
            print(f"Abrufen: Records {start} bis {start + 99}...")

            books = self.search(query, max_records=100, start_record=start)

            if not books:
                print("Keine weiteren Ergebnisse")
                break

            all_books.extend(books)
            self.save_books(books)

            start += 100

            # Rate Limiting - sei nett zum Server!
            if len(all_books) < total_records:
                print(f"Warte {delay}s...")
                time.sleep(delay)

        print("-" * 50)
        print(f"Fertig! {len(all_books)} Bücher abgerufen und gespeichert")
        return all_books

    def get_stats(self) -> dict:
        """Gibt Statistiken über die Datenbank zurück"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        stats = {}

        cursor.execute("SELECT COUNT(*) FROM books")
        stats['total_books'] = cursor.fetchone()[0]

        cursor.execute("SELECT COUNT(DISTINCT year) FROM books WHERE year IS NOT NULL")
        stats['unique_years'] = cursor.fetchone()[0]

        cursor.execute("SELECT year, COUNT(*) FROM books WHERE year IS NOT NULL GROUP BY year ORDER BY COUNT(*) DESC LIMIT 10")
        stats['top_years'] = cursor.fetchall()

        cursor.execute("SELECT language, COUNT(*) FROM books WHERE language IS NOT NULL GROUP BY language ORDER BY COUNT(*) DESC LIMIT 5")
        stats['top_languages'] = cursor.fetchall()

        conn.close()
        return stats

    def fulltext_search(self, search_term: str, limit: int = 20) -> List[dict]:
        """Volltextsuche in der lokalen Datenbank"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()

        # Rebuild FTS index if needed
        cursor.execute("INSERT INTO books_fts(books_fts) VALUES('rebuild')")

        cursor.execute('''
            SELECT b.* FROM books b
            JOIN books_fts fts ON b.ppn = fts.ppn
            WHERE books_fts MATCH ?
            LIMIT ?
        ''', (search_term, limit))

        columns = [desc[0] for desc in cursor.description]
        results = [dict(zip(columns, row)) for row in cursor.fetchall()]

        conn.close()
        return results


# Vordefinierte Suchbegriffe für DHBW-relevante Themen
DHBW_QUERIES = {
    'informatik': 'pica.all=Informatik',
    'wirtschaft': 'pica.all=Betriebswirtschaft',
    'technik': 'pica.all=Ingenieurwissenschaft',
    'maschinenbau': 'pica.all=Maschinenbau',
    'elektrotechnik': 'pica.all=Elektrotechnik',
    'programmierung': 'pica.all=Programmierung',
    'python': 'pica.tit=Python',
    'java': 'pica.tit=Java',
    'datenbanken': 'pica.all=Datenbank',
    'ki': 'pica.all="Künstliche Intelligenz"',
    'machine_learning': 'pica.all="Machine Learning"',
    'statistik': 'pica.all=Statistik',
    'mathematik': 'pica.all=Mathematik',
    'controlling': 'pica.all=Controlling',
    'marketing': 'pica.all=Marketing',
}


def main():
    """Hauptprogramm - Beispielnutzung"""

    # Datenbank im tools-Ordner erstellen
    db_path = Path(__file__).parent / "dhbw_library.db"
    scraper = K10PlusScraper(str(db_path))

    print("=" * 60)
    print("DHBW Bibliothek Scraper - K10plus SRU")
    print("=" * 60)
    print()
    print("Verfügbare vordefinierte Suchen:")
    for key in DHBW_QUERIES:
        print(f"  - {key}")
    print()

    # Beispiel: Python-Bücher abrufen
    print("Starte Beispielsuche: Python-Bücher")
    print("-" * 60)

    scraper.search_and_save(
        query=DHBW_QUERIES['python'],
        total_records=500,  # Erstmal nur 500 zum Testen
        delay=1.5  # 1.5 Sekunden Pause zwischen Anfragen
    )

    # Statistiken anzeigen
    print()
    print("Datenbankstatistiken:")
    print("-" * 60)
    stats = scraper.get_stats()
    print(f"Gesamtanzahl Bücher: {stats['total_books']}")
    print(f"Verschiedene Erscheinungsjahre: {stats['unique_years']}")
    print()
    print("Top 10 Erscheinungsjahre:")
    for year, count in stats.get('top_years', []):
        print(f"  {year}: {count} Bücher")
    print()
    print("Top 5 Sprachen:")
    for lang, count in stats.get('top_languages', []):
        print(f"  {lang}: {count} Bücher")


if __name__ == "__main__":
    main()
