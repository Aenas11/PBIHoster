# Dynamic Layout System - Implementation Summary

## Overview
Successfully refactored the dynamic layout system to be more extensible, type-safe, and maintainable.

## ⚠️ Critical Fix: Layout Data Preservation

### Issue
Components would disappear or reset when moved/resized because `grid-layout-plus`'s v-model updates only include position/size data, losing the `componentType` and `componentConfig` properties needed to render components.

### Solution
Implemented `onLayoutUpdated()` handler that:
1. Captures the layout state before grid-layout-plus updates it
2. Preserves component-specific data (`componentType`, `componentConfig`, `metadata`)
3. Merges position updates from grid-layout-plus with preserved component data
4. Prevents infinite update loops by using `@layout-updated` event instead of computed setters

This ensures components maintain their identity and configuration during drag/resize operations.

## Key Improvements Implemented

### 1. **Type System** (`src/types/`)
- **`components.ts`**: Defined strong TypeScript interfaces for component definitions, props, and configuration
  - `ComponentDefinition`: Registry metadata for each component type
  - `DashboardComponentProps`: Standard props interface all components must implement
  - `ComponentConfigProps`: Props for configuration components
  
- **`layout.ts`**: Extended grid layout types
  - `GridItemWithComponent`: Extends base grid item with component-specific data
  - `CreateGridItemInput`: Helper type for creating new panels
  - Separated layout concerns (position, size) from component concerns (type, config)

### 2. **Component Registry System** (`src/plugins/componentRegistry.ts`)
- Centralized registry for all dashboard component types
- Singleton pattern with global instance
- Methods: `register`, `registerAll`, `get`, `has`, `getAll`, `getTypes`
- Type-safe component resolution

### 3. **Composable Enhancements** (`src/composables/`)
- **`useComponentRegistry.ts`**: Vue composable wrapper for registry access
- **`useGridLayout.ts`** refactored:
  - `addPanel(componentType, customConfig)`: Now requires component type and uses registry
  - `updatePanelConfig()`: Update component config after creation
  - `getPanel()`: Retrieve specific panel by ID
  - **`onLayoutUpdated(newLayout)`**: Critical handler that preserves component data during drag/resize
    - Creates a map of existing component data before update
    - Merges position changes with preserved `componentType`, `componentConfig`, and `metadata`
    - Prevents data loss during grid layout operations
  - `updateLayout()`: Legacy method kept for compatibility
  - Proper TypeScript types throughout
  - Console logging for debugging data flow

### 4. **Component Updates**
- **`SimpleHtmlComponent.vue`**: Updated to use `DashboardComponentProps`
  - Receives typed `config` prop instead of generic `model`
  - Added proper styling and overflow handling
  
- **`SimpleHtmlComponentConfigure.vue`**: Fully implemented configuration UI
  - Uses `ComponentConfigProps` interface
  - Live preview of HTML content
  - Two-way binding with `v-model`
  - Carbon Design System components
  
- **`ErrorComponent.vue`**: New fallback component
  - Displays when component type not found
  - Helpful error messages for debugging
  - Proper styling matching Carbon Design System

### 5. **Component Registration** (`src/config/components.ts`)
- Central file for registering all dashboard components
- Easy to extend with new component types
- Metadata includes:
  - Display name and description
  - Default and minimum dimensions
  - Default configuration
  - Component and configuration component references

### 6. **PageView Refactoring** (`src/views/PageView.vue`)
- Removed hardcoded component registry
- Uses `useComponentRegistry()` for dynamic component resolution
- `resolveComponent()`: Type-safe component resolution with fallback
- Proper prop passing to dynamic components
- Type-safe template iteration with explicit type casting
- **v-model + @layout-updated pattern**: Uses both v-model for two-way binding and event handler for data preservation
- `:vertical-compact="false"`: Disabled to prevent automatic repositioning during drag operations

### 7. **ToolsPanel Enhancement** (`src/components/ToolsPanel.vue`)
- Dynamic button generation from registry
- Shows all registered component types
- Each component type gets its own "Add" button
- Organized into sections (Add Components, Layout Actions, Panel Count)

### 8. **Application Bootstrap** (`src/main.ts`)
- Components registered before app creation
- Console logging confirms registration

## Architecture Benefits

### Extensibility
- **Add new components**: Simply create component + config component, add to `components.ts`
- **No code changes needed**: In PageView, ToolsPanel, or other core files
- **Plugin-style architecture**: Components are self-contained modules

### Type Safety
- Full TypeScript coverage
- Discriminated unions for component types
- Compile-time validation of props
- No `any` types (using `unknown` where needed)

### Maintainability
- Clear separation of concerns
- Single Responsibility Principle applied
- Easy to test individual components
- Registry pattern allows mocking for tests

