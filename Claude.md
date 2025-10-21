# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# SearchEngine PoC - Project Analysis

## Project Overview
This is a **Proof of Concept (PoC) Search Engine** for IT-Architecture semester 6 at Erhvervsakademiet Aarhus. The system is designed as an internal document search solution for organizations with 50+ employees.

**Important Note**: This is a school project with restrictions on file modifications and creation. Not all files may be editable, and the simplicity is intentional for educational purposes.

**Documentation**: See `/documentation/` folder for detailed module-by-module implementation history and `documentation/quick-start.md` for deployment commands.

## Architecture & Components

### Solution Structure (AKF Scale Cube Architecture)
- **`indexer`** - Console application that crawls and indexes documents (now with partition support)
- **`ConsoleSearch`** - Console application providing interactive search interface
- **`SearchAPI`** - ASP.NET Core Web API providing RESTful search endpoints (Y-Scale component, X-Scale + Z-Scale ready)
- **`Coordinator`** - ASP.NET Core Web API aggregating results from data partitions (Z-Scale component) ✅ **NEW**
- **`SearchWebApp`** - Blazor Server web application with Claude.ai-inspired UI
- **`Shared`** - Class library containing common models and configuration
- **`nginx/`** - Load balancer configurations (round-robin + sticky sessions)
- **`scripts/`** - Automated deployment scripts (X-Scale + Z-Scale)
- **`documentation/`** - Module-by-module implementation documentation

**Scaling Dimensions Implemented:**
- ✅ **Y-Axis (Functional Decomposition)**: Separate API service for search logic (Module 3)
- ✅ **X-Axis (Horizontal Duplication)**: Multiple identical API instances with nginx load balancing (Module 6)
- ✅ **Z-Axis (Data Partitioning)**: Coordinator pattern with multiple database partitions (Module 7)

### Technology Stack
- **.NET 9.0** C# applications (console, API, and web)
- **ASP.NET Core Web API** for search service layer
- **Blazor Server** for modern web UI
- **SQLite database** for inverted index storage
- **nginx** for load balancing and horizontal scaling ✅ **NEW**
- **Microsoft.Data.Sqlite** NuGet package (version 8.0.1)
- **Microsoft.AspNetCore.OpenApi** for API documentation

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
- **Z-Scale Support (Module 7)**: Accepts partition parameters for distributed indexing

**Indexing Workflow**:
1. Prompts user to select dataset size (small/medium/large) OR accepts via command-line
2. **Optional**: Accept partition parameters (`<dataset> <partition_number> <total_partitions>`)
3. Recursively crawls configured directory for `.txt` files
4. **Z-Scale**: If partitioning, distributes subdirectories using modulo algorithm
5. Extracts and normalizes words using defined separators
6. Builds inverted index in SQLite database (single or partitioned)
7. Outputs comprehensive statistics including:
   - Total documents indexed
   - Total word occurrences
   - Top N most frequent words (user-configurable)

**Usage Examples**:
```bash
# Standard indexing (all documents to searchDB.db)
dotnet run medium

# Partitioned indexing (Z-Scale)
dotnet run medium 1 3  # Partition 1 of 3 → searchDB1.db
dotnet run medium 2 3  # Partition 2 of 3 → searchDB2.db
dotnet run medium 3 3  # Partition 3 of 3 → searchDB3.db
```

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

### 3. Search API (`SearchAPI` project) ✅ **NEW - Y-SCALE COMPONENT**
**Entry Point**: `Program.cs` → ASP.NET Core Web API
- **Search Controller**: `Controllers/SearchController.cs` - RESTful API endpoints
- **Search Logic**: `SearchLogic.cs` - Core search algorithms (moved from ConsoleSearch)
- **Database**: `Data/DatabaseSqlite.cs` - Database access layer
- **Models**: Pattern and document hit models for API responses

**API Endpoints**:
- `GET /api/search` - Standard search with query parameters
  - Parameters: `query`, `caseSensitive`, `limit`, `includeTimestamps`
- `GET /api/search/pattern` - Pattern search with wildcards (? and *)
  - Parameters: `pattern`, `caseSensitive`, `limit`

**API Features**:
- RESTful design with proper HTTP status codes
- JSON response format with metadata (timing, hit counts, truncation status)
- Error handling with detailed error messages
- OpenAPI/Swagger documentation support

