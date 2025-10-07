# Quick Start Guide

## Single Instance (Development)

⚠️ **You manually start the API**

```bash
export API_BASE_URL=http://localhost:5137
cd SearchAPI && dotnet run        # Start API manually
cd SearchWebApp && dotnet run     # Separate terminal - start WebApp
```

## Round-Robin Load Balancing

⚠️ **Script starts 3 API instances automatically - only start WebApp!**

```bash
scripts/start-api-instances.sh   # Starts API-1, API-2, API-3 automatically
scripts/start-nginx.sh           # Starts load balancer
cd SearchWebApp && dotnet run    # Only start WebApp - APIs already running!
```

## Sticky Sessions Load Balancing

⚠️ **Script starts 3 API instances automatically - only start WebApp!**

```bash
scripts/start-api-instances.sh   # Starts API-1, API-2, API-3 automatically
scripts/start-nginx-sticky.sh    # Starts load balancer with sticky sessions
cd SearchWebApp && dotnet run    # Only start WebApp - APIs already running!
```
