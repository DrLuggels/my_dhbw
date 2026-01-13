"""
Interaktive CLI für die DHBW Lerndatenbank
"""

import argparse
import json
from pathlib import Path
from library_scraper import K10PlusScraper, DHBW_QUERIES


def cmd_search(args):
    """Sucht online im K10plus-Katalog"""
    scraper = K10PlusScraper(args.db)

    query = DHBW_QUERIES.get(args.query, f"pica.all={args.query}")
    print(f"Suche: {query}")

    scraper.search_and_save(
        query=query,
        total_records=args.limit,
        delay=args.delay
    )


def cmd_local(args):
    """Sucht in der lokalen Datenbank"""
    scraper = K10PlusScraper(args.db)

    results = scraper.fulltext_search(args.term, limit=args.limit)

    if not results:
        print("Keine Treffer gefunden")
        return

    print(f"\n{len(results)} Treffer:\n")
    for i, book in enumerate(results, 1):
        authors = json.loads(book.get('authors', '[]'))
        author_str = ", ".join(authors[:2]) if authors else "Unbekannt"
        print(f"{i}. {book['title'][:60]}...")
        print(f"   Autor(en): {author_str}")
        print(f"   Jahr: {book.get('year', 'k.A.')} | ISBN: {book.get('isbn', 'k.A.')}")
        print()


def cmd_stats(args):
    """Zeigt Datenbankstatistiken"""
    scraper = K10PlusScraper(args.db)
    stats = scraper.get_stats()

    print("\n=== Datenbankstatistiken ===\n")
    print(f"Gesamtanzahl Bücher: {stats['total_books']}")
    print(f"Verschiedene Jahre: {stats['unique_years']}")

    if stats.get('top_years'):
        print("\nTop 10 Erscheinungsjahre:")
        for year, count in stats['top_years']:
            bar = "#" * min(count // 10, 40)
            print(f"  {year}: {count:5} {bar}")

    if stats.get('top_languages'):
        print("\nSprachen:")
        for lang, count in stats['top_languages']:
            print(f"  {lang}: {count}")


def cmd_bulk(args):
    """Lädt mehrere Kategorien auf einmal"""
    scraper = K10PlusScraper(args.db)

    categories = args.categories.split(',') if args.categories else list(DHBW_QUERIES.keys())

    print(f"Lade {len(categories)} Kategorien...")
    print(f"Bücher pro Kategorie: {args.per_category}")
    print()

    for cat in categories:
        if cat not in DHBW_QUERIES:
            print(f"Unbekannte Kategorie: {cat}, überspringe...")
            continue

        print(f"\n>>> Kategorie: {cat.upper()}")
        scraper.search_and_save(
            query=DHBW_QUERIES[cat],
            total_records=args.per_category,
            delay=args.delay
        )


def main():
    parser = argparse.ArgumentParser(
        description="DHBW Bibliothek Lerndatenbank CLI",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Beispiele:
  # Python-Bücher suchen und speichern
  python library_cli.py search python --limit 1000

  # In lokaler Datenbank suchen
  python library_cli.py local "machine learning"

  # Statistiken anzeigen
  python library_cli.py stats

  # Mehrere Kategorien auf einmal laden
  python library_cli.py bulk --categories python,java,ki --per-category 500

Verfügbare Kategorien:
  """ + ", ".join(DHBW_QUERIES.keys())
    )

    parser.add_argument('--db', default='dhbw_library.db',
                        help='Pfad zur SQLite-Datenbank')

    subparsers = parser.add_subparsers(dest='command', help='Befehle')

    # search command
    p_search = subparsers.add_parser('search', help='Online im K10plus suchen')
    p_search.add_argument('query', help='Suchbegriff oder vordefinierte Kategorie')
    p_search.add_argument('--limit', type=int, default=500,
                          help='Maximale Anzahl Bücher (default: 500)')
    p_search.add_argument('--delay', type=float, default=1.5,
                          help='Pause zwischen Anfragen in Sekunden (default: 1.5)')
    p_search.set_defaults(func=cmd_search)

    # local command
    p_local = subparsers.add_parser('local', help='In lokaler Datenbank suchen')
    p_local.add_argument('term', help='Suchbegriff')
    p_local.add_argument('--limit', type=int, default=20,
                         help='Maximale Treffer (default: 20)')
    p_local.set_defaults(func=cmd_local)

    # stats command
    p_stats = subparsers.add_parser('stats', help='Datenbankstatistiken anzeigen')
    p_stats.set_defaults(func=cmd_stats)

    # bulk command
    p_bulk = subparsers.add_parser('bulk', help='Mehrere Kategorien laden')
    p_bulk.add_argument('--categories',
                        help='Komma-getrennte Liste (leer = alle)')
    p_bulk.add_argument('--per-category', type=int, default=500,
                        help='Bücher pro Kategorie (default: 500)')
    p_bulk.add_argument('--delay', type=float, default=2.0,
                        help='Pause zwischen Anfragen (default: 2.0)')
    p_bulk.set_defaults(func=cmd_bulk)

    args = parser.parse_args()

    if args.command is None:
        parser.print_help()
        return

    args.func(args)


if __name__ == "__main__":
    main()
