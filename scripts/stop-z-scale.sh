#!/bin/bash
# stop-z-scale.sh - Stops all Z-Scale architecture services
# Part of IT-Arkitektur Module 7: Z-Scale Implementation
#
# Usage: ./scripts/stop-z-scale.sh
#
# This script gracefully stops all running SearchAPI and Coordinator processes

echo "========================================"
echo "Stopping Z-Scale Architecture"
echo "========================================"
echo ""

# Function to kill processes by name
kill_process() {
    local process_name=$1
    local pids=$(ps aux | grep "$process_name" | grep -v grep | awk '{print $2}')

    if [ -z "$pids" ]; then
        echo "No $process_name processes found"
    else
        echo "Stopping $process_name processes:"
        for pid in $pids; do
            echo "  Killing PID: $pid"
            kill $pid 2>/dev/null
        done
    fi
}

# Stop SearchAPI instances
echo "Stopping SearchAPI instances..."
kill_process "dotnet.*SearchAPI"

echo ""

# Stop Coordinator instances
echo "Stopping Coordinator instances..."
kill_process "dotnet.*Coordinator"

echo ""
echo "========================================"
echo "Z-Scale Stack Stopped"
echo "========================================"
echo ""
echo "All SearchAPI and Coordinator processes have been terminated."
echo ""
echo "To restart:"
echo "  ./scripts/start-z-scale.sh"
echo "========================================"
