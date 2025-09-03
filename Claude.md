# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# SearchEngine PoC - Project Analysis

## Project Overview
This is a **Proof of Concept (PoC) Search Engine** for IT-Architecture semester 6 at Erhvervsakademiet Aarhus. The system is designed as an internal document search solution for organizations with 50+ employees.

**Important Note**: This is a school project with restrictions on file modifications and creation. Not all files may be editable, and the simplicity is intentional for educational purposes.

## Architecture & Components

### Solution Structure
- **`indexer`** - Console application that crawls and indexes documents
- **`ConsoleSearch`** - Console application providing search functionality  
- **`Shared`** - Class library containing common models and configuration

### Technology Stack
- **.NET 9.0** C# console applications
- **SQLite database** for inverted index storage
- **Microsoft.Data.Sqlite** NuGet package (version 8.0.1)

### Database Schema (Inverted Index)
SQLite database with three main tables:
```sql
Document: docId, title, link, date
Word: termId, value  
Occurrence: docId, termId (many-to-many relationship)
```

## Core Components Analysis

### 1. Indexer (`indexer` project)
**Entry Point**: `Program.cs` → `App.cs`
- **Configuration**: `Config.cs` - defines folder to index
- **Crawler**: `Crawler.cs` - main indexing logic
- **Database**: `DatabaseSqlite.cs` - handles database operations

**Key Implementation Details**:
- Only indexes `.txt` files recursively
- Word extraction separators: `" \\\n\t\"$'!,?;.:-_**+=)([]{}<>/@&%€#"`
- Creates inverted index: word → documents containing it
- Platform-specific database paths via `RuntimeInformation`

**Indexing Workflow**:
1. Prompts user to select dataset size (small/medium/large)
2. Recursively crawls configured directory for `.txt` files
3. Extracts and normalizes words using defined separators
4. Builds inverted index in SQLite database
5. Outputs comprehensive statistics including:
   - Total documents indexed
   - Total word occurrences
   - Top N most frequent words (user-configurable)

### 2. Search Engine (`ConsoleSearch` project)
**Entry Point**: `Program.cs` → `App.cs`
- **Search Logic**: `SearchLogic.cs` - implements search algorithm
- **Database**: `DatabaseSqlite.cs` - read-only database access
- **Models**: `SearchResult.cs`, `DocumentHit.cs`
- **Config**: `Config.cs` - feature toggles (case sensitivity, timestamps, result limits, pattern search, compact view)

**Search Workflow**:
1. Displays interactive menu with feature toggles
2. Accepts queries (normal search or pattern matching)
3. Maps query terms to word IDs in database
4. Finds intersecting documents using inverted index
5. Calculates relevance scores and ranks results
6. Returns configurable number of results (default 20) with metadata

**Scoring Algorithm**: 
```
score = (number_of_matching_terms / total_query_terms)
```

### 3. Shared Library (`Shared` project)
- **`BEDocument.cs`**: Document business entity model
- **`Paths.cs`**: Cross-platform database path configuration (auto-detects Windows/macOS/Linux)
- **`IDatabase.cs`**: Database interface (used by both indexer and search)

## Test Data Structure

### Enron Email Dataset
Located in: `C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\seData copy\`

**Three Dataset Sizes**:
- **small**: 13 emails in 1 folder (functional testing)
- **medium**: ~5,000 emails in ~20 mailboxes (functional + performance)
- **large**: ~50,000 emails for 15 users (performance testing)

**Data Format**: Email files with headers and content
```
Message-ID: <...>
Date: Wed, 13 Dec 2000 18:41:00 -0800 (PST)
From: sender@domain.com
To: recipient@domain.com
Subject: Email Subject
...email content...
```

## Requirements (from Danish Documentation)

### Business Context
- Organization with 50+ employees producing documents
- Documents in multiple formats (docx, pdf, rtf, txt, csv, pptx, xlsx)
- Multi-terabyte collections across file servers
- Need for "instant content search"
- Domain-specific customization capability

### Technical Requirements
- **100% recall**: All documents containing any search terms must appear
- **Ranking**: Results ordered by descending score
- **Scoring**: Percentage of query terms found in document (0.0 to 1.0)
- **Future features**: Synonym dictionary for domain-specific terms

### User Interface Requirements
- Google-like search interface
- Results showing: title, link, date, score, snippet
- Can be web application (intranet) or desktop application

## Essential Commands

### Build Solution
```bash
dotnet build SearchEngine.sln
```

### Run Projects
```bash
# Run indexer with dataset selection (crawls and indexes documents)
cd indexer
dotnet run small     # Index small dataset (13 emails)
dotnet run medium    # Index medium dataset (~5,000 emails) 
dotnet run large     # Index large dataset (~50,000 emails)
# Alternative: dotnet run (will prompt for dataset selection)

