# Book Library

Ivy demo: dashboard + CRUD. Data lives in **SQLite** only: `db.sqlite` (tables `Authors`, `Genres`, `Books` with foreign keys).

## Regenerate the database

```bash
cd project-demos/book-library
./Scripts/generate-db.sh
```

Edits go in `Scripts/schema.sql` and `Scripts/seed.sql`, then run the script again. Rebuild so `db.sqlite` is copied to `bin/`.

## Run

```bash
dotnet watch
```

Runtime writes **`db.sqlite` in the project folder** (next to `BookLibrary.csproj`), not under `bin/`, so Git/your diff view sees DB changes. Override with env `BOOK_LIBRARY_DATA` if needed.

## Deploy

```bash
ivy deploy
```
