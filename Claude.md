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
1. Recursively crawls configured directory for `.txt` files
2. Extracts and normalizes words using defined separators
3. Builds inverted index in SQLite database
4. Outputs indexing statistics

### 2. Search Engine (`ConsoleSearch` project)
**Entry Point**: `Program.cs` → `App.cs`
- **Search Logic**: `SearchLogic.cs` - implements search algorithm
- **Database**: `DatabaseSqlite.cs` - read-only database access
- **Models**: `SearchResult.cs`, `DocumentHit.cs`

**Search Workflow**:
1. Accepts multi-word queries via console
2. Maps query terms to word IDs in database
3. Finds intersecting documents using inverted index
4. Calculates relevance scores and ranks results
5. Returns top 10 results with metadata

**Scoring Algorithm**: 
```
score = (number_of_matching_terms / total_query_terms)
```

### 3. Shared Library (`Shared` project)
- **`BEDocument.cs`**: Document business entity model
- **`Paths.cs`**: Platform-specific database path configuration
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
# Run indexer (crawls and indexes documents)
dotnet run --project indexer

# Run search console (interactive search)
dotnet run --project ConsoleSearch
```

### Configuration Setup
Before running, update these paths in:
- `Shared/Paths.cs` - Database path (platform-specific)
- `indexer/Config.cs` - Folder to index

### Database Inspection
Use SQLite browser to inspect `Data/searchDB.db` after indexing.

## Current Configuration
⚠️ **Paths are configured for current macOS environment**:
- Database: `/Users/rosell/ITA_SEM6_SearchEngine/Data/searchDB.db`
- Index folder: `C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\seData copy\medium`

## Available Assignments (from Danish docs)

### Assignment 1: Setup
- Install .NET 9.0
- Install SQLite browser
- Build all projects, update NuGet packages
- Update paths in `Shared.Paths` and `Indexer.Config`
- Run indexer, inspect database
- Test search console with 1, 2, and multiple word queries

### Assignment 2: Enhanced Statistics
- Modify indexer output to show:
  - Total word occurrences indexed
  - User-specified number of most frequent words
  - Words ranked by frequency (most frequent first)

### Assignment 3: Case Sensitivity Control
- Add user commands like `/casesensitive=on/off`
- Alternative: Add `CaseSensitive` boolean to Config class

### Assignment 4: Timestamp Display Control
- Add user commands like `/timestamp=on/off`
- Alternative: Add `ViewTimeStamps` boolean to Config class

### Assignment 5: Result Limit Configuration
- Change from fixed 10 results to user-configurable
- Add commands like `/results=15` or `/results=all`
- Alternative: Add `int?` result limit to Config class

### Assignment 6: Pattern Matching (Advanced)
- Implement wildcard search with `?` (single char) and `*` (multiple chars)
- Example: `BJ????7` matches 7-char words starting with BJ, ending with 7
- Show matching terms in results

## Current State Assessment

### ✅ Working Components
- Basic indexing and search functionality
- Clean three-project architecture
- Realistic test data (Enron dataset)
- Proper inverted index implementation
- Score-based ranking

### ⚠️ Areas Needing Work
- Path configuration for Windows environment
- No snippets in search results yet
- No synonym dictionary support
- No case sensitivity control
- Fixed 10-result limit
- No wildcard/pattern search

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

**Important**: When using `/init` command in Claude Code CLI, always read both `Claude.md` and `assignments.md` for complete project context.

## Getting Started Checklist (Assignment 1)
1. Verify .NET 9.0 installation
2. Update `Shared\Paths.cs` database path for Windows
3. Update `indexer\Config.cs` folder path to point to test data
4. Install SQLite browser for database inspection
5. Build solution and run indexer
6. Test search functionality with various queries
7. Choose assignment(s) to implement based on learning objectives

## Architecture Notes

### Key Files
- `indexer/App.cs` - Indexing main workflow at indexer:32
- `indexer/Crawler.cs` - Text extraction and word parsing logic
- `ConsoleSearch/App.cs` - Interactive search console interface
- `ConsoleSearch/SearchLogic.cs` - Search algorithm and ranking at ConsoleSearch:45
- `Shared/Paths.cs` - Cross-platform database path configuration at Shared:7

### Search Algorithm Details
The system implements a basic TF (term frequency) scoring model where each document's relevance score is calculated as the percentage of query terms found within it. The inverted index allows efficient lookup of documents containing specific terms, with results ranked by descending relevance score and limited to top 10.

### Configuration Dependencies
Both applications depend on proper path configuration in `Shared/Paths.cs` and indexer folder setting in `indexer/Config.cs`. The path logic uses `RuntimeInformation` to detect the current platform and select appropriate file system paths.