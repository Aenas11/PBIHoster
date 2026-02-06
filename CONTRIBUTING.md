# Contributing Guide

Welcome to PBIHoster! This guide covers development setup, code standards, and the pull request process.

## Getting Started

### Prerequisites

- **Git** 2.30+
- **.NET SDK** 10.0+
- **Node.js** 18+ with npm 9+
- **VS Code** (optional but recommended)

### Local Development Setup

1. **Clone the repository**

```bash
git clone https://github.com/aenas11/PBIHoster.git
cd PBIHoster
```

2. **Backend setup**

```bash
cd ReportTree.Server
dotnet restore
dotnet build
```

3. **Frontend setup**

```bash
cd ../reporttree.client
npm install
npm run build
```

4. **Run locally**

In separate terminals:

```bash
# Terminal 1: Backend (auto-reloads on changes)
cd ReportTree.Server
dotnet watch run
# Listens on http://localhost:5001 (or PORT env var)

# Terminal 2: Frontend (auto-reloads on changes)
cd reporttree.client
npm run dev
# Listens on http://localhost:5173, proxies API to backend
```

5. **Access the app**

- Open http://localhost:5173
- Register first user (automatically becomes Admin)
- Log in and explore

### Database Access

The local database is auto-created at `ReportTree.Server/reporttree.db` (LiteDB file).

To inspect it:
```bash
# Using LiteDB Studio (recommended)
# Download from: https://www.litedb.org/

# Or query via C# REPL:
dotnet repl
> var db = new LiteDB.LiteDatabase("filename=reporttree.db");
> db.GetCollection<dynamic>("AppUser").FindAll().ToList()
```

## Code Organization

### Backend (`ReportTree.Server/`)

```
ReportTree.Server/
├── Program.cs                    # Application startup, middleware, minimal API endpoints
├── Controllers/                  # REST API endpoints (resource-based)
│   ├── AdminController.cs
│   ├── PagesController.cs
│   ├── PowerBIController.cs
│   └── ...
├── Models/                       # Domain entities (AppUser, Page, etc.)
├── DTOs/                         # Data transfer objects for API requests/responses
├── Services/                     # Business logic and external integrations
│   ├── AuthService.cs
│   ├── PowerBIService.cs
│   ├── PageAuthorizationService.cs
│   └── ...
├── Persistance/                  # Repository pattern for data access
│   ├── Interfaces/
│   │   ├── IUserRepository.cs
│   │   ├── IPageRepository.cs
│   │   └── ...
│   └── Implementations/
│       ├── LiteDbUserRepository.cs
│       ├── LiteDbPageRepository.cs
│       └── ...
├── Security/                     # Security-related utilities
│   ├── TokenService.cs
│   ├── AuthenticationMiddleware.cs
│   └── ...
├── HealthChecks/                 # Health check implementations
└── ReportTree.Server.csproj      # Project file
```

### Frontend (`reporttree.client/`)

```
reporttree.client/
├── src/
│   ├── main.ts                   # Application entry point
│   ├── App.vue                   # Root component
│   ├── components/               # Reusable Vue components
│   │   ├── PageTree.vue          # Navigation tree
│   │   ├── PageEditor.vue        # Page editor modal
│   │   ├── PowerBIViewer.vue     # Report embedding
│   │   └── ...
│   ├── views/                    # Page-level containers
│   │   ├── HomeView.vue
│   │   ├── AdminView.vue
│   │   ├── PageView.vue
│   │   └── ...
│   ├── stores/                   # Pinia state management
│   │   ├── auth.ts               # Authentication state
│   │   ├── theme.ts              # Theme selection
│   │   ├── pages.ts              # Pages state
│   │   └── ...
│   ├── composables/              # Reusable logic hooks
│   │   ├── useAuth.ts
│   │   ├── usePages.ts
│   │   └── ...
│   ├── services/                 # API clients
│   │   ├── api.ts                # Base HTTP client
│   │   ├── authService.ts
│   │   ├── pageService.ts
│   │   └── ...
│   ├── router/                   # Vue Router configuration
│   ├── assets/                   # Static assets (images, fonts)
│   └── types/                    # TypeScript type definitions
├── vite.config.ts                # Vite configuration
├── tsconfig.json                 # TypeScript configuration
├── package.json                  # Dependencies and scripts
└── index.html                    # HTML entry point
```

## Code Standards