### 4. Web Application (`SearchWebApp` project) ✅ **NEW - BLAZOR UI**
**Entry Point**: `Program.cs` → Blazor Server Application
- **Main Page**: `Pages/Search.razor` - Claude.ai-inspired search interface
- **Layout**: `Shared/MainLayout.razor` - App structure with collapsible sidebar
- **Styling**: `wwwroot/css/claude-theme.css` - Dark theme with orange accents

**UI Features**:
- **Claude.ai-inspired design**: Dark background (`rgb(12, 12, 12)`) with orange accents (`rgb(234, 88, 12)`)
- **Collapsible sidebar**: Starts closed like Claude.ai interface
- **Search interface**: Prominent search bar with real-time API integration
- **Filter toggles**: Case sensitivity, pattern search, compact view, timestamps
- **Configurable results**: 20/50/100/150/200 or custom limit
- **Expandable results**: Compact view with click-to-expand functionality
- **Responsive design**: Mobile-friendly layout

**API Integration**:
- Consumes SearchAPI endpoints at `localhost:5137` (default API port)
- Real-time search with loading states
- Error handling with user-friendly messages
- Proper HTTP client configuration

### 5. Coordinator Service (`Coordinator` project) ✅ **NEW - Z-SCALE COMPONENT (Module 7)**
**Entry Point**: `Program.cs` → ASP.NET Core Web API
- **Coordinator Service**: `Services/CoordinatorService.cs` - Parallel query and result aggregation logic
- **Coordinator Controller**: `Controllers/CoordinatorController.cs` - API endpoints for distributed search
- **Configuration**: `appsettings.json` - SearchAPI instances URLs

**Coordinator Pattern (Z-Scale Data Partitioning)**:
```
Client → Coordinator (5153)
            ↓
   ┌────────┼────────┐
   ↓        ↓        ↓
API-1    API-2    API-3
(DB1)    (DB2)    (DB3)
```

**Key Features**:
- **Parallel queries**: Sends simultaneous requests to all SearchAPI instances
- **Result merging**: Combines DocumentHits from all data partitions
- **Relevance sorting**: Re-sorts merged results by NoOfHits descending
- **Graceful degradation**: Continues if one partition fails
- **Timing aggregation**: Sums response times from all partitions

**API Endpoints**:
- `GET /api/search?query={query}&caseSensitive={bool}&limit={int}` - Standard search across all partitions
- `GET /api/search/pattern?pattern={pattern}&caseSensitive={bool}&limit={int}` - Pattern search across all partitions
- `GET /api/search/ping` - Health check returning "Coordinator"

**Configuration** (`appsettings.json`):
```json
{
  "SearchAPISettings": {
    "Instances": [
      "http://localhost:5137",
      "http://localhost:5138",
      "http://localhost:5139"
    ]
  }
}
```

**Workflow**:
1. Receive search request from client
2. Create parallel HTTP GET requests to ALL configured SearchAPI instances
3. Wait for all responses using `Task.WhenAll`
4. Merge all `DocumentHits` lists
5. Sort combined results by relevance score
6. Return unified `SearchResult` with aggregated metadata

**Z-Scale Benefits**:
- ✅ Horizontal data scalability - add more partitions as dataset grows
- ✅ Parallel processing - all partitions searched simultaneously
- ✅ Complete results - aggregates data from all partitions
- ✅ Independent scaling - each partition can scale independently

### 6. Shared Library (`Shared` project)
- **`BEDocument.cs`**: Document business entity model
- **`Paths.cs`**: Cross-platform database path configuration (auto-detects Windows/macOS/Linux)
- **`IDatabase.cs`**: Database interface (used by indexer, ConsoleSearch, and SearchAPI)

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

#### 1. Index Documents (Required First Step)

**Standard Indexing (Single Database):**
```bash
# Run indexer with dataset selection (crawls and indexes documents)
cd indexer
dotnet run small     # Index small dataset (13 emails)
dotnet run medium    # Index medium dataset (~5,000 emails)
dotnet run large     # Index large dataset (~50,000 emails)
# Alternative: dotnet run (will prompt for dataset selection)
```

