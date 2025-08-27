# SearchEngine PoC - Opgaver og Fremgang

## Oversigt
Denne fil holder styr på alle opgaver til SearchEngine projektet og vores fremgang med hver opgave.

****************************************************
**MODUL 2 - SØGEMASKINE OPGAVER**
****************************************************

## Opgave 1: Opsætning og Installation
**Status**: 🔄 I gang  
**Beskrivelse**: Få søgemaskinen til at køre på computeren

### Krav:
- [x] Koden kræver .net 9.0 - skal installeres
- [x] SQLite browser installation - se https://sqlitebrowser.org/dl/
- [x] Builde alle projekter og opdatere NuGet packages
- [x] Lægge dokumenter i indexering-folderen (brug seData.zip filer)
- [x] Opdatere `Shared.Paths` så database-stien peger rigtigt
- [x] Opdatere `Indexer.Config` så fil-stien til indeksering er korrekt
- [ ] Køre indexer programmet
- [ ] Inspicere databasen og lave stikprøver af indekserede ord
- [ ] Køre searchConsole og afprøve med 1 ord, 2 ord og flere ord

### 🔧 Sådan Kører Du Systemet:

**1. Build projektet:**
```bash
cd "C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main"
dotnet build
```

**2. Kør indexer (opretter database og indekserer filer):**
```bash
cd indexer
dotnet run
```

**3. Kør søgemaskinen:**
```bash
cd ConsoleSearch
dotnet run
```

**4. Inspicer databasen (SQLite Browser):**
- Åbn SQLite Browser (DB Browser for SQLite)
- File → Open Database
- Vælg: `C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\searchDB.db`
- Se på tabellerne: `document`, `word`, `occ` (occurrence)
- Lav stikprøver - check at dokumenter og ord er indekseret korrekt

**5. Test søgninger:**
- Skriv et ord og tryk Enter
- Skriv flere ord adskilt af mellemrum
- Skriv `q` for at afslutte

### Nuværende Konfiguration (skal ændres):
```csharp
// Shared\Paths.cs
public static string DATABASE = @"/Users/oleeriksen/Data/searchDBmedium.db";

// indexer\Config.cs  
public static string FOLDER = @"/Users/oleeriksen/Data/seData/medium";
```

### ✅ Hvad Vi Har Gjort:
- [x] Analyseret hele projektet og forstået arkitekturen
- [x] Identificeret konfigurations-filer der skal opdateres
- [x] Opdateret `Shared\Paths.cs` med Windows database sti
- [x] Opdateret `indexer\Config.cs` med Windows folder sti
- [x] Buildet projekterne succesfuldt (0 warnings, 0 errors)
- [x] Kørt indexer og oprettet database (13MB, 3034 dokumenter, 31079 forskellige ord)
- [ ] Testet søgemaskinen med forskellige søgninger

### 📁 Opdaterede Windows Stier:
```csharp
// Shared\Paths.cs:
DATABASE = @"C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\searchDB.db";

// indexer\Config.cs:
FOLDER = @"C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\seData copy\medium";
```

### 🎉 Indexer Resultater:
```
DONE! used 111991,158 ms (1 minut 52 sekunder)
Indexed 3034 documents
Number of different words: 31079
Database size: 13 MB
```

**De første 10 ord i indekset:**
1. Message (ID: 1)
2. ID (ID: 2) 
3. 29790972 (ID: 3)
4. 1075855665306 (ID: 4)
5. JavaMail (ID: 5)
6. evans (ID: 6)
7. thyme (ID: 7)
8. Date (ID: 8)
9. Wed (ID: 9)
10. 13 (ID: 10)

### 🗃️ Database Struktur (3 tabeller):

**1. `document` tabel:**
- `id` (INTEGER PRIMARY KEY) - Unikt dokument ID
- `url` (TEXT) - Fil sti til dokumentet
- `idxTime` (TEXT) - Tidspunkt for indeksering
- `creationTime` (TEXT) - Dokumentets oprettelsestidspunkt

**2. `word` tabel:**
- `id` (INTEGER PRIMARY KEY) - Unikt ord ID  
- `name` (VARCHAR(50)) - Selve ordet