### Backend (C#/.NET)

**Language Version**: C# 12 (.NET 10)  
**Conventions**: Microsoft C# Coding Conventions

**Naming**:
```csharp
// Classes and methods: PascalCase
public class UserService { }
public async Task<User> GetUserByIdAsync(Guid id) { }

// Fields and properties: camelCase (private) or PascalCase (public)
private readonly ILogger<UserService> _logger;
public string Username { get; set; }

// Constants: UPPER_SNAKE_CASE (locally) or PascalCase (public)
private const int MaxLoginAttempts = 5;
public const string AdminRole = "Admin";
```

**Async/Await**:
```csharp
// ✅ All IO-bound operations use async
public async Task<User> GetUserAsync(Guid id)
{
    return await _repository.GetByIdAsync(id);
}

// ❌ Avoid blocking calls
public User GetUser(Guid id)
{
    return _repository.GetById(id).Result;  // BLOCKS!
}
```

**Error Handling**:
```csharp
// ✅ Use custom exceptions for domain errors
public class UnauthorizedException : Exception { }
public class PageNotFoundException : Exception { }

// ✅ Log exceptions with context
try
{
    await _service.DoSomethingAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process request for user {UserId}", userId);
    throw;
}

// ❌ Avoid silent failures
try { /* code */ }
catch { }  // Never do this!
```

**Dependency Injection**:
```csharp
// ✅ Constructor injection
public class PageService
{
    private readonly IPageRepository _repository;
    private readonly IAuditLogService _auditLog;
    
    public PageService(IPageRepository repository, IAuditLogService auditLog)
    {
        _repository = repository;
        _auditLog = auditLog;
    }
}

// Register in Program.cs
builder.Services.AddScoped<IPageRepository, LiteDbPageRepository>();
builder.Services.AddScoped<IPageService, PageService>();
```

**Authorization**:
```csharp
// ✅ Use attributes and custom policies
[Authorize(Roles = "Admin, Editor")]
public async Task<IActionResult> CreatePage([FromBody] CreatePageDto dto)
{
    // Implementation
}

// ✅ Service-level checks
public async Task<Page> GetPageAsync(Guid pageId, Guid userId)
{
    var page = await _repository.GetByIdAsync(pageId);
    
    if (!_authService.CanAccessPage(userId, page))
        throw new UnauthorizedException("Access denied");
    
    return page;
}
```

### Frontend (Vue 3 + TypeScript)

**Language**: TypeScript (strict mode enabled)  
**Framework**: Vue 3 Composition API with `<script setup>`  
**Conventions**: Airbnb/Standard with Vue 3 extensions

**Naming**:
```ts
// Components: PascalCase in both filename and usage
// PageTree.vue, PageEditor.vue
<template>
  <PageTree :pages="pages" />
</template>

// Composables: useXxx pattern
// useAuth.ts, usePages.ts
const { user, login, logout } = useAuth();

// Constants: UPPER_SNAKE_CASE
const MAX_PAGE_TITLE_LENGTH = 100;

// Variables and functions: camelCase
const userPreferences = { ... }
const formatDate = (date: Date) => { ... }
```

**Component Structure**:
```vue
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuth } from '@/composables/useAuth'

// Type definitions
interface PageData {
  id: string
  title: string
}

// Props & Emits
interface Props {
  pageId: string
  readonly?: boolean
}
const props = withDefaults(defineProps<Props>(), {
  readonly: false
})
const emit = defineEmits<{
  save: [page: PageData]
  cancel: []
}>()

// Composables
const { user, hasRole } = useAuth()

// Local state
const page = ref<PageData | null>(null)
const isLoading = ref(false)

// Computed
const canEdit = computed(() => hasRole('Admin', 'Editor'))

// Methods
const loadPage = async () => {
  isLoading.value = true
  try {
    page.value = await pageService.getPage(props.pageId)
  } catch (error) {
    console.error('Failed to load page:', error)
  } finally {
    isLoading.value = false
  }
}

const savePage = async () => {
  if (!page.value) return
  
  try {
    await pageService.updatePage(page.value)
    emit('save', page.value)
  } catch (error) {
    console.error('Failed to save page:', error)
  }
}

// Lifecycle
onMounted(() => {
  loadPage()
})
</script>

<template>
  <div class="page-editor">
    <template v-if="isLoading">
      <div>Loading...</div>
    </template>
    
    <template v-else-if="page && canEdit">
      <input v-model="page.title" type="text" placeholder="Page title" />
      <button @click="savePage">Save</button>
      <button @click="emit('cancel')">Cancel</button>
    </template>
    
    <template v-else-if="page">
      <div>{{ page.title }}</div>
    </template>
  </div>
</template>

<style scoped>
.page-editor {
  padding: 1rem;
  border: 1px solid #ccc;
  border-radius: 4px;
}
</style>
```