**Partitioned Indexing (Z-Scale - Module 7):** ✅ **NEW**
```bash
# Automated partition creation using script (recommended)
./scripts/partition-dataset.sh medium 3
# Creates: searchDB1.db, searchDB2.db, searchDB3.db

# Manual partitioning (advanced)
cd indexer
dotnet run medium 1 3  # Partition 1 of 3
dotnet run medium 2 3  # Partition 2 of 3
dotnet run medium 3 3  # Partition 3 of 3
```

#### 2. Search Applications (Choose One or Both)

**Console Search (Original)**
```bash
# Run interactive console search
cd ConsoleSearch
dotnet run
```

**API Service (Y-Scale Component)** ✅ **NEW**
```bash
# Run REST API service (required for web app)
cd SearchAPI
dotnet run
# API will be available at: http://localhost:5137
# OpenAPI documentation at: http://localhost:5137/openapi/v1.json
```

**Web Application (Blazor UI)** ✅ **NEW**
```bash
# Run Blazor web application (requires API to be running)
cd SearchWebApp
dotnet run
# Web app will be available at: http://localhost:5000 or https://localhost:5001
```

#### 3. Multi-Application Startup
```bash
# For full web experience, run both API and Web App simultaneously:
# Terminal 1: Start the API
cd SearchAPI && dotnet run

# Terminal 2: Start the Web App (in separate terminal)
cd SearchWebApp && dotnet run
```

#### 4. X-Scale Load Balancing (AKF Scale Cube - Horizontal Scaling) ✅ **NEW**
Demonstrates horizontal scaling by running multiple identical API instances behind an nginx load balancer.

**Prerequisites:**
```bash
# Install nginx (macOS)
brew install nginx
```

**Quick Start with Scripts:**
```bash
# Step 1: Start 3 SearchAPI instances (ports 5137, 5138, 5139)
scripts/start-api-instances.sh

# Step 2: Start nginx load balancer (port 8080)
# Option A: Round-robin (requests distributed evenly)
scripts/start-nginx.sh

# Option B: Sticky sessions (same client → same instance)
scripts/start-nginx-sticky.sh

# Step 3: Run clients - they automatically connect to load balancer
# Console Search
cd ConsoleSearch && dotnet run

# Web App (in separate terminal)
cd SearchWebApp && dotnet run

# Step 4: Stop all services when done
scripts/stop-all.sh
```

**Manual Startup (Alternative):**
```bash
# Terminal 1: API Instance 1 (API-1)
cd SearchAPI && dotnet run --launch-profile http

# Terminal 2: API Instance 2 (API-2)
cd SearchAPI && dotnet run --launch-profile http2

# Terminal 3: API Instance 3 (API-3)
cd SearchAPI && dotnet run --launch-profile http3

# Terminal 4: nginx load balancer
# Round-robin:
nginx -c $(pwd)/nginx/nginx.conf -p $(pwd)/nginx
# OR Sticky sessions:
nginx -c $(pwd)/nginx/nginx-sticky.conf -p $(pwd)/nginx

# Terminal 5: Web App or Console Search
cd SearchWebApp && dotnet run
# OR
cd ConsoleSearch && dotnet run
```

**Verifying Load Balancing:**
- **Web App**: Watch the orange watermark in top-right corner showing "Instance: API-1/2/3"
- **Console Search**: Each search displays `[Instance: API-X]` in the output
- **API Direct Test**: `curl http://localhost:8080/api/search?query=test` - run multiple times to see different instanceId values
- **nginx Health Check**: `curl http://localhost:8080/health`

**Load Balancing Strategies:**
1. **Round-robin** (`nginx/nginx.conf`): Distributes requests evenly across all healthy backends
2. **Sticky sessions** (`nginx/nginx-sticky.conf`): Same client IP always routed to same backend (ip_hash)

**Three Deployment Modes:**
1. **Single Instance** (Development):
   ```bash
   # Set environment variable for single instance mode
   export API_BASE_URL=http://localhost:5137  # ConsoleSearch
   # OR edit appsettings.json: "ApiSettings:BaseUrl": "http://localhost:5137"  # SearchWebApp
   cd SearchAPI && dotnet run
   ```

2. **X-Scale Round-Robin** (Production-like):
   ```bash
   scripts/start-api-instances.sh
   scripts/start-nginx.sh  # Round-robin
   # Clients use default: http://localhost:8080
   ```

