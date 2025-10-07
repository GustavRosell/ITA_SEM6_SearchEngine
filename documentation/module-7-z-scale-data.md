# Modul 7: Z-Skalering af Data i Praksis (Data Partitioning)

**Course**: IT-Arkitektur 6. Semester, Erhvervsakademiet Aarhus
**Date**: 8. oktober 2025 (I MORGEN)
**Status**: 🔄 To be implemented

---

## Overview

Implementation af Z-Scale (data partitioning/sharding) for at håndtere store datamængder ved at opdele data across multiple databases eller shards.

---

## Objectives (Planned)

**Z-Scale Principle**: Partition data by attribute
- Data sharding across multiple databases
- Geographic or logical partitioning
- Shard routing logic
- Consistent hashing eller range-based partitioning

---

## Potentielle Approaches

### 1. Document Sharding
- Partition documents by ID range
- Shard 1: docId 1-10000
- Shard 2: docId 10001-20000
- Shard 3: docId 20001+

### 2. Alphabet-Based Sharding
- Shard by first letter of document title
- Shard A-H
- Shard I-P
- Shard Q-Z

### 3. Date-Based Sharding
- Partition by document creation date
- Historical shard (old documents)
- Recent shard (new documents)
- Archive strategy

---

## Expected Implementation

**To be determined based on:**
- Lecture content tomorrow
- Specific assignment requirements
- Performance considerations
- Demonstration needs for exam

---

## Placeholder Notes

**Key Concepts:**
- Consistent hashing
- Shard routing
- Query fanout
- Rebalancing strategies

**Challenges:**
- Cross-shard queries
- Data migration
- Shard coordination
- Complexity vs benefit trade-offs

---

## Files That May Be Modified

**Potential changes:**
- `SearchAPI/Data/DatabaseSqlite.cs` - Shard routing logic
- `SearchAPI/Services/SearchLogic.cs` - Multi-shard queries
- `SearchAPI/Controllers/SearchController.cs` - Shard awareness
- New shard configuration files
- Database connection management

---

## Summary

Z-Scale documentation vil blive opdateret i morgen efter lecture og implementation.

**Previous**: Modul 6 - X-Scale horizontal scaling (completed)
**Current**: Modul 7 - Z-Scale data partitioning (tomorrow)
**Next**: Exam preparation og final polishing
