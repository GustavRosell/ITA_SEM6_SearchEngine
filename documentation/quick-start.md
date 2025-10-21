# Quick Start Guide

This guide explains the three deployment modes for the SearchEngine project. Each mode demonstrates a different aspect of the AKF Scale Cube.

---

## Mode 1: Development (Single Database)

**Purpose**: Local development, testing, and debugging
**Scaling**: None - simplest setup

### Setup

```bash
# Step 1: Index the dataset
cd indexer
dotnet run medium    # Creates Data/searchDB.db

# Step 2: Start the API
cd SearchAPI
dotnet run          # Runs on http://localhost:5137

# Step 3: Start the Web App (in separate terminal)
cd SearchWebApp
dotnet run          # Runs on http://localhost:5000
```

### Access
- **Web App**: http://localhost:5000
- **API**: http://localhost:5137

### Verify
- Web app loads and search works
- No watermark or simple watermark showing single instance

---

## Mode 2: X-Scale (Horizontal Scaling - Load Balancing)

**Purpose**: Demonstrate traffic distribution across identical API instances
**Scaling**: X-Axis (horizontal duplication for load distribution and high availability)

### Setup

```bash
# Step 1: Index the dataset (same database for all instances)
cd indexer
dotnet run medium    # Creates Data/searchDB.db

# Step 2: Start 3 API instances
scripts/start-api-instances.sh
# Starts API-1 (5137), API-2 (5138), API-3 (5139)
# All read from the SAME database: Data/searchDB.db

# Step 3: Start nginx load balancer
scripts/start-nginx.sh
# nginx listens on http://localhost:8080
# Distributes requests across all 3 API instances

# Step 4: Start the Web App (in separate terminal)
cd SearchWebApp
dotnet run          # Runs on http://localhost:5000
```

### Access
- **Web App**: http://localhost:5000 (connects to nginx on port 8080)
- **nginx**: http://localhost:8080
- **Direct API access** (bypass nginx):
  - API-1: http://localhost:5137
  - API-2: http://localhost:5138
  - API-3: http://localhost:5139

### Verify X-Scale is Working

1. **Web App watermark**: Watch the orange watermark in top-right corner
   - Should rotate between "Instance: API-1", "Instance: API-2", "Instance: API-3"
   - Indicates nginx is load balancing across instances

2. **Direct curl test**:
   ```bash
   # Run this multiple times
   curl http://localhost:8080/api/search?query=test

   # Check "instanceId" field in JSON response
   # Should alternate: "API-1", "API-2", "API-3"
   ```

3. **All instances return identical results** (same database)

### Stop Services
```bash
scripts/stop-all.sh
```

---

## Mode 3: Z-Scale (Data Partitioning - Aggregation)

**Purpose**: Demonstrate distributed search across partitioned data
**Scaling**: Z-Axis (data sharding across multiple databases)

### Setup

```bash
# Step 1: Partition the dataset
scripts/partition-dataset.sh
# Interactive script will prompt for:
# - Dataset size (small/medium/large)
# - Number of partitions (default: 3)
#
# Creates 3 databases with different data:
# - Data/searchDB1.db (subdirs 0, 3, 6, 9, ...)
# - Data/searchDB2.db (subdirs 1, 4, 7, 10, ...)
# - Data/searchDB3.db (subdirs 2, 5, 8, 11, ...)

# Step 2: Start Z-Scale services
scripts/start-z-scale.sh
# Starts:
# - SearchAPI-1 (port 5137) → searchDB1.db
# - SearchAPI-2 (port 5138) → searchDB2.db
# - SearchAPI-3 (port 5139) → searchDB3.db
# - Coordinator (port 5050) → aggregates all partitions

# Step 3: Configure Web App to use Coordinator
# Edit SearchWebApp/appsettings.json:
#   "ApiSettings": { "BaseUrl": "http://localhost:5050/api/coordinator" }

# Step 4: Start the Web App (in separate terminal)
cd SearchWebApp
dotnet run          # Runs on http://localhost:5000
```

