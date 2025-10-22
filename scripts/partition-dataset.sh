#!/bin/bash
# partition-dataset.sh - Automates database partitioning for Z-Scale architecture
# Part of IT-Arkitektur Module 7: Data Partitioning (Z-Scale)
#
# Usage:
#   Interactive: ./scripts/partition-dataset.sh
#   With args:   ./scripts/partition-dataset.sh <dataset> <num_partitions>
# Example: ./scripts/partition-dataset.sh medium 3
#
# This script runs the indexer multiple times, each time creating a separate
# database partition containing a subset of the documents.

# Interactive mode if no arguments provided
if [ "$#" -eq 0 ]; then
    echo "========================================"
    echo "Z-Scale Database Partitioning (Interactive)"
    echo "========================================"
    echo ""
    echo "Select dataset size:"
    echo "  1) small   (13 emails in 1 folder)"
    echo "  2) medium  (~5,000 emails in ~20 mailboxes)"
    echo "  3) large   (~50,000 emails for 15 users)"
    echo ""
    read -p "Choice [1-3]: " dataset_choice

    case $dataset_choice in
        1) DATASET="small" ;;
        2) DATASET="medium" ;;
        3) DATASET="large" ;;
        *)
            echo "Error: Invalid choice. Please select 1, 2, or 3."
            exit 1
            ;;
    esac

    echo ""
    read -p "Number of partitions [default: 3]: " NUM_PARTITIONS
    NUM_PARTITIONS=${NUM_PARTITIONS:-3}
    echo ""
elif [ "$#" -eq 2 ]; then
    # Command-line mode (backward compatibility)
    DATASET=$1
    NUM_PARTITIONS=$2
else
    echo "Usage: $0 [dataset] [num_partitions]"
    echo "  Interactive: $0"
    echo "  With args:   $0 <dataset> <num_partitions>"
    echo ""
    echo "  dataset: small, medium, or large"
    echo "  num_partitions: number of database partitions to create (e.g., 2, 3, 4)"
    echo ""
    echo "Example: $0 medium 3"
    exit 1
fi

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
