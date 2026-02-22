# Showcase CRM

## Description

Showcase CRM is a full-featured Customer Relationship Management web application built with Ivy Framework. It demonstrates how to build a complete CRM with companies, contacts, leads, deals, and users management—featuring an interactive dashboard with metrics and charts, Kanban boards for deal pipeline, DataTables with row actions, and Entity Framework Core with SQLite.

## Built With Ivy

This web application is powered by [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## Features

### Dashboard

- **Metrics** – Total Revenue, New Leads, Deals Closed, Average Deal Size, Win Rate, Active Companies, New Contacts, Pipeline Value
- **Date Range Picker** – Filter all metrics and charts by custom date range
- **Charts** – Daily deal creation trend, lead generation, pipeline by stage (pie), revenue trend, lead status distribution, leads by source

### Companies

- **List View** – DataTable with sorting, filtering, search, and row actions (View, Edit, Delete)
- **Details Blade** – Company info with related Contacts, Deals, and Leads
- **Create/Edit** – Dialogs and sheets for managing company data

### Contacts

- **List View** – DataTable with full CRUD
- **Details Blade** – Contact info with related Deals and Leads
- **Create/Edit** – Create and edit contacts with company association

### Leads

- **List View** – DataTable with row actions
- **Details Blade** – Lead info with associated Deals
- **Create/Edit** – Manage leads with status and source tracking

### Deals

- **Kanban Board** – Drag-and-drop pipeline (Prospecting → Qualification → Proposal → Closed Won/Lost)
- **DataTable View** – Tabular view with sorting, filtering, and row actions
- **Create/Edit** – Create deals with Company, Contact, and Lead selection

### Users

- **List View** – User management with DataTable
- **Details Blade** – User information view

### Technical Highlights

- **Entity Framework Core** – SQLite database with migrations
- **Blade Navigation** – Side panel navigation for details and related data
- **Sheets & Dialogs** – Inline editing and creation flows
- **UseQuery** – Cached data fetching with expiration
- **UseState / UseEffect** – Reactive state management

> [!NOTE]
> This project includes an `AGENTS.md` file which provides context for AI agents (like GitHub Copilot, Cursor, or Claude) to better understand the Ivy framework and your project's structure.

## How to Run Locally

1. **Prerequisites:** .NET 10.0 SDK
2. **Navigate to the project:**
   ```bash
   cd project-demos/showcase-crm
   ```
3. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
4. **Start the app:**
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

The application uses a pre-seeded SQLite database (`db.sqlite`) included in the project—no additional configuration required.

## Deploy to Ivy Hosting

1. **Navigate to the project:**
   ```bash
   cd project-demos/showcase-crm
   ```
2. **Deploy:**
   ```bash
   ivy deploy
   ```

## Learn More

- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)
- Ivy Framework: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- Entity Framework Core: [learn.microsoft.com/ef/core](https://learn.microsoft.com/ef/core)

## Tags

CRM, Sales, Leads, Deals, Kanban, Entity Framework, SQLite, Ivy
