# Y-Scaling Implementation - Search Engine

## Opgave 3: Y-skalering af ConsoleSearch ✅
ConsoleSearch er nu opdelt i to komponenter:
- **SearchAPI** - Web API med søgelogikken (ekstraheret fra ConsoleSearch)
- **ConsoleSearch** - Thin console client der kalder API'et

## Opgave 4: Blazor Web App ✅
SearchWebApp er en Blazor Server app der bruger samme SearchAPI.

## Arkitektur Oversigt

```
SearchEngine.sln
├── SearchAPI/          [NY - Web API med søgelogik]
├── SearchWebApp/       [NY - Blazor web app]  
├── ConsoleSearch/      [MODIFICERET - Thin client]
├── indexer/           [UÆNDRET]
└── Shared/            [UÆNDRET]
```

## Sådan kører du systemet

### 1. Byg hele løsningen
```bash
dotnet build SearchEngine.sln
```

### 2. Start SearchAPI (skal køre først!)
```bash
cd SearchAPI
dotnet run
```
API'et kører på `http://localhost:5000`

### 3. Test med ConsoleSearch
Åbn ny terminal:
```bash
cd ConsoleSearch
dotnet run
```
Du ser nu "Console Search (API Mode)" og kan søge som før.

### 4. Test med Blazor Web App
Åbn ny terminal:
```bash
cd SearchWebApp
dotnet run
```
Gå til `http://localhost:5001` i browseren.

## API Endpoints

- `GET /api/search?query=energy&caseSensitive=false&limit=20&includeTimestamps=true`
- `GET /api/search/pattern?pattern=en*gy&caseSensitive=false&limit=20`

## Features implementeret

### Console App (samme funktionalitet som før)
- ✅ Case sensitivity toggle
- ✅ Timestamp display toggle  
- ✅ Result limit configuration
- ✅ Pattern search med wildcards
- ✅ Compact view
- ✅ Alle menu options virker

### Blazor Web App
- ✅ Søgefunktionalitet (normal + pattern)
- ✅ Alle search options (case sensitive, timestamps, result limit)
- ✅ Responsive design med Bootstrap
- ✅ Real-time loading indicators
- ✅ Error handling

## Y-Scaling Fordele Demonstreret

1. **Separation of Concerns**: Søgelogik er centraliseret i API'et
2. **Multiple Clients**: Console app og web app bruger samme backend
3. **Skalabilitet**: API kan servere flere clients samtidigt
4. **Maintainability**: Ændringer til søgelogik påvirker kun ét sted
5. **Technology Flexibility**: Console (.NET Core) og Web (Blazor) kan udvikles separat

## Test Scenarie

1. Start SearchAPI
2. Kør både ConsoleSearch og Blazor app samtidigt
3. Søg i begge - samme resultater vises
4. Demonstrer at begge clients bruger samme API backend

Dette viser succesfuld Y-axis scaling hvor UI og business logic er separeret i forskellige deployerbare enheder.