**3. `occ` (occurrence) tabel:**
- `wordId` (INTEGER) - Reference til word.id
- `docId` (INTEGER) - Reference til document.id
- Foreign keys + index på wordId for hurtig søgning

**Sådan inspiceres databasen:**
1. Åbn DB Browser for SQLite
2. Open Database → vælg `searchDB.db`
3. Klik på "Browse Data" tab
4. Vælg tabel for at se indhold:
   - `document`: Se alle 3034 indekserede dokumenter
   - `word`: Se alle 31079 forskellige ord 
   - `occ`: Se ord-dokument relationer (mange-til-mange)

============================================

## Opgave 2: Forbedret Statistik
**Status**: ⏳ Afventer opgave 1  
**Beskrivelse**: Ændre indexer output til at vise ord-hyppighed

### Krav:
- Informere om hvor mange ord forekomster der er indekseret
- Spørge brugeren hvor mange ord de vil se
- Vise ord rangeret efter hyppighed (hyppigste først)
- Output format: `<Message, 1> - 48342` (ord, id, hyppighed)

### Hvad Vi Har Gjort:
- [ ] Endnu ikke påbegyndt

---

## Opgave 3: Case Sensitivity Kontrol  
**Status**: ⏳ Afventer opgave 1  
**Beskrivelse**: Gør søgning case-sensitiv kontrollerbar

### Krav:
- Bruger kan skrive `/casesensitive=on` eller `/casesensitive=off`
- Alternativ: Config klasse med `CaseSensitive` boolean attribut

### Hvad Vi Har Gjort:
- [ ] Endnu ikke påbegyndt

---

## Opgave 4: Timestamp Visning
**Status**: ⏳ Afventer opgave 1  
**Beskrivelse**: Brugeren kan vælge om tidsstempel skal vises

### Krav:
- Kommandoer som `/timestamp=on` eller `/timestamp=off`
- Alternativ: `ViewTimeStamps` boolean i Config klasse

### Hvad Vi Har Gjort:
- [ ] Endnu ikke påbegyndt

---

## Opgave 5: Konfigurerbar Resultat Grænse
**Status**: ⏳ Afventer opgave 1  
**Beskrivelse**: Ændre fra faste 10 resultater til bruger-valgt antal

### Krav:
- Ændre fra 10 til 20 som standard
- Kommandoer som `/results=15` eller `/results=all`
- Alternativ: `int?` attribut i Config (null = alle resultater)

### Hvad Vi Har Gjort:
- [ ] Endnu ikke påbegyndt

---

## Opgave 6: Mønster-søgning (Avanceret)
**Status**: ⏳ Afventer opgave 1  
**Beskrivelse**: Wildcard/regulære udtryk søgning

### Krav:
- `?` = et vilkårligt tegn
- `*` = vilkårligt antal tegn (også ingen)
- Eksempel: `BJ????7` matcher 7-tegns ord startende med BJ, sluttende med 7
- Vis matchende termer i resultatet
- Første version: kun et mønster ad gangen (ikke kombineret med normale søgeord)

### Eksempel Output:
```
enter search terms - q for quit
BJ????7
Pattern Search
1: /path/to/document.txt -- contains 3 matching terms:
BJ12347, BJERGE7, BJ73857
```

### Hvad Vi Har Gjort:
- [ ] Endnu ikke påbegyndt

---

## Noter og Overvejelser

### Skole Projekt Begrænsninger:
- Vi må ikke nødvendigvis redigere alle filer i projektet
- Kan være begrænsninger i at oprette nye filer
- Simplicitet er med vilje - det er et uddannelsesprojekt

### Test Data:
- Small dataset: 13 emails (funktionel test)
- Medium dataset: ~5000 emails (funktionel + performance)  
- Large dataset: ~50000 emails (performance test)

### Vigtige Filer at Forstå:
- `indexer\App.cs` - Indeksering workflow
- `indexer\Crawler.cs` - Tekst udtrækning og ord parsing
- `ConsoleSearch\App.cs` - Søge interface
- `ConsoleSearch\SearchLogic.cs` - Søge algoritme
- `Shared\Paths.cs` - Konfiguration der skal opdateres