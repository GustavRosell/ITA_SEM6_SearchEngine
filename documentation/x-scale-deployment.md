# X-Scale Deployment Guide

This document provides advanced configuration details for X-Scale (horizontal scaling) deployment with nginx load balancing.

**For quick setup**, see `documentation/quick-start.md` - Mode 2.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [nginx Configuration](#nginx-configuration)
3. [Load Balancing Strategies](#load-balancing-strategies)
4. [Manual Deployment](#manual-deployment)
5. [Health Checks](#health-checks)
6. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

### X-Scale Pattern

```
Client Requests
      ↓
nginx (Port 8080) - Load Balancer
      ↓ (distributes traffic)
  ┌───┼───┐
  ↓   ↓   ↓
API-1 API-2 API-3 (Ports 5137, 5138, 5139)
  ↓   ↓   ↓
searchDB.db (SAME database - all instances share identical data)
```

### Key Characteristics

- **Horizontal Duplication**: All API instances are identical
- **Shared Data**: All instances read from the same database
- **Traffic Distribution**: nginx distributes requests across instances
- **High Availability**: If one instance fails, others continue serving
- **Stateless**: Each request can be handled by any instance

---

## nginx Configuration

### Default Configuration (`nginx/nginx.conf`)

**Round-Robin Load Balancing**:

```nginx
worker_processes 1;

events {
    worker_connections 1024;
}

http {
    # Define upstream backend servers
    upstream searchapi {
        server localhost:5137;  # SearchAPI-1
        server localhost:5138;  # SearchAPI-2
        server localhost:5139;  # SearchAPI-3
    }

    # Main server block
    server {
        listen 8080;
        server_name localhost;

        # Proxy all requests to backend
        location / {
            proxy_pass http://searchapi;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        }

        # Health check endpoint
        location /health {
            access_log off;
            return 200 "nginx OK\n";
            add_header Content-Type text/plain;
        }
    }
}
```

### Sticky Sessions Configuration (`nginx/nginx-sticky.conf`)

**IP Hash for Session Affinity**:

```nginx
worker_processes 1;

events {
    worker_connections 1024;
}

http {
    # Define upstream with ip_hash for sticky sessions
    upstream searchapi {
        ip_hash;  # Same client IP → same backend
        server localhost:5137;  # SearchAPI-1
        server localhost:5138;  # SearchAPI-2
        server localhost:5139;  # SearchAPI-3
    }

    server {
        listen 8080;
        server_name localhost;

        location / {
            proxy_pass http://searchapi;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        }

        location /health {
            access_log off;
            return 200 "nginx OK (sticky)\n";
            add_header Content-Type text/plain;
        }
    }
}
```

---

## Load Balancing Strategies

### 1. Round-Robin (Default)

**Configuration**: Default `upstream searchapi` block

**Behavior**:
- Requests distributed evenly across all backends
- Request 1 → API-1, Request 2 → API-2, Request 3 → API-3, Request 4 → API-1...

**Use Case**:
- Default choice for most scenarios
- Even load distribution
- No session state required

**Start**:
```bash
scripts/start-nginx.sh
```

### 2. Sticky Sessions (IP Hash)

**Configuration**: `upstream searchapi { ip_hash; ... }`

**Behavior**:
- Same client IP always routed to same backend
- Based on hash of client IP address
- Maintains session affinity

**Use Case**:
- Applications with server-side session state
- Cache warming scenarios
- Debugging specific instance

**Start**:
```bash
scripts/start-nginx-sticky.sh
```

### 3. Weighted Round-Robin (Advanced)

**Configuration**:
```nginx
upstream searchapi {
    server localhost:5137 weight=3;  # 60% of traffic
    server localhost:5138 weight=1;  # 20% of traffic
    server localhost:5139 weight=1;  # 20% of traffic
}
```

**Use Case**:
- Backends with different hardware capabilities
- Gradual rollout of new version
- Canary deployments

### 4. Least Connections (Advanced)

**Configuration**:
```nginx
upstream searchapi {
    least_conn;  # Route to backend with fewest active connections
    server localhost:5137;
    server localhost:5138;
    server localhost:5139;
}
```

**Use Case**:
- Long-running requests
- Uneven request processing times
- Optimize backend utilization

---

## Manual Deployment

### Step-by-Step Manual Setup

**Terminal 1: SearchAPI Instance 1**
```bash
cd SearchAPI
dotnet run --launch-profile http
# Starts on http://localhost:5137
# Environment: INSTANCE_ID=API-1
```

**Terminal 2: SearchAPI Instance 2**
```bash
cd SearchAPI
dotnet run --launch-profile http2
# Starts on http://localhost:5138
# Environment: INSTANCE_ID=API-2
```

**Terminal 3: SearchAPI Instance 3**
```bash
cd SearchAPI
dotnet run --launch-profile http3
# Starts on http://localhost:5139
# Environment: INSTANCE_ID=API-3
```

**Terminal 4: nginx**
```bash
# macOS/Linux
nginx -c $(pwd)/nginx/nginx.conf -p $(pwd)/nginx

# Windows (Git Bash)
nginx -c "$(pwd)/nginx/nginx.conf" -p "$(pwd)/nginx"
```

**Terminal 5: Web App**
```bash
cd SearchWebApp
dotnet run
# Access: http://localhost:5000
```

### Verify Manual Setup

```bash
# Check all APIs are running
curl http://localhost:5137/api/search/ping  # Should return "API-1"
curl http://localhost:5138/api/search/ping  # Should return "API-2"
curl http://localhost:5139/api/search/ping  # Should return "API-3"

# Check nginx
curl http://localhost:8080/health           # Should return "nginx OK"

# Test load balancing
for i in {1..6}; do
  curl -s http://localhost:8080/api/search?query=test | jq -r '.instanceId'
done
# Should show: API-1, API-2, API-3, API-1, API-2, API-3 (round-robin)
```

---

## Health Checks

### nginx Health Check

```bash
curl http://localhost:8080/health
# Expected: "nginx OK"
```

### Backend Health Checks

```bash
# Individual API instances
curl http://localhost:5137/api/search/ping
curl http://localhost:5138/api/search/ping
curl http://localhost:5139/api/search/ping
# Expected: Returns instance ID (API-1, API-2, API-3)
```

### Automated Health Monitoring

```bash
#!/bin/bash
# health-check.sh

echo "Checking nginx..."
curl -sf http://localhost:8080/health || echo "nginx DOWN"

echo "Checking backends..."
for port in 5137 5138 5139; do
  response=$(curl -sf http://localhost:$port/api/search/ping)
  if [ $? -eq 0 ]; then
    echo "Port $port: UP ($response)"
  else
    echo "Port $port: DOWN"
  fi
done
```

---

## Troubleshooting

### nginx Won't Start

**Problem**: `nginx: [emerg] bind() to 0.0.0.0:8080 failed (Address already in use)`

**Solution**:
```bash
# Check what's using port 8080
netstat -ano | findstr :8080   # Windows
lsof -i :8080                  # macOS/Linux

# Kill the process or change nginx port in nginx.conf
```

**Problem**: `nginx: command not found`

**Solution**:
```bash
# Install nginx
brew install nginx  # macOS
# Windows: Download from nginx.org
```

### Load Balancer Not Distributing

**Problem**: All requests go to same instance

**Solution**:
```bash
# Check if using sticky sessions (ip_hash)
grep ip_hash nginx/nginx.conf

# If using sticky sessions, that's expected behavior
# To test round-robin, use different IP addresses or:
scripts/start-nginx.sh  # Use round-robin config
```

### API Instance Down

**Problem**: One API instance not responding

**Solution**:
```bash
# nginx will automatically route around failed backends
# Check which instance is down
curl http://localhost:5137/api/search/ping
curl http://localhost:5138/api/search/ping
curl http://localhost:5139/api/search/ping

# Restart the failed instance
cd SearchAPI
dotnet run --launch-profile http  # or http2/http3
```

### Watermark Not Changing

**Problem**: Web app shows same instance ID

**Checks**:
1. Is nginx running? `curl http://localhost:8080/health`
2. Is WebApp configured correctly? Check `appsettings.json`:
   ```json
   "ApiSettings": { "BaseUrl": "http://localhost:8080" }
   ```
3. Are you using sticky sessions? `grep ip_hash nginx/nginx.conf`
4. Try incognito/different browser (clears cache)

### Stop All Services

```bash
# Automated
scripts/stop-all.sh

# Manual
# Stop nginx
nginx -s stop

# Stop all .NET processes
taskkill /F /IM dotnet.exe   # Windows
pkill -f dotnet              # macOS/Linux
```

---

## Performance Considerations

### Connection Pooling

Each backend API instance maintains its own database connection pool. With 3 instances:
- Total connections: 3 × pool size
- Monitor with `sqlite3 Data/searchDB.db ".databases"`

### Request Distribution

With round-robin and 3 backends:
- Each instance handles ~33% of traffic
- Load distribution is approximate (not exact)

### Session Affinity Trade-offs

**Round-Robin**:
- ✅ Even load distribution
- ✅ Better fault tolerance
- ❌ No session affinity

**Sticky Sessions (ip_hash)**:
- ✅ Same user → same instance (cache benefits)
- ❌ Uneven load if few clients
- ❌ Instance failure affects specific users

---

## Advanced nginx Features

### Request Logging

```nginx
http {
    log_format detailed '$remote_addr - [$time_local] "$request" '
                       '$status $body_bytes_sent "$http_referer" '
                       '$upstream_addr $upstream_status';

    access_log logs/access.log detailed;

    upstream searchapi {
        # ... backends
    }
}
```

### Timeouts

```nginx
location / {
    proxy_pass http://searchapi;
    proxy_connect_timeout 5s;
    proxy_send_timeout 30s;
    proxy_read_timeout 30s;
}
```

### Failover Configuration

```nginx
upstream searchapi {
    server localhost:5137 max_fails=3 fail_timeout=30s;
    server localhost:5138 max_fails=3 fail_timeout=30s;
    server localhost:5139 max_fails=3 fail_timeout=30s backup;  # Only use if others fail
}
```

---

## Deployment Automation Scripts

### Start All Services

**File**: `scripts/start-api-instances.sh`

```bash
#!/bin/bash
# Starts 3 SearchAPI instances in background

cd SearchAPI
dotnet run --launch-profile http > /dev/null 2>&1 &
echo "Started API-1 (PID: $!)"

dotnet run --launch-profile http2 > /dev/null 2>&1 &
echo "Started API-2 (PID: $!)"

dotnet run --launch-profile http3 > /dev/null 2>&1 &
echo "Started API-3 (PID: $!)"

echo "All API instances started"
```

### Start nginx

**File**: `scripts/start-nginx.sh`

```bash
#!/bin/bash
# Starts nginx with round-robin config

nginx -c "$(pwd)/nginx/nginx.conf" -p "$(pwd)/nginx"
echo "nginx started on port 8080 (round-robin)"
```

### Stop All

**File**: `scripts/stop-all.sh`

```bash
#!/bin/bash
# Stops nginx and all .NET processes

nginx -s stop 2>/dev/null
pkill -f "dotnet.*SearchAPI" 2>/dev/null
echo "All services stopped"
```

---

## Kubernetes Preview

This X-Scale pattern maps directly to Kubernetes:

```yaml
# Deployment with 3 replicas (horizontal scaling)
apiVersion: apps/v1
kind: Deployment
metadata:
  name: searchapi
spec:
  replicas: 3  # X-Scale: 3 identical instances
  template:
    spec:
      containers:
      - name: searchapi
        image: searchapi:latest
        ports:
        - containerPort: 5137
---
# Service (load balancer)
apiVersion: v1
kind: Service
metadata:
  name: searchapi
spec:
  type: LoadBalancer
  selector:
    app: searchapi
  ports:
  - port: 80
    targetPort: 5137
```

See Module 8 documentation for complete Kubernetes deployment.

---

## Summary

**X-Scale Benefits**:
- ✅ High availability (redundancy)
- ✅ Load distribution (handle more traffic)
- ✅ Zero-downtime deployment (update one at a time)
- ✅ Horizontal scalability (add more instances easily)

**nginx Role**:
- Entry point for all requests
- Distributes load across backends
- Health monitoring and failover
- Production-ready reverse proxy

**Best Practices**:
1. Use round-robin for most scenarios
2. Monitor backend health
3. Set appropriate timeouts
4. Log requests for debugging
5. Test failover scenarios