3. **X-Scale Sticky Sessions** (Session Affinity):
   ```bash
   scripts/start-api-instances.sh
   scripts/start-nginx-sticky.sh  # Sticky sessions
   # Same client always hits same instance
   ```

**Architecture Benefits:**
- ✅ **Horizontal scalability**: Add more instances to handle increased load
- ✅ **High availability**: If one instance fails, others continue serving requests
- ✅ **Zero-downtime deployment**: Update instances one at a time
- ✅ **Real-world pattern**: Industry-standard load balancing with nginx
- ✅ **Flexible configuration**: Easy switch between single/multi-instance modes

#### 5. Z-Scale Data Partitioning (AKF Scale Cube - Data Sharding) ✅ **NEW**
Demonstrates data partitioning by distributing documents across multiple databases and aggregating results through a Coordinator service.

**Prerequisites:**
```bash
# Ensure all projects are built
dotnet build SearchEngine.sln
```

**Quick Start with Scripts:**
```bash
# Step 1: Partition the dataset across 3 databases
cd indexer
scripts/partition-dataset.sh small 3
# OR scripts/partition-dataset.sh medium 3
# OR scripts/partition-dataset.sh large 3

# Step 2: Start all Z-Scale services (3 SearchAPI instances + Coordinator)
scripts/start-z-scale.sh

# Step 3: Test Coordinator aggregation
# The Coordinator runs on port 5050 and queries all 3 partitions
curl "http://localhost:5050/api/coordinator?query=test&limit=20"

# Step 4: Run clients (they can connect to Coordinator or individual APIs)
# Console Search (connects to Coordinator by setting API_BASE_URL)
export API_BASE_URL=http://localhost:5050/api/coordinator
cd ConsoleSearch && dotnet run

# Web App (update appsettings.json to point to Coordinator)
cd SearchWebApp && dotnet run

# Step 5: Stop all services when done
scripts/stop-z-scale.sh
```

**Manual Startup (Alternative):**
```bash
# Step 1: Partition dataset manually (creates searchDB1.db, searchDB2.db, searchDB3.db)
cd indexer
dotnet run small 1 3  # Partition 1 of 3
dotnet run small 2 3  # Partition 2 of 3
dotnet run small 3 3  # Partition 3 of 3

# Step 2: Start SearchAPI instances with different database partitions
# Terminal 1: API Instance 1 (searchDB1.db, port 5137)
cd SearchAPI && dotnet run --launch-profile http

# Terminal 2: API Instance 2 (searchDB2.db, port 5138)
cd SearchAPI && dotnet run --launch-profile http2

# Terminal 3: API Instance 3 (searchDB3.db, port 5139)
cd SearchAPI && dotnet run --launch-profile http3

# Terminal 4: Coordinator (aggregates results from all 3 APIs)
cd Coordinator && dotnet run
# Coordinator will be available at: http://localhost:5050

# Terminal 5: Test aggregation
curl "http://localhost:5050/api/coordinator?query=energy&limit=20"
```

**Verifying Z-Scale Data Partitioning:**
- **Database Verification**: Check `Data/` folder for `searchDB1.db`, `searchDB2.db`, `searchDB3.db`
- **Partition Distribution**: Each database should contain different subdirectories (modulo-based distribution)
- **Coordinator Aggregation**:
  ```bash
  # Test that Coordinator queries all 3 partitions
  curl "http://localhost:5050/api/coordinator?query=test"
  # Response should aggregate results from all 3 databases
  ```
- **Direct API Testing**: Query individual partitions to verify data distribution
  ```bash
  curl "http://localhost:5137/api/search?query=test"  # Partition 1
  curl "http://localhost:5138/api/search?query=test"  # Partition 2
  curl "http://localhost:5139/api/search?query=test"  # Partition 3
  ```

**Z-Scale Architecture:**
```
User Request
     ↓
Coordinator (port 5050)
     ↓ (parallel queries)
├─→ SearchAPI-1 (port 5137) → searchDB1.db (subdirs: 0, 3, 6, 9, ...)
├─→ SearchAPI-2 (port 5138) → searchDB2.db (subdirs: 1, 4, 7, 10, ...)
└─→ SearchAPI-3 (port 5139) → searchDB3.db (subdirs: 2, 5, 8, 11, ...)
     ↓
Merge & Sort Results
     ↓
Return Aggregated Results
```

