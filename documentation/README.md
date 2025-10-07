# Documentation Overview

**Project**: SearchEngine PoC - IT-Arkitektur 6. Semester
**Student**: Gustav Rosell Klitholm
**Institution**: Erhvervsakademiet Aarhus

---

## Module Timeline

| Modul | Date | Topic | Status |
|-------|------|-------|--------|
| 2 | 27-08-2025 | Intro til Case-System | ✅ Complete |
| 3 | 03-09-2025 | Skalering (AKF Scale Cube Intro) | ✅ Complete |
| 4 | 10-09-2025 | Y-Scale af Service | ✅ Complete |
| 5 | 24-09-2025 | Teknikker til Specifikation | ⚠️ One optional task pending |
| 6 | 01-10-2025 | X-Skalering af Kode | ✅ Complete |
| 7 | 08-10-2025 | Z-Skalering af Data | 🔄 Tomorrow |

---

## Document Structure

Each module has its own detailed markdown file documenting:
- **Overview**: What was implemented
- **Objectives**: Learning goals and requirements
- **Implementation**: Technical details and code changes
- **Files Modified/Created**: Specific file references with line numbers
- **Testing**: How to verify the implementation
- **Summary**: Key takeaways and next steps

---

## Quick Links

### Core Modules
- [Modul 2: Case-System Intro](./module-2-case-system-intro.md) - Basic search engine features
- [Modul 3: Scaling Intro](./module-3-scaling-intro.md) - AKF Scale Cube theory
- [Modul 4: Y-Scale Service](./module-4-y-scale-service.md) - API separation and WebApp
- [Modul 5: Specification](./module-5-specification-techniques.md) - Documentation techniques
- [Modul 6: X-Scale Horizontal](./module-6-x-scale-horizontal.md) - Load balancing and multiple instances
- [Modul 7: Z-Scale Data](./module-7-z-scale-data.md) - Data partitioning (tomorrow)

---

## Architecture Evolution

### Phase 1: Monolithic Console App (Modul 2)
```
ConsoleSearch (UI + Logic + Data)
```

### Phase 2: Y-Scale - Service Separation (Modul 4)
```
SearchAPI (Service)
├── ConsoleSearch (Client 1)
└── SearchWebApp (Client 2)
```

### Phase 3: X-Scale - Horizontal Scaling (Modul 6)
```
nginx Load Balancer
├── SearchAPI Instance 1
├── SearchAPI Instance 2
└── SearchAPI Instance 3
    ├── ConsoleSearch
    └── SearchWebApp
```

### Phase 4: Z-Scale - Data Partitioning (Modul 7 - Tomorrow)
```
nginx Load Balancer
├── SearchAPI → Shard Router
    ├── Database Shard 1
    ├── Database Shard 2
    └── Database Shard 3
```

---

## Key Technologies

**Backend:**
- .NET 9.0 (C#)
- ASP.NET Core Web API
- SQLite with inverted index

**Frontend:**
- Blazor Server
- Claude.ai-inspired dark theme

**Infrastructure:**
- nginx load balancer
- Round-robin and sticky sessions strategies
- Multiple instance deployment

**DevOps:**
- Bash scripts for deployment
- Environment-based configuration
- Health monitoring endpoints

---

## Files Organization

```
ITA_SEM6_SearchEngine/
├── documentation/           # THIS FOLDER
│   ├── README.md           # This file
│   ├── module-2-case-system-intro.md
│   ├── module-3-scaling-intro.md
│   ├── module-4-y-scale-service.md
│   ├── module-5-specification-techniques.md
│   ├── module-6-x-scale-horizontal.md
│   └── module-7-z-scale-data.md
├── nginx/                   # Load balancer configs
├── scripts/                 # Deployment scripts
├── SearchAPI/              # Y-Scale service
├── SearchWebApp/           # Blazor UI
├── ConsoleSearch/          # Console client
├── indexer/                # Document indexing
├── Shared/                 # Common models
└── CLAUDE.md               # Technical documentation
```

---

## For Exam Preparation

**Demonstration Flow:**
1. Start with Modul 2 doc - Show basic features
2. Explain Y-Scale - Show API separation (Modul 4)
3. Demonstrate X-Scale - Live load balancing with watermarks (Modul 6)
4. Discuss Z-Scale - Data partitioning strategy (Modul 7)

**Key Selling Points:**
- Complete AKF Scale Cube implementation
- Professional folder structure
- Clean code with documentation
- Industry-standard tools (nginx)
- Flexible deployment modes
- Real-world scalability patterns

---

## Updates

**Latest**: 1. oktober 2025
- Modul 6 completed (X-Scale)
- Documentation structure created
- Ready for Modul 7 tomorrow

**Next**: 8. oktober 2025
- Implement Z-Scale
- Update module-7 documentation
- Final exam prep
