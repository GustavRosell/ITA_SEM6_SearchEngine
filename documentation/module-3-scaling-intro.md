# Modul 3: Skalering - AKF Scale Cube Intro

**Course**: IT-Arkitektur 6. Semester, Erhvervsakademiet Aarhus
**Date**: 3. september 2025
**Status**: ✅ Teoretisk introduktion gennemgået

---

## Overview

Introduktion til skaleringskoncepter og AKF Scale Cube. Teoretisk fundament for kommende praktiske implementationer.

---

## AKF Scale Cube

**De tre akser for skalering:**

### X-Axis: Horizontal Duplication
- Cloning af identiske services
- Load balancing
- Stateless design
- **Implementeres i Modul 6**

### Y-Axis: Functional Decomposition
- Opdeling efter funktion/service
- Microservices
- API separation
- **Implementeres i Modul 4**

### Z-Axis: Data Partitioning
- Opdeling efter data
- Sharding
- Geographic distribution
- **Implementeres i Modul 7**

---

## Diskussioner

**Hvornår bruger man hvilken skalering?**
- X-Axis: Simple workload increases
- Y-Axis: Different services have different scaling needs
- Z-Axis: Data volume becomes bottleneck

**Kombinationer:**
- Ofte bruges flere akser samtidigt
- Start med Y (services), derefter X (instances), så Z (data)

---

## Relevans for Search Engine

**Vores case:**
- Stor organisation (50+ employees)
- Multi-terabyte document collections
- "Instant content search" requirement

**Skalerings-behov:**
- Y-Axis: Separer search fra andre services
- X-Axis: Multiple search instances for availability
- Z-Axis: Partition documents across shards

---

## Summary

Teoretisk fundament for skaleringsarkitektur etableret.

**Next**: Modul 4 - Y-Scale praktisk implementation