### Access
- **Web App**: http://localhost:5000 (connects to Coordinator)
- **Coordinator**: http://localhost:5050
- **Direct partition access** (bypass Coordinator):
  - Partition 1: http://localhost:5137 (partial results)
  - Partition 2: http://localhost:5138 (partial results)
  - Partition 3: http://localhost:5139 (partial results)

### Verify Z-Scale is Working

1. **Check partitioned databases exist**:
   ```bash
   ls -la Data/searchDB*.db
   # Should show 3 files: searchDB1.db, searchDB2.db, searchDB3.db
   ```

2. **Test Coordinator ping**:
   ```bash
   curl http://localhost:5050/api/coordinator/ping
   # Should return: "Coordinator"
   ```

3. **Verify aggregation** (Coordinator has MORE results than individual partitions):
   ```bash
   # Query individual partitions
   curl "http://localhost:5137/api/search?query=energy" | jq '.totalDocuments'
   curl "http://localhost:5138/api/search?query=energy" | jq '.totalDocuments'
   curl "http://localhost:5139/api/search?query=energy" | jq '.totalDocuments'

   # Query through Coordinator (aggregates all partitions)
   curl "http://localhost:5050/api/coordinator?query=energy" | jq '.totalDocuments'
   # Should equal sum of all partitions
   ```

4. **Web App watermark**: Shows "Mode: Z-Scale (3 partitions)" in green
   - Indicates connected to Coordinator (not direct API)

### Stop Services
```bash
scripts/stop-z-scale.sh
```

### Manual Z-Scale Setup (for debugging)

If you prefer step-by-step control:

```bash
# Partition dataset manually
cd indexer
dotnet run small 1 3  # Partition 1 of 3
dotnet run small 2 3  # Partition 2 of 3
dotnet run small 3 3  # Partition 3 of 3

# Start each SearchAPI instance (separate terminals)
cd SearchAPI && dotnet run --launch-profile http   # Terminal 1: API-1
cd SearchAPI && dotnet run --launch-profile http2  # Terminal 2: API-2
cd SearchAPI && dotnet run --launch-profile http3  # Terminal 3: API-3

# Start Coordinator (separate terminal)
cd Coordinator && dotnet run                        # Terminal 4

# Test aggregation
curl "http://localhost:5050/api/coordinator?query=test"
```

---

## Advanced Topics

### X-Scale Advanced Configuration
For detailed nginx configuration, sticky sessions, health checks, and advanced load balancing topics, see:
- `documentation/x-scale-deployment.md`

### Z-Scale Architecture Details
For implementation details, partition algorithms, and architecture diagrams, see:
- `documentation/module-7-z-scale.md`

---

## Comparison: The Three Modes

| Aspect | Mode 1 (Dev) | Mode 2 (X-Scale) | Mode 3 (Z-Scale) |
|--------|--------------|------------------|------------------|
| **Database** | 1 database | 1 database (shared) | 3 databases (partitioned) |
| **API Instances** | 1 instance | 3 instances (identical) | 3 instances (different data) |
| **Load Balancer** | None | nginx | Coordinator |
| **Results per Instance** | Complete | Complete | Partial |
| **Result Aggregation** | N/A | No (same data) | Yes (merge partitions) |
| **Purpose** | Development | Traffic distribution | Data scalability |
| **AKF Axis** | None | X-Axis | Z-Axis |
| **Benefit** | Simple | High availability | Handle large datasets |

---

## Troubleshooting

### Port Conflicts
If ports are already in use:
```bash
# Check what's using a port (Windows)
netstat -ano | findstr :5137

# Kill process by PID
taskkill /F /PID <pid>
```

### Database Not Found
```bash
# Ensure you've indexed the dataset
cd indexer
dotnet run medium

# Check database exists
ls -la Data/searchDB*.db
```

### nginx Won't Start
```bash
# Install nginx (macOS)
brew install nginx

# Check nginx error logs
tail -f nginx/logs/error.log
```

### Services Still Running
```bash
# Stop all .NET processes
taskkill /F /IM dotnet.exe   # Windows
pkill -f dotnet              # macOS/Linux
```
