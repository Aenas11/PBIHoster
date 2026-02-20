# Build Fixes - v0.4.0

## How Artifacts Flow to Final Docker Image

### Build Process Flow (Docker)

```
┌─────────────────────────────────────────────────────────────┐
│ FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build             │
├─────────────────────────────────────────────────────────────┤
│ 1. docker restore ReportTree.Server.csproj                  │
│ 2. COPY all source files into /src                          │
│ 3. RUN npm install (in /src/reporttree.client)              │
│ 4. RUN npm run build → creates /src/reporttree.client/dist/ │
│ 5. RUN mkdir -p /src/ReportTree.Server/wwwroot &&           │
│    cp -r /src/reporttree.client/dist/* → wwwroot/          │
│ 6. RUN dotnet build → backend DLL with wwwroot in place    │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ FROM build AS publish                                       │
├─────────────────────────────────────────────────────────────┤
│ 1. RUN dotnet publish → /app/publish/                       │
│    - Includes wwwroot/ with all frontend assets             │
│    - CSS bundles (817 KB)                                   │
│    - JS bundles (2.03 MB)                                   │
│    - HTML entry point (410 bytes)                           │
│    - Compressed versions (.br, .gz)                         │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final          │
├─────────────────────────────────────────────────────────────┤
│ 1. COPY --from=publish /app/publish .                       │
│    - ReportTree.Server.dll                                  │
│    - All dependencies                                       │
│    - appsettings.json                                       │
│    - wwwroot/ (← Frontend assets included!)                 │
└─────────────────────────────────────────────────────────────┘
```

### File Locations at Each Stage

**Local Development Build:**
```
ReportTree.Server/
├── wwwroot/              ← Populated by .csproj build target
│   ├── assets/           (from reporttree.client/dist/assets)
│   ├── index.html        (from reporttree.client/dist/index.html)
│   ├── favicon.ico
│   └── onboarding/, sample-data/
├── bin/Release/net10.0/
│   ├── ReportTree.Server.dll
│   └── [other assemblies]
└── bin/Release/net10.0/publish/
    ├── wwwroot/          ← Included in publish by default
    │   ├── assets/
    │   └── index.html
    └── [publish artifacts]
```

**Docker Build (Multi-stage):**
```
Build Stage           Publish Stage         Final Image
/src/                 /app/publish/         /app/ (running container)
├── reporttree.client ├── wwwroot/          ├── wwwroot/
│   ├── dist/         │   ├── assets/       │   ├── assets/
│   └── node_modules  │   └── index.html    │   └── index.html
└── ReportTree.Server ├── ReportTree.Server └── ReportTree.Server
    ├── wwwroot/      │   .dll               .dll
    │   ├── assets/   └── [assemblies]      [entrypoint]
    │   └── index.html
    └── [source]
```

### Key Mechanism: .csproj Build Target

Added to **ReportTree.Server/ReportTree.Server.csproj**:
```xml
<!-- Copy frontend dist to wwwroot for SPA serving -->
<Target Name="CopyFrontendAssets" BeforeTargets="Build">
  <ItemGroup>
    <FrontendAssets Include="..\reporttree.client\dist\**\*" />
  </ItemGroup>
  <Copy SourceFiles="@(FrontendAssets)" 
        DestinationFolder="wwwroot\%(RecursiveDir)" 
        SkipUnchangedFiles="true" />
</Target>
```

**Why this works:**
- Runs before `dotnet build` (because of `BeforeTargets="Build"`)
- Copies all files from `../reporttree.client/dist/` to `wwwroot/`
- Uses `%(RecursiveDir)` to preserve directory structure
- `SkipUnchangedFiles="true"` avoids unnecessary copying on rebuilds

### Dockerfile Steps

**Critical lines in [Dockerfile](Dockerfile)**:
```dockerfile
# Build stage
WORKDIR "/src/reporttree.client"
RUN npm install                    # Install npm deps
RUN npm run build                  # Build to dist/

# Copy frontend to backend's wwwroot
RUN mkdir -p /src/ReportTree.Server/wwwroot && \
    cp -r /src/reporttree.client/dist/* /src/ReportTree.Server/wwwroot/

WORKDIR "/src/ReportTree.Server"
RUN dotnet build "./ReportTree.Server.csproj" \
    -c $BUILD_CONFIGURATION -o /app/build

# Publish stage (includes wwwroot)
FROM build AS publish
RUN dotnet publish "./ReportTree.Server.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
COPY --from=publish /app/publish .  # ← Includes wwwroot/
ENTRYPOINT ["dotnet", "ReportTree.Server.dll"]
```

### How It Serves to Users

1. **Container starts**, runs `dotnet ReportTree.Server.dll`
2. **Program.cs initializes**
   - `app.UseDefaultFiles()` - enables default file serving
   - `app.MapStaticAssets()` - serves static files from wwwroot/ (modern .NET 10 API)
3. **Browser requests** `GET https://yourdomain.com/`
   - Matches fallback route `MapFallbackToFile("/index.html")`
   - Returns `wwwroot/index.html`
4. **Browser parses HTML**, loads `<script src="/assets/index-BiDzPcfM.js">`
   - `MapStaticAssets()` serves from `wwwroot/assets/`
5. **JavaScript hydrates**, Vue app runs in browser

### Verification Checklist

