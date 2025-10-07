#!/bin/bash

# Start multiple SearchAPI instances for X-Scale load balancing
# Part of IT-Arkitektur 6. semester - AKF Scale Cube implementation

echo "=== Starting SearchAPI Instances for X-Scale Load Balancing ==="
echo ""

# Determine script location and navigate to project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

# Verify we're in the correct project root
if [ ! -f "SearchEngine.sln" ]; then
    echo "ERROR: Not in project root directory"
    echo "Expected to find SearchEngine.sln"
    exit 1
fi

# Navigate to SearchAPI directory
cd SearchAPI

# Start API Instance 1 (Port 5137, INSTANCE_ID=API-1)
echo "Starting API-1 on port 5137..."
dotnet run --launch-profile http &
API1_PID=$!
sleep 2

# Start API Instance 2 (Port 5138, INSTANCE_ID=API-2)
echo "Starting API-2 on port 5138..."
dotnet run --launch-profile http2 &
API2_PID=$!
sleep 2

# Start API Instance 3 (Port 5139, INSTANCE_ID=API-3)
echo "Starting API-3 on port 5139..."
dotnet run --launch-profile http3 &
API3_PID=$!
sleep 2

echo ""
echo "=== All API Instances Started ==="
echo "API-1: http://localhost:5137 (PID: $API1_PID)"
echo "API-2: http://localhost:5138 (PID: $API2_PID)"
echo "API-3: http://localhost:5139 (PID: $API3_PID)"
echo ""
echo "Next steps:"
echo "  1. Start load balancer: scripts/start-nginx.sh (round-robin)"
echo "  2. Or sticky sessions: scripts/start-nginx-sticky.sh"
echo "  3. Stop all: scripts/stop-all.sh"
echo ""
echo "Press Ctrl+C to stop (note: instances will remain running in background)"

# Wait for all background jobs
wait
