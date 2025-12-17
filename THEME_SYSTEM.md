# Theme System Documentation

## Overview

The PBIHoster application now includes a comprehensive theme system based on Carbon Design System themes, with support for custom corporate themes.

## Features

- **Built-in Themes**: White, Gray 10, Gray 90, Gray 100 from Carbon Design System
- **Custom Themes**: Enterprise customers can create and apply custom corporate themes
- **Theme Persistence**: Selected theme is saved in localStorage
- **Role-Based Management**: Admin and Editor roles can create/manage custom themes
- **Organization-Specific**: Themes can be scoped to specific organizations

## User Guide

### Switching Themes

1. Click the theme switcher icon in the header (top right)
2. Select from available themes:
   - White (default light theme)
   - Gray 10 (light theme with subtle gray background)
   - Gray 90 (dark theme)
   - Gray 100 (darker theme)
   - Any custom themes available to your organization

### Creating Custom Themes (Admin/Editor Only)

1. Navigate to the Theme Manager (accessible from admin panel or tools)
2. Click "Create Custom Theme"
3. Fill in the required fields:
   - **Theme Name**: A descriptive name for your theme
   - **Organization ID**: (Optional) Leave empty for a global theme, or specify an organization ID
   - **Theme Tokens**: JSON object defining color tokens

4. Use the sample JSON as a starting point
5. Click "Save" to create the theme

### Theme Token Structure

Custom themes use Carbon Design System color tokens. Here's a minimal example:

```json
{
  "background": "#ffffff",
  "interactive-01": "#0f62fe",
  "interactive-02": "#393939",
  "ui-background": "#ffffff",
  "ui-01": "#f4f4f4",
  "text-01": "#161616",
  "text-02": "#525252",
  "link-01": "#0f62fe"
}
```

For a complete list of available tokens, visit:
https://carbondesignsystem.com/elements/color/tokens/

## Developer Guide

### Frontend Architecture

#### Theme Store (`src/stores/theme.ts`)

The theme store manages:
- Current theme selection
- Custom theme data
- Theme application to the DOM
- API calls for theme CRUD operations

Key methods:
- `setTheme(theme)`: Switch to a built-in theme
- `setCustomTheme(theme)`: Apply a custom theme
- `loadCustomThemes()`: Fetch custom themes from API
- `saveCustomTheme(theme)`: Create/update a custom theme
- `deleteCustomTheme(id)`: Delete a custom theme
- `initTheme()`: Initialize theme on app start

#### Components

1. **ThemeSwitcher.vue**: Header dropdown for theme selection
2. **ThemeManager.vue**: Admin interface for managing custom themes

### Backend Architecture

#### Models (`Models/CustomTheme.cs`)

```csharp
public class CustomTheme
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string> Tokens { get; set; }
    public bool IsCustom { get; set; }
    public string? OrganizationId { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### API Endpoints (`Controllers/ThemesController.cs`)

- `GET /api/themes`: Get all themes (optionally filtered by organizationId)
- `GET /api/themes/{id}`: Get a specific theme
- `POST /api/themes`: Create a new custom theme (Admin/Editor only)
- `PUT /api/themes/{id}`: Update a theme (Admin/Editor only)
- `DELETE /api/themes/{id}`: Delete a theme (Admin/Editor only)

#### Repository (`Persistance/LiteDbThemeRepository.cs`)

LiteDB-based persistence with methods for CRUD operations.

### Adding Custom Theme Support to Your Views

Themes are automatically applied at the document root level using CSS variables. To use theme colors in your components:

```vue
<style>
.my-component {
  background-color: var(--cds-ui-background);
  color: var(--cds-text-01);
}

.my-button {
  background-color: var(--cds-interactive-01);
  color: var(--cds-text-04);
}
</style>
```

### Theme Initialization

Themes are initialized in `App.vue` on mount:

```typescript
onMounted(async () => {
  // Initialize theme
  themeStore.initTheme()
  
  // Load custom themes if user is authenticated
  if (authStore.isAuthenticated) {
    await themeStore.loadCustomThemes()
  }
})
```

## Technical Details

### How Themes Work

1. **Built-in Themes**: Carbon Design System provides four themes (white, g10, g90, g100) with predefined color tokens
2. **CSS Variables**: Theme tokens are applied as CSS custom properties (e.g., `--cds-background`)
3. **Theme Classes**: The appropriate theme class (e.g., `cds--g90`) is added to `document.documentElement`
4. **Custom Themes**: Override CSS variables with custom values while maintaining the structure

### Theme Storage

- **Current Selection**: Stored in localStorage as `theme` key
- **Custom Theme Data**: Stored in localStorage as `customTheme` key
- **Backend Persistence**: Custom themes are stored in LiteDB

### Security

- Authentication required for all theme API endpoints
- Only Admin and Editor roles can create/update/delete themes
- Users can only delete themes they created (unless Admin)
- Organization-scoped themes ensure data isolation

## Best Practices

### For Theme Creators

1. **Start with an Existing Theme**: Copy tokens from white/g10/g90/g100 and modify
2. **Test Accessibility**: Ensure sufficient contrast ratios (WCAG AA minimum)
3. **Use Semantic Naming**: Token names should be semantic (e.g., `interactive-01`) not descriptive (e.g., `blue`)
4. **Test All Components**: Preview your theme across all views before publishing
5. **Document Your Theme**: Include a description of when to use your custom theme

### For Developers

1. **Always Use CSS Variables**: Use `var(--cds-*)` instead of hardcoded colors
2. **Support All Themes**: Test your components with all built-in themes
3. **Don't Override Theme Variables**: Let the theme system manage colors
4. **Use Carbon Components**: Leverage `@carbon/vue` components which are theme-aware

## Troubleshooting

### Theme Not Applying

1. Check browser console for errors
2. Verify theme is saved in localStorage
3. Clear browser cache and reload
4. Check that CSS variables are being set on `document.documentElement`

### Custom Theme Not Saving

1. Verify user has Admin or Editor role
2. Check network tab for API errors
3. Validate JSON format of theme tokens
4. Ensure authentication token is valid

### Colors Look Wrong

1. Verify all required tokens are defined
2. Check token names match Carbon conventions
3. Test with a built-in theme to isolate the issue
4. Review browser console for CSS warnings

## Future Enhancements

Potential improvements:
- Theme preview before applying
- Import/export themes as JSON
- Theme versioning and history
- Organization theme library
- Real-time theme sync across tabs
- Dark mode detection and auto-switching
- Theme marketplace for sharing themes
