#!/bin/bash

# Start nginx load balancer with sticky sessions for SearchAPI X-Scale architecture
# Part of IT-Arkitektur 6. semester - AKF Scale Cube implementation

echo "=== Starting nginx Load Balancer (Sticky Sessions) ==="
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

# Check if nginx is installed
if ! command -v nginx &> /dev/null
then
    echo "ERROR: nginx is not installed"
    echo "Install with: brew install nginx (macOS)"
    exit 1
fi

# Check if nginx-sticky.conf exists
if [ ! -f "nginx/nginx-sticky.conf" ]; then
    echo "ERROR: nginx/nginx-sticky.conf not found"
    echo "Run this script from project root"
    exit 1
fi

# Create logs directory if it doesn't exist
mkdir -p nginx/logs

# Start nginx with sticky sessions configuration
echo "Starting nginx on port 8080 with sticky sessions (ip_hash)..."
nginx -c "$PROJECT_ROOT/nginx/nginx-sticky.conf" -p "$PROJECT_ROOT/nginx"

if [ $? -eq 0 ]; then
    echo ""
    echo "=== nginx Load Balancer Started Successfully ==="
    echo "Strategy: Sticky Sessions (same client IP → same backend)"
    echo "Load Balancer URL: http://localhost:8080"
    echo ""
    echo "Backend instances:"
    echo "  - API-1: http://localhost:5137"
    echo "  - API-2: http://localhost:5138"
    echo "  - API-3: http://localhost:5139"
    echo ""
    echo "Test load balancer health: http://localhost:8080/health"
    echo "Test search API: http://localhost:8080/api/search?query=test"
    echo ""
    echo "Note: With sticky sessions, all your requests will go to the same instance"
    echo "      To test distribution, restart nginx or use different client IPs"
    echo ""
    echo "Logs: nginx/logs/access.log and nginx/logs/error.log"
    echo "To stop nginx: scripts/stop-all.sh"
else
    echo "ERROR: Failed to start nginx"
    echo "Check nginx/logs/error.log for details"
    exit 1
fi