**Error Handling**:
```ts
// ✅ Use try-catch for async operations
const saveSettings = async () => {
  try {
    await settingsService.update(settings.value)
    showSuccessNotification('Settings saved')
  } catch (error) {
    console.error('Failed to save settings:', error)
    showErrorNotification('Failed to save settings. Please try again.')
  }
}

// ✅ Type errors for better debugging
interface ApiError {
  message: string
  code?: string
  details?: unknown
}

const handleError = (error: unknown) => {
  if (error instanceof AxiosError<ApiError>) {
    console.error(`API Error [${error.response?.data.code}]: ${error.response?.data.message}`)
  } else if (error instanceof Error) {
    console.error(`Error: ${error.message}`)
  } else {
    console.error('Unknown error:', error)
  }
}
```

**Composables**:
```ts
// composables/useAuth.ts
import { ref, computed } from 'vue'
import { authService } from '@/services/authService'

export const useAuth = () => {
  const user = ref<User | null>(null)
  const isLoading = ref(false)

  const hasRole = (role: string, ...otherRoles: string[]) => {
    if (!user.value) return false
    const roles = [role, ...otherRoles]
    return roles.some(r => user.value!.roles.includes(r))
  }

  const login = async (username: string, password: string) => {
    isLoading.value = true
    try {
      const response = await authService.login(username, password)
      localStorage.setItem('token', response.accessToken)
      user.value = response.user
    } finally {
      isLoading.value = false
    }
  }

  const logout = () => {
    localStorage.removeItem('token')
    user.value = null
  }

  return {
    user: computed(() => user.value),
    isLoading: computed(() => isLoading.value),
    hasRole,
    login,
    logout
  }
}
```

## Testing

### Backend Tests

Tests located in `ReportTree.Server.Tests/`

```bash
# Run all tests
dotnet test ReportTree.Server.Tests/ReportTree.Server.Tests.csproj

# Run specific test
dotnet test --filter "ClassName=AuthServiceTests"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**Test Structure**:
```csharp
public class AuthServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _userRepository = new MockUserRepository();
        _tokenService = new MockTokenService();
        _service = new AuthService(_userRepository, _tokenService);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var user = new AppUser { Username = "john", PasswordHash = BCrypt.HashPassword("pass") };
        _userRepository.Add(user);

        // Act
        var result = await _service.LoginAsync("john", "pass", "127.0.0.1");

        // Assert
        Assert.NotNull(result.AccessToken);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ThrowsException()
    {
        // Arrange
        var user = new AppUser { Username = "john", PasswordHash = BCrypt.HashPassword("pass") };
        _userRepository.Add(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _service.LoginAsync("john", "wrong", "127.0.0.1")
        );
    }
}
```

### Frontend Tests

Tests located alongside components

```bash
# Run all tests
npm run test

# Run in watch mode
npm run test:watch

# Run with coverage
npm run test:coverage
```

**Test Example**:
```ts
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import PageEditor from './PageEditor.vue'

describe('PageEditor', () => {
  it('renders page title', () => {
    const wrapper = mount(PageEditor, {
      props: {
        pageId: '123'
      }
    })
    
    expect(wrapper.find('input').exists()).toBe(true)
  })

  it('emits save event with page data', async () => {
    const wrapper = mount(PageEditor, {
      props: { pageId: '123' }
    })

    await wrapper.find('input').setValue('New Title')
    await wrapper.find('button[type="submit"]').trigger('click')

    expect(wrapper.emitted('save')).toBeTruthy()
  })
})
```

## Git Workflow

### Branching Strategy

- **main**: Production-ready code (tagged with versions)
- **develop**: Integration branch for features
- **feature/\***: Feature development (`feature/add-oidc-auth`, `feature/powerbi-rls`)
- **bugfix/\***: Bug fixes (`bugfix/page-sorting`, `bugfix/jwt-expiry`)
- **hotfix/\***: Critical production fixes (`hotfix/security-patch`)

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**: feat, fix, docs, style, refactor, perf, test, chore  
**Scopes**: auth, pages, powerbi, admin, api, frontend, security, etc.

**Examples**:
```
feat(powerbi): add RLS support for report embedding

