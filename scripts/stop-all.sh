#!/bin/bash

# Stop all SearchAPI instances and nginx load balancer
# Part of IT-Arkitektur 6. semester - AKF Scale Cube implementation

echo "=== Stopping All Services ==="
echo ""

# Stop nginx
echo "Stopping nginx..."
nginx -s stop 2>/dev/null
if [ $? -eq 0 ]; then
    echo "✓ nginx stopped successfully"
else
    echo "✗ nginx not running or already stopped"
fi

# Stop all dotnet processes running SearchAPI
echo "Stopping SearchAPI instances..."
BEFORE_COUNT=$(pgrep -f "dotnet.*SearchAPI" | wc -l)

if [ "$BEFORE_COUNT" -gt 0 ]; then
    pkill -f "dotnet.*SearchAPI" 2>/dev/null
    sleep 1

    # Verify they're stopped
    AFTER_COUNT=$(pgrep -f "dotnet.*SearchAPI" | wc -l)
    if [ "$AFTER_COUNT" -eq 0 ]; then
        echo "✓ Stopped $BEFORE_COUNT SearchAPI instance(s)"
    else
        echo "⚠ Warning: $AFTER_COUNT instance(s) still running"
        echo "  Try: pkill -9 -f \"dotnet.*SearchAPI\""
    fi
else
    echo "✗ No SearchAPI instances running"
fi

# Check for orphaned dotnet processes
DOTNET_COUNT=$(pgrep -f "dotnet" | wc -l)
if [ "$DOTNET_COUNT" -gt 0 ]; then
    echo ""
    echo "Note: $DOTNET_COUNT other dotnet process(es) still running"
    echo "  (This is normal if you have other .NET apps running)"
fi

echo ""
echo "=== All Services Stopped ==="