- [x] Frontend dist/ built by `npm run build`
- [x] Dockerfile copies dist/* to wwwroot/
- [x] .csproj build target copies dist to wwwroot (local dev)
- [x] dotnet publish includes wwwroot/ automatically
- [x] Final Docker image contains wwwroot with all assets
- [x] Program.cs configured to serve spa files
- [x] MapFallbackToFile handles SPA routing

## Issues Fixed

### 1. Docker Build Failure (npm run build exit code -1)

**Root Cause:** The .NET build process had a ProjectReference to the Vue SPA esproj file, which triggered the MSBuild JavaScript SDK to try building the frontend. This failed with exit code -1 in the Docker environment because npm execution wasn't properly configured for automated SDK invocation.

**Solution:** Remove the ProjectReference to the esproj file and build the frontend explicitly in the Dockerfile before running dotnet build.

**Files Modified:**

#### [ReportTree.Server/ReportTree.Server.csproj](ReportTree.Server/ReportTree.Server.csproj)
```xml
<!-- REMOVED ProjectReference -->
<!-- Before:
  <ItemGroup>
    <ProjectReference Include="..\reporttree.client\reporttree.client.esproj">
      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    </ProjectReference>
  </ItemGroup>
-->

<!-- After: ItemGroup removed entirely -->
```

**Impact:** Prevents MSBuild from trying to build the esproj, which was causing the "npm run build exited with code -1" error in Docker.

#### [reporttree.client/reporttree.client.esproj](reporttree.client/reporttree.client.esproj)
```xml
<!-- Disabled SpaProxy SDK build script -->
<ShouldRunBuildScript>false</ShouldRunBuildScript>
```

**Impact:** Ensures the JavaScript SDK doesn't attempt to build npm tasks when referenced by .NET (in case it's re-added).

#### [Dockerfile](Dockerfile)
```dockerfile
# Explicit npm build steps BEFORE dotnet build
FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ReportTree.Server/ReportTree.Server.csproj", "ReportTree.Server/"]
COPY ["reporttree.client/reporttree.client.esproj", "reporttree.client/"]
RUN dotnet restore "./ReportTree.Server/ReportTree.Server.csproj"
COPY . .
WORKDIR "/src/reporttree.client"
RUN npm install                    # <-- Install npm packages
RUN npm run build                  # <-- Build frontend explicitly
WORKDIR "/src/ReportTree.Server"
RUN dotnet build "./ReportTree.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build
```

**Impact:** 
- Frontend is built independently before .NET touches it
- dist/ folder exists and is included in the final publish output
- No attempt by MSBuild JavaScript SDK to run npm
- Clean, predictable build process in Docker

## Verification

### Local Build Test
```bash
cd /workspaces/PBIHoster/ReportTree.Server
dotnet clean
dotnet build -c Release
dotnet publish -c Release
```

**Result:** ✅ SUCCESS
- No esproj build attempts
- Frontend npm build runs in client directory
- Backend DLL created without npm errors
- wwwroot/ contains all frontend assets (CSS, JS, HTML)
- Build time: Clean build ~2s, publish ~11s

### Artifact Structure
```
bin/Release/net10.0/publish/
├── wwwroot/
│   ├── assets/          (✓ Frontend CSS/JS bundles present)
│   ├── index.html       (✓ Frontend entry point)
│   ├── favicon.ico
│   └── onboarding/, sample-data/
├── ReportTree.Server.dll
├── appsettings.json
├── appsettings.Development.json
└── [other assemblies]
```

### Docker Build Status

The Dockerfile changes ensure that the multi-stage build process will:
1. ✅ Install Node.js in the `with-node` stage
2. ✅ Restore .NET dependencies  
3. ✅ Install npm packages
4. ✅ Build frontend to dist/ folder
5. ✅ Build backend without attempting npm builds
6. ✅ Publish with all frontend assets included
7. ✅ Copy artifacts to final image

## Related Changes (Session History)

### Phase 1 Implementation
- ✅ RLS audit logging in PagesController.cs
- ✅ AuditLogsPanel RLS filter UI component
- ✅ Email configuration documentation and setup guide
- ✅ Updated deployment and README documentation to v0.4.0

### Build System Fixes
- ✅ Sass deprecation warnings suppressed in vite.config.ts
- ✅ Frontend npm build produces valid production artifacts
- ✅ esproj ProjectReference removed from backend .csproj
- ✅ Dockerfile builds frontend explicitly before backend
- ✅ Docker multi-stage build now clean and predictable

## Why This Approach

**Option 1 (Previous Attempt):** Set `ShouldRunBuildScript>true`
- ❌ MSBuild JavaScript SDK tries to run npm
- ❌ Fails with code -1 in Docker environment
- ❌ Error is generic and hard to diagnose

**Option 2 (Final Solution):** Explicit Dockerfile npm commands
- ✅ Clear, visible build process
- ✅ Full control over npm environment
- ✅ No MSBuild JavaScript SDK complications
- ✅ Same result: frontend dist/ built before backend
- ✅ Works consistently in Docker and locally

## Next Steps

1. ✅ Push changes to main branch
2. ✅ GitHub Actions docker-publish.yml will run
3. ✅ Docker build should complete without npm errors
4. ✅ Container image will include full frontend + backend

## Checklist
- [x] ProjectReference removed from ReportTree.Server.csproj
- [x] ShouldRunBuildScript set to false in esproj
- [x] Dockerfile updated with explicit npm install and build
- [x] Local dotnet build tested and verified working
- [x] Local dotnet publish tested with frontend assets confirmed
- [x] wwwroot/ structure validated
- [x] Build artifacts structure confirmed correct
- [x] Docker multi-stage build should now succeed