The backend now accepts RLSRoles parameter when generating embed tokens.
Frontend UI updated to configure RLS rules per report component.

Closes #42
```

```
fix(auth): prevent account lockout race condition

Use atomic update to increment failed attempts, preventing concurrent
requests from creating duplicate lockouts.

Fixes #89
```

### Pull Request Process

1. **Create feature branch**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/your-feature
   ```

2. **Make changes**
   - Follow code standards above
   - Add tests for new functionality
   - Update documentation if needed

3. **Test locally**
   ```bash
   dotnet test  # Backend
   npm run test # Frontend
   npm run lint
   ```

4. **Commit and push**
   ```bash
   git add .
   git commit -m "feat(scope): description"
   git push origin feature/your-feature
   ```

5. **Create Pull Request**
   - Use PR template (auto-populated)
   - Link related issues: `Closes #123`
   - Ensure all CI checks pass
   - Request review from maintainers

6. **Address review feedback**
   ```bash
   git add .
   git commit -m "review: address feedback on X"
   git push
   ```

7. **Merge**
   - Rebase and merge (keeps history clean)
   - Delete feature branch

## Documentation

All documentation uses Markdown and follows these standards:

- **README.md**: High-level overview with quick start
- **CONTRIBUTING.md**: Development guide (this file)
- **ARCHITECTURE.md**: System design and patterns
- **API.md**: REST API endpoint documentation
- **DATABASE.md**: Data model and schema
- **SECURITY.md**: Security implementation details
- **DEPLOYMENT.md**: Production deployment guide
- **ROADMAP.md**: Feature plans and implementation status
- Code comments: Explain "why", not "what"

## Common Tasks

### Adding a New API Endpoint

1. **Create DTO** (if needed)
   ```csharp
   // DTOs/MyDtos.cs
   public class CreateMyResourceDto
   {
       [Required]
       public string Name { get; set; }
   }
   ```

2. **Create Controller** or add to existing
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class MyResourceController : ControllerBase
   {
       [HttpPost]
       [Authorize(Roles = "Admin, Editor")]
       public async Task<IActionResult> Create([FromBody] CreateMyResourceDto dto)
       {
           // Implementation
       }
   }
   ```

3. **Update API documentation** (API.md)

4. **Add tests**

### Adding a New Page Component

1. **Create Vue component**
   ```vue
   <!-- src/components/MyComponent.vue -->
   <script setup lang="ts">
   // Implementation
   </script>
   <template>
     <!-- Template -->
   </template>
   <style scoped>
   /* Styles */
   </style>
   ```

2. **Add to router** if it's a page view

3. **Add composable** for shared logic (if needed)

4. **Update documentation** (README.md)

5. **Add tests**

### Running the Full Build

```bash
# Backend
cd ReportTree.Server
dotnet publish -c Release

# Frontend (included in above via npm)
cd ../reporttree.client
npm run build

# Docker
docker build -t pbihoster:local .

# Test Docker locally
docker run -p 8080:8080 pbihoster:local
# Visit http://localhost:8080
```

## Troubleshooting

**Port already in use**
```bash
# Kill process on port 5001 (backend)
lsof -ti:5001 | xargs kill -9

# Or use different port
PORT=5002 dotnet watch run
```

**Database locked**
```bash
# Restart the application to release lock
# Or delete the database and let it reinitialize
rm ReportTree.Server/reporttree.db
```

**npm install fails**
```bash
# Clear cache
npm cache clean --force

# Delete node_modules
rm -rf reporttree.client/node_modules

# Reinstall
cd reporttree.client
npm install
```

**JWT token issues in development**
```bash
# Set same JWT_KEY in both:
# .env file (for Docker)
# Environment variable (for local development)
export JWT_KEY=$(openssl rand -base64 32)
```

---

## Questions?

- Check [ARCHITECTURE.md](ARCHITECTURE.md) for system design questions
- Check [API.md](API.md) for endpoint questions
- Check [DATABASE.md](DATABASE.md) for data model questions
- Open a GitHub issue for bugs or suggestions
- Start a discussion for design questions

Thank you for contributing! 🎉
