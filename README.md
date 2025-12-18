# PBIHoster (ReportTree)

A Power BI hosting solution with a dynamic report tree, user authentication, and role-based access control.

## Overview

PBIHoster allows organizations to host and manage Power BI reports with an "App owns the data" approach. It features a customizable side navigation menu (Report Tree) that supports hierarchy, role-based visibility, and dynamic page management.

## Tech Stack

- **Backend**: ASP.NET Core (.NET 10) Web API
- **Frontend**: Vue 3 + TypeScript + Vite
- **Database**: LiteDB (Embedded NoSQL)
- **UI Library**: Carbon Design System
- **Deployment**: Docker Compose with Caddy reverse proxy

## Features

- **Authentication**: JWT-based auth with roles (Admin, Editor, Viewer).
- **Dynamic Layout**: Configurable dashboard layout.
- **Theme System**: Support for light/dark modes and custom themes.
- **Page Management**: Dynamic creation and management of the navigation tree.

## Page Management System

The application includes a comprehensive system for managing the navigation structure directly from the UI.

### Key Features

- **Dynamic Hierarchy**: Create unlimited levels of nested pages and folders.
- **Role-Based Access**: Assign roles to pages to control visibility.
- **Edit Mode**: A dedicated mode for managing the structure without navigating.
- **CRUD Operations**: Create, Read, Update, and Delete pages.

### How to Use

1.  **Enter Edit Mode**:
    - Click the **"Edit Pages"** button at the bottom of the side navigation menu.
    - The menu items will show a yellow border and an edit icon (✎) to indicate they are editable.

2.  **Create a Top-Level Page**:
    - In Edit Mode, click the **"New Top Level Page"** button at the bottom of the menu.
    - Fill in the Title and select an Icon.

3.  **Create a Child Page (Folder Structure)**:
    - **Existing Folder**: If a menu item already has children, expand it and click **"Add Child Page"**.
    - **New Folder**: To turn a page into a folder, click on the page to edit it, then use the **"Add Child Page"** button in the modal footer.

4.  **Edit a Page**:
    - Click on any page item while in Edit Mode.
    - A modal will appear allowing you to change the **Title**, **Icon**, or **Delete** the page.

5.  **Delete a Page**:
    - Open the Edit Modal for the page you want to remove.
    - Click the **"Delete"** button.
    - *Note: Deleting a parent page will also delete all its children.*

6.  **Pin/Unpin Menu**:
    - The side menu can be pinned to stay expanded or unpinned to collapse into a rail mode (icons only).
    - Use the **"Pin Menu"** / **"Unpin Menu"** button at the bottom.

### Data Model

Pages are stored in the `LiteDB` database with the following structure:

```json
{
  "id": 1,
  "title": "Dashboard",
  "icon": "Dashboard20",
  "parentId": null,
  "order": 0,
  "roles": ["Admin", "Viewer"]
}
```
