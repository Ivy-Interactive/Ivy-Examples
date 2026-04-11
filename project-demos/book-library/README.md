# Book Library

## Description

Book Library is a compact Ivy sample app for tracking a personal reading list: a **dashboard** with metrics and charts plus **My Books** with full CRUD (list, details, create, edit, delete). Data is stored in **SQLite** with a small relational model: **Authors**, **Genres**, and **Books** linked by foreign keys.

<p align="center">
  <a href="https://ivy-sliplane-deployment.sliplane.app/sliplane-deploy-app?repo=https://github.com/Ivy-Interactive/Ivy-Examples/tree/main/project-demos/book-library">
    <img src="https://raw.githubusercontent.com/Ivy-Interactive/Ivy-Examples/main/project-demos/sliplane-manage/Assets/deploy-button.svg"
         alt="Host your Ivy app on Sliplane" />
  </a>
</p>

The link opens the [Sliplane deploy flow](https://ivy-sliplane-deployment.sliplane.app) with this repo path pre-filled (`Dockerfile` under `project-demos/book-library`). Replace the URL if you deploy from a fork.

## Built With Ivy

This web application is powered by [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## Features

### Dashboard

- **Metrics** – total books, reading now, completed (with average rating), want to read  
- **Charts** – books by genre (pie), library overview by status (bar)  
- **Currently reading** – progress cards; keys refresh when page counts change  

### My Books

- **List** – searchable list with status icons; blade navigation to details  
- **Details** – author, genre, status, rating, pages, notes; edit and delete  
- **Create / Edit** – dialogs and sheets; new authors/genres are created in the DB as needed  

### Technical Highlights

- **SQLite** – single file `db.sqlite`; schema and seed in `Scripts/schema.sql` and `Scripts/seed.sql`  
- **Relations** – `Books.AuthorId → Authors`, `Books.GenreId → Genres`  
- **`IVolume`** – persisted data path; dev uses the project directory so `db.sqlite` stays next to the `.csproj` (visible in Git diffs)  
- **Regenerate DB** – `./Scripts/generate-db.sh`, then rebuild  

## How to Run Locally

1. **Prerequisites:** .NET 10.0 SDK  
2. **Navigate:**
   ```bash
   cd project-demos/book-library
   ```
3. **Run:**
   ```bash
   dotnet watch
   ```
4. Open the URL from the terminal (typically `http://localhost:5010`).

Runtime writes **`db.sqlite`** in the project folder. Override with env `BOOK_LIBRARY_DATA` if needed.

## Deploy to Ivy Hosting

```bash
cd project-demos/book-library
ivy deploy
```

Or use the **Deploy on Sliplane** button at the top of this README.

## Learn More

- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)  
- Ivy Framework: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)  

## Tags

Books, Reading List, Dashboard, CRUD, SQLite, Ivy, Sliplane
