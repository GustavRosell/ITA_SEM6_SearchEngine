# Modul 2: Intro til Case-System

**Course**: IT-Arkitektur 6. Semester, Erhvervsakademiet Aarhus
**Date**: 27. august 2025
**Status**: ✅ Første iteration modtaget og grundlæggende features implementeret

---

## Overview

Modtog første iteration af search engine PoC-systemet. Grundlæggende search engine med inverted index og SQLite database.

---

## Udleveret System

**Projekter modtaget:**
- `indexer` - Console application til crawling og indexering
- `ConsoleSearch` - Console interface til søgning
- `Shared` - Fælles models og configuration
- Test data: Enron email dataset (small/medium/large)

**Database schema:**
```sql
Document: docId, title, link, date
Word: termId, value
Occurrence: docId, termId (many-to-many)
```

---

## Basis Features

### Indexering
- Crawler der gennemgår .txt filer rekursivt
- Word extraction med separators
- Inverted index opbygning
- SQLite database storage

### Søgning
- Console-baseret interface
- Basic term search
- TF scoring: `score = matching_terms / total_query_terms`
- Resultater med document hits

---

## Assignments Gennemført

I løbet af modulet blev følgende opgaver implementeret:

### Assignment 1: Setup
✅ Installeret .NET 9.0 og SQLite browser
✅ Bygget alle projekter
✅ Opdateret paths til cross-platform support
✅ Kørt indexer og testet basic search

### Assignment 2: Enhanced Statistics
✅ Total word occurrences display
✅ Most frequent words (user-configurable)
✅ Frequency ranking

### Assignment 3: Case Sensitivity Control
✅ Toggle case-sensitive search
✅ Menu option "1"
✅ `Config.CaseSensitive` boolean

### Assignment 4: Timestamp Display
✅ Toggle timestamp display
✅ Menu option "2"
✅ `Config.ViewTimeStamps` boolean

### Assignment 5: Result Limits
✅ Configurable limits (ikke fixed 10)
✅ Menu option "3"
✅ Support for specific numbers eller "all"

### Assignment 6: Pattern Matching
✅ Wildcard search med `?` og `*`
✅ Menu option "4"
✅ `Config.PatternSearch` boolean

---

## Ekstra Forbedringer

**Compact View:**
✅ Menu option "5"
✅ Clean single-line display
✅ Filename extraction

**Help System:**
✅ Type `?` for help
✅ Contextual help med current settings
✅ Examples og command reference

**Bug Fixes:**
✅ Pattern search ordering (filename number extraction)

---

## Files Modified

**Core Files:**
- `ConsoleSearch/Config.cs` - Configuration properties
- `ConsoleSearch/App.cs` - Menu system og display logic
- `indexer/App.cs` - Statistics display
- `SearchLogic.cs` - Search algorithms
- `DatabaseSqlite.cs` - Database queries

---

## Testing

Manual testing med Enron dataset:
- Small: 13 emails (functional)
- Medium: ~5,000 emails (functional + performance)
- Large: ~50,000 emails (performance)

---

## Summary

Første iteration modtaget og alle 6 core assignments gennemført med ekstra features.

**Next**: Modul 3 - Skalering (intro til AKF Scale Cube)