### Data Model Separation
- Layout data (position, size) separate from component data (config)
- Component config is flexible (`Record<string, unknown>`)
- Each component type defines its own config schema
- Metadata tracking (created, updated timestamps)

## Known Considerations

### Grid Layout Data Preservation
The system uses a specific pattern to handle `grid-layout-plus` updates:
- **v-model:layout** enables two-way binding for grid operations
- **@layout-updated** event handler preserves component-specific data
- The handler runs AFTER grid-layout-plus updates positions but BEFORE Vue re-renders
- This prevents component data loss during drag/resize operations

### Debugging
Console logs are included in:
- `addPanel()`: Shows when components are added and their initial state
- `onLayoutUpdated()`: Shows data preservation during layout updates (can be removed in production)

## How to Add a New Component Type

1. **Create Component** (`src/components/DashboardComponents/YourComponent.vue`):
```vue
<script setup lang="ts">
import type { DashboardComponentProps } from '../../types/components'
const props = defineProps<DashboardComponentProps>()
// Access config: props.config.yourProperty
</script>
```

2. **Create Configuration Component** (optional):
```vue
<script setup lang="ts">
import type { ComponentConfigProps } from '../../types/components'
const props = defineProps<ComponentConfigProps>()
const emit = defineEmits<{ 'update:modelValue': [value: Record<string, unknown>] }>()
</script>
```

3. **Register in `config/components.ts`**:
```typescript
{
    type: 'your-component',
    name: 'Your Component',
    description: 'What it does',
    component: markRaw(YourComponent),
    configComponent: markRaw(YourComponentConfigure),
    defaultConfig: { /* defaults */ },
    defaultDimensions: { w: 4, h: 4 },
    minDimensions: { w: 2, h: 2 }
}
```

That's it! The component automatically appears in the Tools menu.

## Future Enhancements (Recommendations)

1. **Component Configuration UI**: Modal/panel to edit component config after creation
2. **Drag-and-drop from palette**: Visual component selection
3. **Component templates**: Pre-configured component sets
4. **Validation schemas**: JSON Schema validation for component configs
5. **Lazy loading**: Load component modules on-demand
6. **Component versioning**: Handle component config migrations
7. **Export/Import layouts**: JSON format for sharing
8. **Undo/Redo**: Track layout history
9. **Permissions**: Component-level access control based on user role
10. **Component library**: Visual showcase of available components

## Files Created/Modified

### Created
- `src/types/components.ts`
- `src/types/layout.ts`
- `src/plugins/componentRegistry.ts`
- `src/composables/useComponentRegistry.ts`
- `src/components/DashboardComponents/ErrorComponent.vue`
- `src/config/components.ts`

### Modified
- `src/main.ts`
- `src/composables/useGridLayout.ts`
- `src/services/layoutService.ts`
- `src/views/PageView.vue`
- `src/components/ToolsPanel.vue`
- `src/components/DashboardComponents/SimpleHtmlComponent.vue`
- `src/components/DashboardComponents/SimpleHtmlComponentConfigure.vue`

## Technical Details

### Layout Update Flow
```
User Action (drag/resize)
    ↓
grid-layout-plus updates v-model array (loses component data)
    ↓
@layout-updated event fires
    ↓
onLayoutUpdated() handler:
  - Captures current component data (componentType, componentConfig, metadata)
  - Creates map: item.i → component data
  - Merges position updates with preserved data
    ↓
Vue reactivity triggers re-render
    ↓
Components render with preserved data ✅
```

### Why This Pattern Works
1. **v-model is necessary** - grid-layout-plus needs to update the array for drag/resize to work
2. **Event handler prevents data loss** - Runs synchronously after v-model update but before render
3. **No infinite loops** - Handler only merges data, doesn't trigger new updates
4. **Type safety maintained** - TypeScript ensures all required properties exist

## Build Status
✅ TypeScript compilation: **PASSED**
✅ Vite build: **SUCCESS**
✅ No type errors
✅ All components properly typed
✅ Layout data preservation: **WORKING**
✅ Drag and resize: **FUNCTIONAL**

## Page Management System

### Overview
Implemented a dynamic page management system allowing users to create, update, and delete pages and folders directly from the UI.

### Backend (`ReportTree.Server`)
- **Model**: `Page` entity with hierarchy support (`ParentId`).
- **Persistence**: `LiteDbPageRepository` for CRUD operations.
- **API**: `PagesController` with secure endpoints.

### Frontend (`reporttree.client`)
- **Store**: `usePagesStore` (Pinia) for state management and tree construction.
- **UI Components**:
  - `TheSideMenu.vue`: Updated with "Edit Mode", recursive rendering, and management actions.
  - `PageModal.vue`: Form for creating/editing pages.
- **Features**:
  - **Edit Mode**: Visual toggle to switch between navigation and management.
  - **Hierarchy**: Support for nested pages (folders).
  - **Pinning**: Collapsible side menu with rail mode.
