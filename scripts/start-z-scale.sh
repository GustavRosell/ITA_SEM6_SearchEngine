#!/bin/bash
# start-z-scale.sh - Starts all services for Z-Scale data partitioning architecture
# Part of IT-Arkitektur Module 7: Z-Scale Implementation
#
# Usage: ./scripts/start-z-scale.sh
#
# This script starts:
#   - 3 SearchAPI instances (ports 5137, 5138, 5139) with different database partitions
#   - 1 Coordinator instance (port 5153) for aggregating search results
#
# Prerequisites:
#   - Database partitions must exist (searchDB1.db, searchDB2.db, searchDB3.db)
#   - Run partition-dataset.sh first if needed

echo "========================================"
echo "Starting Z-Scale Architecture"
echo "========================================"
echo ""

# Get the project root directory
PROJECT_ROOT="$(dirname "$0")/.."
cd "$PROJECT_ROOT" || exit 1

# Create logs directory if it doesn't exist
mkdir -p logs

echo "Starting SearchAPI instances..."
echo "----------------------------------------"

# Start SearchAPI Instance 1 (port 5137, searchDB1.db)
echo "Starting API-1 on port 5137 (searchDB1.db)..."
cd SearchAPI || exit 1
dotnet run --launch-profile http > ../logs/api1.log 2>&1 &
API1_PID=$!
echo "  PID: $API1_PID (log: logs/api1.log)"
cd ..

sleep 2

# Start SearchAPI Instance 2 (port 5138, searchDB2.db)
echo "Starting API-2 on port 5138 (searchDB2.db)..."
cd SearchAPI || exit 1
dotnet run --launch-profile http2 > ../logs/api2.log 2>&1 &
API2_PID=$!
echo "  PID: $API2_PID (log: logs/api2.log)"
cd ..

sleep 2

# Start SearchAPI Instance 3 (port 5139, searchDB3.db)
echo "Starting API-3 on port 5139 (searchDB3.db)..."
cd SearchAPI || exit 1
dotnet run --launch-profile http3 > ../logs/api3.log 2>&1 &
API3_PID=$!
echo "  PID: $API3_PID (log: logs/api3.log)"
cd ..

sleep 2

echo ""
echo "Starting Coordinator..."
echo "----------------------------------------"

# Start Coordinator (port 5153)
echo "Starting Coordinator on port 5153..."
cd Coordinator || exit 1
dotnet run > ../logs/coordinator.log 2>&1 &
COORDINATOR_PID=$!
echo "  PID: $COORDINATOR_PID (log: logs/coordinator.log)"
cd ..

echo ""
echo "========================================"
echo "Z-Scale Stack Started Successfully!"
echo "========================================"
echo ""
echo "Services Running:"
echo "  SearchAPI-1:  http://localhost:5137 (PID: $API1_PID) → searchDB1.db"
echo "  SearchAPI-2:  http://localhost:5138 (PID: $API2_PID) → searchDB2.db"
echo "  SearchAPI-3:  http://localhost:5139 (PID: $API3_PID) → searchDB3.db"
echo "  Coordinator:  http://localhost:5153 (PID: $COORDINATOR_PID)"
echo ""
echo "Testing endpoints:"
echo "  Direct API:   curl http://localhost:5137/api/search?query=test"
echo "  Coordinator:  curl http://localhost:5153/api/search?query=test"
echo "  Health check: curl http://localhost:5153/api/search/ping"
echo ""
echo "Logs are saved to:"
echo "  logs/api1.log, logs/api2.log, logs/api3.log, logs/coordinator.log"
echo ""
echo "To stop all services:"
echo "  ./scripts/stop-z-scale.sh"
echo "========================================"
