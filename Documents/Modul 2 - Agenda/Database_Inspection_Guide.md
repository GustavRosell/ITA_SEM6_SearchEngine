# Database Inspektion & Søgetest Vejledning

Denne vejledning viser hvordan du inspicerer SQLite databasen og grundigt tester søgefunktionaliteten.

## 📊 Database Inspektion med SQLite Browser

### 1. Åbn Databasen
1. Start **DB Browser for SQLite**
2. **File → Open Database**
3. Naviger til: `/Users/rosell/ITA_SEM6_SearchEngine/Data/searchDB.db`
4. Klik **Open**

### 2. Verificer at Alle Dokumenter er Indekseret

#### Tjek Dokument Antal
1. Klik **Browse Data** fanen
2. Vælg **`document`** tabellen fra dropdown
3. Verificer at du ser **3.034 rækker** (for medium datasæt)
4. Scroll gennem for at se forskellige dokument stier

#### Eksempel Dokument Indgange
Led efter indgange som:
```
id: 1, url: /Users/rosell/.../allen-p/all_documents/1.txt, idxTime: 8/27/2025 12:01:54 PM
id: 2, url: /Users/rosell/.../allen-p/all_documents/2.txt, idxTime: 8/27/2025 12:01:54 PM
```

### 3. Inspicér Indekserede Ord (Stikprøver)

#### Tjek Ord Antal
1. Vælg **`word`** tabellen fra dropdown  
2. Verificer at du ser **31.079 rækker** (for medium datasæt)
3. Browse gennem ordene for at se hvad der blev indekseret

#### Eksempel Ord Indgange
Led efter indgange som:
```
id: 1, name: Message
id: 2, name: ID  
id: 5, name: JavaMail
id: 6, name: evans
```

#### Avanceret Ord Analyse med SQL
Klik **Execute SQL** fanen og kør disse forespørgsler:

```sql
-- Tæl totale dokumenter
SELECT COUNT(*) as total_documents FROM document;

-- Tæl totale unikke ord
SELECT COUNT(*) as total_words FROM word;

-- Tæl totale ord forekomster
SELECT COUNT(*) as total_occurrences FROM occ;

-- Find hyppigste ord
SELECT w.name, COUNT(*) as frequency
FROM word w
JOIN occ o ON w.id = o.wordId
GROUP BY w.name
ORDER BY frequency DESC
LIMIT 20;

-- Find dokumenter der indeholder specifikt ord
SELECT d.url, d.idxTime 
FROM document d
JOIN occ o ON d.id = o.docId  
JOIN word w ON o.wordId = w.id
WHERE w.name = 'energy'
ORDER BY d.idxTime;

-- Tjek om specifikke ord eksisterer
SELECT name FROM word 
WHERE name IN ('power', 'energy', 'meeting', 'email')
ORDER BY name;

-- Tilfældige dokumenter
SELECT * FROM document 
ORDER BY RANDOM() 
LIMIT 10;
```

### 4. Verificer Indeks Integritet

Tjek **`occ`** (occurrence) tabellen:
```sql
-- Verificer ord-dokument relationer
SELECT COUNT(*) as total_relationships FROM occ;

-- Eksempel ord-dokument forbindelser
SELECT w.name as word, d.url as document
FROM occ o
JOIN word w ON o.wordId = w.id
JOIN document d ON o.docId = d.id
LIMIT 20;
```

## 🔍 Søgekonsol Test

### 1. Start Søgekonsol
```bash
cd /Users/rosell/ITA_SEM6_SearchEngine/ConsoleSearch
dotnet run
```

### 2. Test Enkelt Ord Forespørgsler (1 ord)

#### Forventede Hit:
```
energy
```
**Forventet:** Flere resultater der viser dokumenter indeholdende "energy"

```
power
```
**Forventet:** Flere resultater fra strøm-relaterede emails

```
meeting
```
**Forventet:** Møde-relaterede dokumenter

#### Forventede Ingen Hit:
```
unicorn
```
**Forventet:** "Ignored: unicorn" og "Documents: 0"

```
supercalifragilisticexpialidocious
```
**Forventet:** "Ignored: [ord]" og "Documents: 0"

### 3. Test To Ord Forespørgsler (2 ord)

#### Begge Ord Fundet:
```
power plant
```
**Forventet:** Dokumenter indeholdende både "power" OG "plant", rangeret efter relevans

```
energy meeting
```
**Forventet:** Dokumenter indeholdende begge ord

#### Et Ord Mangler:
```
power unicorn
```
**Forventet:** 
- "Ignored: unicorn"
- Resultater indeholdende kun "power"
- "Missing: [unicorn]" i resultat detaljer

### 4. Test Multiple Ord Forespørgsler (flere end 2 ord)

#### Alle Ord Fundet:
```
power energy meeting
```
**Forventet:** Dokumenter indeholdende alle tre termer (højeste score først)

#### Blandede Resultater:
```
power energy unicorn dragon
```
**Forventet:**
- "Ignored: unicorn, dragon"
- Resultater med "power" og "energy"
- "Missing:" felt viser hvilke termer der mangler fra hvert dokument

#### Alle Ord Mangler:
```
unicorn dragon phoenix
```
**Forventet:** "Ignored: unicorn, dragon, phoenix" og "Documents: 0"

### 5. Forventet Output Format

For hvert søgeresultat skal du se:
```
1 : /sti/til/dokument.txt -- contains X search terms
Index time: 8/27/2025 12:01:54 PM
Missing: [liste af manglende termer]
```

Plus sammendrag:
```
Documents: 159. Time: 76.5
```

## 📋 Verifikations Tjekliste

### Database Integritet ✓
- [ ] Document tabel har 3.034 indgange (medium datasæt)
- [ ] Word tabel har ~31.079 indgange  
- [ ] Occurrence tabel har mange relationer
- [ ] Filstier i document tabel er korrekte
- [ ] Indeks tidsstempler er nylige

### Søgefunktionalitet ✓
- [ ] Enkelt ord søgning returnerer resultater
- [ ] Enkelt ord søgning håndterer "ingen resultater" elegant
- [ ] To ord søgning rangerer efter relevans (2 match > 1 match)
- [ ] Multiple ord søgning viser "Missing:" termer korrekt
- [ ] Ikke-eksisterende ord viser "Ignored:" besked
- [ ] Forespørgselstid er rimelig (< 200ms for de fleste forespørgsler)
- [ ] Resultater er begrænset til top 10
- [ ] Resultater viser filstier, match antal og tidsstempler

## 🎯 Eksempel Test Session

```
Console Search
enter search terms - q for quit

energy
[Skal vise ~75 resultater]

power plant  
[Skal vise dokumenter med begge termer først]

meeting email schedule
[Skal vise dokumenter rangeret efter hvor mange termer de indeholder]

unicorn
Ignored: unicorn
Documents: 0. Time: 46.615

q
[Afslut]
```

## 💡 Tips

1. **Tilfældig Prøvetagning**: Brug SQL forespørgsler til at vælge tilfældige dokumenter og verificer de er søgbare
2. **Ord Verifikation**: Vælg ord fra word tabellen og verificer de returnerer søgeresultater  
3. **Performance**: Bemærk søgetider - de skal være under 100ms for simple forespørgsler
4. **Edge Cases**: Test tomme forespørgsler, meget lange forespørgsler og specielle tegn
5. **Konsistens**: Kør den samme forespørgsel flere gange for at verificere konsistente resultater

Dette fuldfører database inspektion og søgetest kravene fra Opgave 1.