# PBIHoster Deployment Guide

This folder contains the necessary files to deploy the PBIHoster application using Docker.

## Production Architecture

In production, the Vue.js frontend is built and served as static files by the ASP.NET Core backend. Both run as a single application on one port.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed on your machine.
- [Docker Compose](https://docs.docker.com/compose/install/) (usually included with Docker Desktop).

## Quick Start

1.  Download the `docker-compose.yml` file from this folder to your local machine.
2.  Open a terminal in the directory where you saved the file.
3.  Run the following command to start the application:

    ```bash
    docker compose up -d
    ```

4.  The application will be available at [http://localhost:8080](http://localhost:8080).

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

