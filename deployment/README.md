# PBIHoster Deployment Guide

This folder contains the necessary files to deploy the PBIHoster application using Docker.

## Production Architecture

In production, the Vue.js frontend is built and served as static files by the ASP.NET Core backend. Both run as a single application on one port.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed on your machine.
- [Docker Compose](https://docs.docker.com/compose/install/) (usually included with Docker Desktop).

## Quick Start

### With HTTPS (Recommended for Production)

1.  Download `docker-compose.yml` and `Caddyfile` from this folder
2.  Edit `Caddyfile` and replace `yourdomain.com` with your actual domain
3.  Ensure your domain's DNS points to your server's IP address
4.  Run:

    ```bash
    docker compose up -d
    ```

5.  The application will be available at `https://yourdomain.com` (Caddy handles automatic SSL)

### Without HTTPS (Testing/Local Only)

1.  Download `docker-compose.http.yml` from this folder
2.  Run:

    ```bash
    docker compose -f docker-compose.http.yml up -d
    ```

3.  The application will be available at [http://localhost:8080](http://localhost:8080)

## Port Configuration

### Docker Compose Port Mapping
Edit `docker-compose.yml` to map different ports:

```yaml
ports:
  - "80:8080"      # Map host port 80 → Container port 8080
  - "443:8080"     # If using reverse proxy for HTTPS
```

To change the internal port the app listens on:
```yaml
environment:
  - PORT=3000      # App listens on port 3000 inside container
ports:
  - "80:3000"      # Map host port 80 to container port 3000
```

### Direct Run (No Docker)

```bash
# Development
cd ReportTree.Server
dotnet run

# Production
cd ReportTree.Server
dotnet publish -c Release -o ./publish
cd publish
PORT=8080 ./ReportTree.Server
```

## Scaling & High Availability

### ⚠️ Important: LiteDB Limitation

PBIHoster currently uses **LiteDB**, a file-based embedded database that **does not support concurrent access from multiple processes**. This affects scaling strategies.

### Single-Instance Deployment (Current - Recommended)

For production, deploy as a single container/instance:

```yaml
# docker-compose.yml
services:
  pbihoster:
    image: ghcr.io/aenas11/pbihoster:latest
    replicas: 1  # ⚠️ MUST be 1 with LiteDB
    volumes:
      - pbihoster_data:/data  # Persistent storage for database
    environment:
      - JWT_KEY=...
      - CORS_ORIGIN_1=...
      - POWERBI_TENANT_ID=...
      - POWERBI_CLIENT_ID=...
      - POWERBI_CLIENT_SECRET=...
```

**High Availability Strategy with Single Instance**:

1. **Persistent Volume**: Database stored on reliable shared storage (EBS, GCP Persistent Disk, etc.)
2. **Automated Backups**: Daily snapshots to object storage (S3, GCS, Azure Blob)
3. **Health Checks**: Liveness (`/health`) and readiness (`/ready`) probes
4. **Rapid Recovery**: Container auto-restart on failure (within 2-5 minutes)
5. **Monitoring**: Alert on pod failures for manual intervention if needed

### Multiple Replicas (Future - Requires Database Migration)

To scale horizontally with multiple replicas, you must migrate to a network-accessible database:

```yaml
# Future architecture (v1.0+)
services:
  postgres:  # or MySQL, managed RDS, CloudSQL, etc.
    image: postgres:15-alpine
    environment:
      - POSTGRES_DB=pbihoster
      - POSTGRES_PASSWORD=...
    
  pbihoster:
    image: ghcr.io/aenas11/pbihoster:latest
    replicas: 3  # Can scale horizontally
    environment:
      - DATABASE_URL=postgresql://postgres:5432/pbihoster
      - DATABASE_TYPE=postgresql
      - JWT_KEY=...
      - CORS_ORIGIN_1=...
```

**When to migrate to PostgreSQL/MySQL**:
- Need true high availability (99.95%+ uptime SLA)
- Expecting 100+ concurrent users
- Multiple geographic regions
- Multi-tenant deployment

**Migration effort**: Medium (Phase 1 of future roadmap)

### Load Balancing Strategy

Even with single instance, you can use load balancers for other benefits:

```yaml
# Option 1: Docker Compose with external load balancer
caddy:  # Acts as load balancer
  image: caddy:latest
  ports:
    - "80:80"
    - "443:443"
  
pbihoster:
  image: ghcr.io/aenas11/pbihoster:latest
  ports:
    - "8080"  # Not exposed externally
  
# Caddy health checks pbihoster and serves requests
# If container fails, Caddy returns 5xx until it recovers
```

```yaml
# Option 2: Kubernetes with single replica + auto-restart
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pbihoster
spec:
  replicas: 1  # ⚠️ Keep at 1 with LiteDB
  selector:
    matchLabels:
      app: pbihoster
  template:
    metadata:
      labels:
        app: pbihoster
    spec:
      containers:
      - name: pbihoster
        image: ghcr.io/aenas11/pbihoster:latest
        ports:
        - containerPort: 8080
        volumeMounts:
        - name: data
          mountPath: /data
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: pbihoster-data
          accessMode: ReadWriteOnce  # CRITICAL for LiteDB
```

### Database Migration Roadmap

| Version | Database | Max Replicas | HA Capability |
|---------|----------|--------------|---------------|
| v0.3 (Current) | LiteDB | 1 | Restart-based |
| v1.0 (Planned) | PostgreSQL/MySQL | 3+ | True multi-replica |

**Why the delay?**
- LiteDB great for getting started (zero setup)
- Full-featured for single-instance deployments
- Database migration is non-trivial
- Sufficient for most use cases until high scale

**Current recommendation**: Deploy single instance with robust backups and monitoring.

## HTTPS Configuration

**The application does NOT handle HTTPS directly.** Use a reverse proxy in front for SSL/TLS termination.

### Recommended: Caddy (Automatic HTTPS)

Create `docker-compose.caddy.yml`:
```yaml
services:
  caddy:
    image: caddy:latest
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
      - caddy_data:/data
    
  pbihoster:
    image: ghcr.io/aenas11/pbihoster:main
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - PORT=8080
    # No ports exposed - only accessible via Caddy

volumes:
  caddy_data:
```

Create `Caddyfile`:
```
yourdomain.com {
    reverse_proxy pbihoster:8080
}
```

Run: `docker compose -f docker-compose.caddy.yml up -d`

### Alternative: nginx

Create `nginx.conf`:
```nginx
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl;
    server_name yourdomain.com;
    
    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    
    location / {
        proxy_pass http://pbihoster:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Add to `docker-compose.yml`:
```yaml
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - pbihoster
```

## How It Works

- **Frontend**: Vue.js app is built during Docker image creation and output to `wwwroot/`
- **Backend**: ASP.NET Core serves static files from `wwwroot/` and API routes from `/api`
- **Routing**: All non-API routes fallback to `index.html` for SPA routing
- **Single Port**: Everything runs on one configurable port

## Updating the Application

To update to the latest version of the application:

1.  Pull the latest image:

    ```bash
    docker compose pull
    ```

2.  Restart the container:

    ```bash
    docker compose up -d
    ```

## Troubleshooting

If you encounter any issues, please check the [Issues](https://github.com/Aenas11/PBIHoster/issues) page on our GitHub repository.