# Run search console (interactive search)
cd ConsoleSearch
dotnet run
```

### Restore Packages
```bash
dotnet restore
```

### Configuration Setup
**No manual configuration needed!** The system automatically detects your platform (Windows/macOS/Linux) and uses appropriate paths:
- `Shared/Paths.cs` - Database path (auto-detects platform)
- `indexer/Config.cs` - Dataset folders (auto-detects platform)

### Database Inspection
Use SQLite browser to inspect `Data/searchDB.db` after indexing.

### Manual Testing Approach
**No formal test framework** - this project uses manual testing with realistic datasets:
- **Small dataset**: 13 emails for functional verification
- **Medium dataset**: ~5,000 emails for functional + basic performance testing  
- **Large dataset**: ~50,000 emails for performance testing
- Test search functionality with 1-word, 2-word, and multi-word queries
- Verify interactive menu options and configuration toggles

### Available Commands in Search Console
- **Menu Options**: `1`, `2`, `3`, `4`, `5` - Toggle various settings
- **Help**: `?` - Display comprehensive help with current settings and examples
- **Quit**: `q` - Exit the application
- **Slash Commands**: 
  - `/casesensitive=on|off` - Toggle case sensitivity
  - `/timestamp=on|off` - Toggle timestamp display
  - `/results=NUMBER|all` - Set result limit
  - `/pattern=on|off` - Toggle pattern search mode
  - `/compact=on|off` - Toggle compact view display

## Cross-Platform Configuration
✅ **Automatic platform detection implemented** via `RuntimeInformation.IsOSPlatform()`:
- **Database paths**: Automatically selected based on detected OS
- **Dataset folders**: Platform-specific paths configured in `indexer/Config.cs`
- **No manual path changes needed** when switching between systems
- See `Shared/Paths.cs` and `indexer/Config.cs` for implementation details

## Available Assignments (from Danish docs)

### Assignment 1: Setup
- Install .NET 9.0
- Install SQLite browser
- Build all projects, update NuGet packages
- Update paths in `Shared.Paths` and `Indexer.Config`
- Run indexer, inspect database
- Test search console with 1, 2, and multiple word queries

### Assignment 2: Enhanced Statistics ✅ **COMPLETED**
- ✅ Shows total word occurrences indexed
- ✅ Prompts user for number of most frequent words to display
- ✅ Displays words ranked by frequency (most frequent first)
- ✅ Format: `<word, id> - frequency`

### Assignment 3: Case Sensitivity Control ✅ **COMPLETED**
- ✅ Interactive menu option "1" to toggle case sensitivity
- ✅ `Config.CaseSensitive` boolean controls behavior
- ✅ Default: case-sensitive search enabled

### Assignment 4: Timestamp Display Control ✅ **COMPLETED**
- ✅ Interactive menu option "2" to toggle timestamp display
- ✅ `Config.ViewTimeStamps` boolean controls display
- ✅ Default: timestamps shown in results

### Assignment 5: Result Limit Configuration ✅ **COMPLETED**
- ✅ Interactive menu option "3" to configure result limits
- ✅ Supports specific numbers (e.g., 15) or "all" for unlimited
- ✅ `Config.ResultLimit` int? property (default: 20)
- ✅ Changed from fixed 10 to configurable system

### Assignment 6: Pattern Matching ✅ **COMPLETED**
- ✅ Interactive menu option "4" to enable pattern search mode
- ✅ Supports `?` (single char) and `*` (multiple chars) wildcards
- ✅ `Config.PatternSearch` boolean toggle
- ✅ Shows matching terms in results

### Additional Enhancements ✅ **IMPLEMENTED**
- ✅ **Compact View** - Option "5" for clean, single-line result display
- ✅ **Help System** - Type `?` for comprehensive contextual help
- ✅ **Enhanced Commands** - Additional slash commands (`/compact=on|off`)

## Current State Assessment

### ✅ Working Components
- Basic indexing and search functionality
- Clean three-project architecture
- Realistic test data (Enron dataset)
- Proper inverted index implementation
- Score-based ranking
- Cross-platform support (Windows/macOS/Linux)
- Enhanced statistics with word frequency
- Case sensitivity control (`Config.CaseSensitive`)
- Configurable result limits (`Config.ResultLimit`)
- Timestamp display control (`Config.ViewTimeStamps`)
- Pattern matching with wildcards (`Config.PatternSearch`)
- Compact view for clean result display (`Config.CompactView`)
- Interactive menu system for feature toggles
- Comprehensive help system with contextual examples (accessible via `?`)

### ⚠️ Areas for Future Enhancement
- No snippets in search results yet
- No synonym dictionary support
- Could expand wildcard patterns beyond single/multiple character matching

### 🚫 School Project Constraints
- Limited ability to edit certain files
- Cannot create arbitrary new files
- Simplicity is intentional for educational purposes
- Focus on extending existing functionality vs. major architectural changes

## Assignment Progress Tracking

**📋 See `assignments.md` for detailed progress tracking in Danish**

The `assignments.md` file contains:
- All 6 assignments from the Danish documentation
- Current status and progress for each assignment
- Detailed steps and requirements
- What has been completed vs. what remains

**Note**: All core assignments (1-6) have been implemented, plus additional user experience enhancements. The system now includes enhanced statistics, configurable search options, pattern matching capabilities, compact view display, and a comprehensive help system. See `assignments.md` for detailed implementation status.

## Academic Course Context

### Current Module: IT-Architecture Semester 6
- **Module 2**: Search Engine PoC (completed assignments 1-6)
- **Module 3**: AKF Scale Cube architecture patterns
- Course materials located in `Documents/Modul X - Agenda/` folders
- Assignment responses and analysis files created as course progresses

### Course Materials Structure
- **Reading materials**: `Læselektier.txt` and `Læselektier.pdf` files
- **Assignment responses**: `Opgaver_ModulX.txt` files with Danish answers
- **Database guides**: Technical documentation for SQLite inspection

## Getting Started Checklist
1. Verify .NET 9.0 installation
2. ✅ Cross-platform paths already configured automatically
3. Install SQLite browser for database inspection
4. Build solution: `dotnet build SearchEngine.sln`
5. Run indexer with dataset selection: `cd indexer && dotnet run medium`
6. Test search functionality: `cd ConsoleSearch && dotnet run`
7. Explore interactive menu features (case sensitivity, timestamps, result limits, pattern search, compact view)
8. Try the help system by typing `?` to see all available options and examples

## Architecture Notes

### Key Files
- `indexer/App.cs` - Indexing main workflow at indexer:32
- `indexer/Crawler.cs` - Text extraction and word parsing logic
- `ConsoleSearch/App.cs` - Interactive search console interface
- `ConsoleSearch/SearchLogic.cs` - Search algorithm and ranking at ConsoleSearch:45
- `Shared/Paths.cs` - Cross-platform database path configuration at Shared:7

### Search Algorithm Details
The system implements a basic TF (term frequency) scoring model where each document's relevance score is calculated as the percentage of query terms found within it. The inverted index allows efficient lookup of documents containing specific terms, with results ranked by descending relevance score and limited by the configurable result limit (default: 20).

### Configuration Dependencies
Both applications use automatic cross-platform configuration:
- `Shared/Paths.cs` uses `RuntimeInformation` to detect platform and select appropriate database paths
- `indexer/Config.cs` uses `RuntimeInformation` to select appropriate dataset folder paths
- `ConsoleSearch/Config.cs` provides feature toggles for search behavior:
  - `CaseSensitive` - Case-sensitive search matching
  - `ViewTimeStamps` - Display document indexing timestamps  
  - `ResultLimit` - Maximum results to display (int? - null = unlimited)
  - `PatternSearch` - Wildcard pattern matching mode
  - `CompactView` - Clean single-line result display format

### Interactive Features
The search console (`ConsoleSearch/App.cs`) provides an interactive menu system allowing users to toggle:
1. **Case Sensitivity** - Enable/disable case-sensitive search
2. **Timestamp Display** - Show/hide document timestamps in results
3. **Result Limits** - Configure number of results (default 20, or "all")
4. **Pattern Search** - Enable wildcard matching with `?` (single char) and `*` (multiple chars)
5. **Compact View** - Clean display format removing long file paths, showing results as single lines

**Enhanced Help System**: Users can type `?` (or option `6`) to access comprehensive contextual help showing current settings, examples, and available commands.