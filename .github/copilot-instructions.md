# PBIHoster Copilot Instructions

## Project Overview
- **Stack**: ASP.NET Core (.NET 10) Backend + Vue 3 (Vite/TypeScript) Frontend.
- **Database**: LiteDB (Embedded NoSQL).
- **Deployment**: Docker Compose with Caddy reverse proxy.
- **Auth**: JWT-based with roles (Admin, Editor, Viewer).
- **UI Library**: Carbon Design System for Vue.
- **Purpose**: Host and manage Power BI reports (https://learn.microsoft.com/en-us/power-bi/developer/embedded/embed-organization-app) where "App ownds the data" approach with user authentication and role-based access while also allowing public access to specifiied pages. 

## Architecture & Patterns

### Backend (`ReportTree.Server`)
- **Framework**: ASP.NET Core Web API.
- **API Style**: Hybrid of Minimal APIs (Auth endpoints in `Program.cs`) and Controllers.
- **Persistence**: Repository pattern. See `Persistance/LiteDbUserRepository.cs`.
  - **Database**: `LiteDB` (file-based, no external service required).
- **Auth**: JWT Bearer Authentication.
  - Logic: `Services/AuthService.cs` & `Security/TokenService.cs`.
  - Roles: `Admin`, `Editor`, `Viewer`.
- **Serving Frontend**: SPA fallback configured in `Program.cs` (`MapFallbackToFile("index.html")`).

### Frontend (`reporttree.client`)
- **Framework**: Vue 3 (Composition API) + TypeScript.
- **Build Tool**: Vite.
- **State Management**: Pinia (`stores/auth.ts`).
- **UI Library**: Carbon Design System (`@carbon/vue`). Source code for UI library is here : UI shell = "https://github.com/carbon-design-system/carbon-components-vue/tree/main/src/components/CvUIShell". Other components = "https://github.com/carbon-design-system/carbon-components-vue/tree/main/src/components".
- **Routing**: Vue Router (`router/index.ts`).

## Developer Workflows

### Running the App
- **Backend**: `dotnet watch run --project ReportTree.Server/ReportTree.Server.csproj`
  - Listens on port defined in `launchSettings.json` or `PORT` env var.
- **Frontend**: `cd reporttree.client && npm run dev`
  - Proxies `/api` requests to backend (configured in `vite.config.ts`).

### Building
- **Full Build**: `dotnet publish ReportTree.Server/ReportTree.Server.csproj`
  - The `.csproj` is configured to automatically build the frontend (`npm run build`) and copy assets to `wwwroot`.

### Docker
- **Local/Prod**: `docker-compose up -d`
  - `caddy` service handles SSL and reverse proxies to `pbihoster` (backend).
  - Backend runs on port 8080 internally.

## Coding Conventions
- **Vue Components**: Use `<script setup lang="ts">`.
- **Styles**: Use Carbon Design System components/classes where possible.
- **API Calls**: Frontend uses relative paths `/api/...` which are proxied.
- **Configuration**:
  - Backend: `appsettings.json` & Environment Variables.
  - Frontend: `vite.config.ts` for dev proxy.