**Deployment Modes:**
1. **Single Database** (Development):
   ```bash
   # Traditional single database approach
   cd indexer && dotnet run small
   cd SearchAPI && dotnet run
   ```

2. **Z-Scale Partitioned** (Production-like):
   ```bash
   # Distributed data across 3 partitions with Coordinator
   scripts/partition-dataset.sh small 3
   scripts/start-z-scale.sh
   # Clients use Coordinator: http://localhost:5050/api/coordinator
   ```

3. **Combined X+Z Scale** (Full AKF Cube):
   ```bash
   # Data partitioning + horizontal scaling + load balancing
   scripts/partition-dataset.sh medium 3
   scripts/start-z-scale.sh  # Starts 3 SearchAPI instances + Coordinator
   scripts/start-nginx.sh     # Load balancer in front of Coordinator
   # Maximum scalability: Data sharded, horizontally scaled, load balanced
   ```

**Architecture Benefits:**
- ✅ **Data scalability**: Distribute large datasets across multiple databases
- ✅ **Parallel query execution**: Query all partitions simultaneously using Task.WhenAll
- ✅ **Partition tolerance**: If one partition fails, others still return results
- ✅ **Flexible partitioning**: Easy to add more partitions (just update total count)
- ✅ **Complete AKF implementation**: Y-Scale (API service) + X-Scale (horizontal) + Z-Scale (data sharding)
- ✅ **Future-ready**: Prepares for Kubernetes orchestration (Module 8)

---

## Three Deployment Modes

The system supports three distinct deployment configurations. See `documentation/quick-start.md` for exact commands.

### 1. Single Instance (Development Mode)
**Use Case**: Local development, debugging, testing
**Setup**: Single SearchAPI instance, direct client connection
**Configuration**: Set `API_BASE_URL=http://localhost:5137` environment variable

### 2. Round-Robin Load Balancing (Production-like)
**Use Case**: Even load distribution, high availability
**Setup**: 3 API instances + nginx round-robin load balancer
**Behavior**: Each request goes to next instance in rotation
**Verification**: Watermark changes between API-1/2/3

### 3. Sticky Sessions (Session Affinity)
**Use Case**: Session-dependent workloads, cache warming
**Setup**: 3 API instances + nginx with ip_hash
**Behavior**: Same client IP always routes to same instance
**Verification**: Watermark stays on same instance

**Quick Start**: Run `scripts/start-api-instances.sh` then either `scripts/start-nginx.sh` (round-robin) or `scripts/start-nginx-sticky.sh` (sticky)

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

**Database Location**: SQLite database is automatically created at:
- **Windows**: `Data/searchDB.db` in the project root
- **Cross-platform**: Uses `Shared/Paths.cs` for automatic platform detection

### Default Ports & Configuration
- **SearchAPI**: `http://localhost:5137` (REST API endpoints - direct access)
  - **API Instance 1**: `http://localhost:5137` (INSTANCE_ID=API-1)
  - **API Instance 2**: `http://localhost:5138` (INSTANCE_ID=API-2)
  - **API Instance 3**: `http://localhost:5139` (INSTANCE_ID=API-3)
- **nginx Load Balancer**: `http://localhost:8080` (X-Scale - routes to API instances)
- **SearchWebApp**: `http://localhost:5000` or `https://localhost:5001` (Blazor Server UI)
- **Database**: SQLite file-based (no server required)

**Note**: Clients (ConsoleSearch and SearchWebApp) connect to nginx load balancer (port 8080) when using X-Scale setup, or directly to SearchAPI (port 5137) for single-instance mode.

### Search Result Ordering Logic
**Regular Search** (SQL-based in DatabaseSqlite.cs):
```sql
ORDER BY count DESC, docId ASC
```

