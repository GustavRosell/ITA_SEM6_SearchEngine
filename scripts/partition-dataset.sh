#!/bin/bash
# partition-dataset.sh - Automates database partitioning for Z-Scale architecture
# Part of IT-Arkitektur Module 7: Data Partitioning (Z-Scale)
#
# Usage: ./scripts/partition-dataset.sh <dataset> <num_partitions>
# Example: ./scripts/partition-dataset.sh medium 3
#
# This script runs the indexer multiple times, each time creating a separate
# database partition containing a subset of the documents.

# Check if correct number of arguments provided
if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <dataset> <num_partitions>"
    echo "  dataset: small, medium, or large"
    echo "  num_partitions: number of database partitions to create (e.g., 2, 3, 4)"
    echo ""
    echo "Example: $0 medium 3"
    exit 1
fi

DATASET=$1
NUM_PARTITIONS=$2

# Validate dataset parameter
if [[ ! "$DATASET" =~ ^(small|medium|large)$ ]]; then
    echo "Error: Dataset must be 'small', 'medium', or 'large'"
    exit 1
fi

# Validate num_partitions is a positive integer
if ! [[ "$NUM_PARTITIONS" =~ ^[0-9]+$ ]] || [ "$NUM_PARTITIONS" -lt 1 ]; then
    echo "Error: num_partitions must be a positive integer"
    exit 1
fi

echo "========================================"
echo "Z-Scale Database Partitioning"
echo "========================================"
echo "Dataset: $DATASET"
echo "Partitions: $NUM_PARTITIONS"
echo "========================================"
echo ""

# Change to indexer directory
cd "$(dirname "$0")/../indexer" || exit 1

# Loop through each partition
for (( i=1; i<=NUM_PARTITIONS; i++ )); do
    echo "----------------------------------------"
    echo "Creating Partition $i of $NUM_PARTITIONS"
    echo "Database: searchDB$i.db"
    echo "----------------------------------------"

    # Run indexer with partition parameters
    dotnet run "$DATASET" "$i" "$NUM_PARTITIONS"

    if [ $? -eq 0 ]; then
        echo "✓ Partition $i created successfully"
    else
        echo "✗ Error creating partition $i"
        exit 1
    fi

    echo ""
done

echo "========================================"
echo "Partitioning Complete!"
echo "========================================"
echo "Created $NUM_PARTITIONS database partitions for $DATASET dataset:"
for (( i=1; i<=NUM_PARTITIONS; i++ )); do
    echo "  - searchDB$i.db"
done
echo ""
echo "Next steps:"
echo "  1. Start Z-Scale stack: ./scripts/start-z-scale.sh"
echo "  2. Test Coordinator: curl http://localhost:5153/api/search?query=test"
echo "========================================"