**Pattern Search** (C#-based in SearchLogic.cs):
```csharp
OrderByDescending(x => x.Words.Count)
.ThenBy(x => GetFilenameNumber(docMap[x.DocId]))
```
- Primary sort: Number of matching terms (descending)
- Secondary sort: Filename number extracted from document path (ascending)

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
- **Core Search Engine**: Indexing and search functionality with SQLite inverted index
- **Y-Scaled Architecture**: Clean separation with 5-project structure (indexer, ConsoleSearch, SearchAPI, SearchWebApp, Shared)
- **RESTful API**: SearchAPI provides HTTP endpoints for search operations
- **Modern Web UI**: Blazor Server app with Claude.ai-inspired dark theme
- **Cross-platform support**: Windows/macOS/Linux compatibility
- **Realistic test data**: Enron email dataset (small/medium/large)
- **Enhanced statistics**: Word frequency analysis and indexing metrics
- **Advanced search features**:
  - Case sensitivity control
  - Configurable result limits (20/50/100/150/200/custom)
  - Timestamp display control
  - Pattern matching with wildcards (`?` and `*`)
  - Compact view with expandable results
  - **Fixed search result ordering**: Pattern search now orders by filename number instead of database ID
- **Multiple interfaces**: Console, API, and Web UI all fully functional
- **Interactive features**: Toggle-based configuration, contextual help system
- **Professional UI/UX**: Claude.ai color scheme, responsive design, loading states
- **Performance optimized**: Typical search response times ~20-25ms for pattern searches

### 🔧 Recent Bug Fixes & Improvements
- **Pattern Search Ordering Bug Fixed** (SearchLogic.cs:148): Added `GetFilenameNumber()` method to extract numeric part from filenames and ensure proper ordering (15, 101, 126, 143... instead of chaotic database ID ordering)
- **Home Page Academic**: Updated to show "IT-Arkitektur 6. Semester" with "Try Me" button for exam context
- **Debug Output Cleaned**: Removed all Console.WriteLine debug statements from SearchAPI for clean production output
- **Timestamp Formatting**: Switched to Danish 24-hour format and removed duplicate timestamp displays
- **Zero Warnings Achievement**: Fixed all nullable reference type warnings across the solution
- **File Organization**: Moved SearchLogic to Services folder for better MVC structure

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

**Status**: All 6 core assignments are fully implemented and working. The system includes enhanced statistics, configurable search options, pattern matching capabilities, compact view display, comprehensive help system, and modern web UI. Additional bug fixes and improvements have been completed for exam presentation quality.

## Academic Course Context

### Current Module: IT-Architecture Semester 6
- **Module 2**: Search Engine PoC (completed assignments 1-6)
- **Module 3**: AKF Scale Cube architecture patterns ✅ **IMPLEMENTED**
  - **Y-Scale** (functional decomposition): Search logic separated into dedicated API service
  - **Modern web frontend**: Blazor application consuming REST API
  - **Microservices approach**: Clear separation between indexer, API, console, and web UI
- Course materials located in `Documents/Modul X - Agenda/` folders
- Assignment responses and analysis files created as course progresses

### Course Materials Structure
- **Reading materials**: `Læselektier.txt` and `Læselektier.pdf` files
- **Assignment responses**: `Opgaver_ModulX.txt` files with Danish answers
- **Database guides**: Technical documentation for SQLite inspection

## Getting Started Checklist

### Quick Start (Console Version)
1. Verify .NET 9.0 installation
2. ✅ Cross-platform paths already configured automatically
3. Install SQLite browser for database inspection
4. Build solution: `dotnet build SearchEngine.sln`
5. Run indexer with dataset selection: `cd indexer && dotnet run medium`
6. Test console search: `cd ConsoleSearch && dotnet run`
7. Explore interactive menu features (case sensitivity, timestamps, result limits, pattern search, compact view)
8. Try the help system by typing `?` to see all available options and examples

### Full Web Experience (Recommended) ✅ **NEW**
1-5. Follow steps 1-5 from Quick Start above
6. **Start API service**: `cd SearchAPI && dotnet run` (keep running)
7. **Start web application**: Open new terminal, `cd SearchWebApp && dotnet run`
8. **Open browser**: Navigate to `http://localhost:5000` or `https://localhost:5001`
9. **Experience modern UI**: Claude.ai-inspired interface with dark theme, orange accents, and IT-Arkitektur branding
10. **Test all features**: Search, filters, pattern matching, result limits, compact view with properly ordered results

## Architecture Notes

### Key Files
- `indexer/App.cs` - Indexing main workflow at indexer:32
- `indexer/Crawler.cs` - Text extraction and word parsing logic
- `ConsoleSearch/App.cs` - Interactive search console interface
- `SearchAPI/SearchLogic.cs` - Search algorithm and ranking (moved from ConsoleSearch) at SearchAPI:10
- `SearchAPI/Controllers/SearchController.cs` - REST API endpoints at SearchAPI:8
- `SearchWebApp/Pages/Search.razor` - Claude.ai-inspired web interface at SearchWebApp:1
- `SearchWebApp/wwwroot/css/claude-theme.css` - Dark theme styling at SearchWebApp:1
- `SearchWebApp/Shared/MainLayout.razor` - App layout with collapsible sidebar at SearchWebApp:1
- `Shared/Paths.cs` - Cross-platform database path configuration at Shared:7
- `nginx/nginx.conf` - Round-robin load balancer configuration
- `nginx/nginx-sticky.conf` - Sticky sessions load balancer configuration
- `scripts/` - Deployment automation (start-api-instances.sh, start-nginx.sh, etc.)
- `documentation/` - Module implementation history and guides

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

## Troubleshooting

### Common Issues

**Build Errors - "File is locked by SearchAPI"**
- **Problem**: Cannot build solution when SearchAPI is running
- **Solution**: Stop all running instances before building: `Ctrl+C` in API terminal, then `dotnet build`

**Port Conflicts**
- **Problem**: API won't start due to port 5137 in use
- **Solution**: Kill existing processes or change port in `SearchAPI/Properties/launchSettings.json`

**Web App API Connection Issues**
- **Problem**: Web app shows "Failed to load search results"
- **Solution**: Ensure SearchAPI is running on `localhost:5137` before starting SearchWebApp

**Empty Search Results**
- **Problem**: All searches return 0 results
- **Solution**: Run indexer first: `cd indexer && dotnet run medium`

**Database File Missing**
- **Problem**: "Database file not found" errors
- **Solution**: Check that `Data/searchDB.db` exists after running indexer

**nginx Won't Start**
- **Problem**: `nginx: command not found` or `nginx: [emerg] bind() to 0.0.0.0:8080 failed`
- **Solution**: Install nginx with `brew install nginx` (macOS) or check if port 8080 is already in use

**Load Balancer Not Distributing Requests**
- **Problem**: All requests go to same API instance (e.g., always shows "Instance: API-1")
- **Solution**:
  - Verify all 3 API instances are running: `ps aux | grep dotnet.*SearchAPI`
  - Check nginx error logs: `tail -f nginx/logs/error.log`
  - Restart nginx: `scripts/stop-all.sh && scripts/start-nginx.sh`
  - **Note**: With sticky sessions (`start-nginx-sticky.sh`), this is expected behavior!

**API Instance Won't Start - Port Already in Use**
- **Problem**: `Address already in use` when starting multiple API instances
- **Solution**: Kill existing dotnet processes: `pkill -f "dotnet.*SearchAPI"` then restart with scripts

**Clients Still Connecting to Single API Instance**
- **Problem**: After setting up load balancer, clients still connect directly to port 5137
- **Solution**: Check configuration:
  - **ConsoleSearch**: Environment variable `API_BASE_URL` (default: http://localhost:8080)
  - **SearchWebApp**: `appsettings.json` → `"ApiSettings:BaseUrl": "http://localhost:8080"`
  - If you want single-instance mode, set `API_BASE_URL=http://localhost:5137` or update appsettings.json

**Instance ID Not Showing in UI**
- **Problem**: Watermark or console output doesn't show instance ID
- **Solution**:
  - Check environment variables are set in `launchSettings.json` (INSTANCE_ID=API-1/2/3)
  - Restart API instances after configuration changes
  - Verify SearchController includes instanceId in JSON response

### Debug Information

**Database Inspection**:
```bash
# Use SQLite browser to open Data/searchDB.db
# Check tables: Document, Word, Occurrence
# Verify data was indexed properly
```

**API Endpoint Testing**:
```bash
# Test API directly:
http://localhost:5137/api/search?query=test  # Single instance
http://localhost:8080/api/search?query=test  # Load balancer

# Test health endpoints:
http://localhost:5137/api/search/health  # Single instance health
http://localhost:8080/health  # nginx health
http://localhost:8080/api/search/health  # Backend health via load balancer

# Test pattern search:
http://localhost:5137/api/search/pattern?pattern=t*st
```

**Performance Verification**:
- Typical response times: 20-25ms for pattern searches
- Database size: ~5MB for medium dataset (~5,000 documents)
- Memory usage: <100MB for all components