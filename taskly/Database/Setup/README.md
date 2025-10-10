# Taskly Database Setup

This folder contains the database schema and seed data for the Taskly application using SQLite.

## Files

- **schema.sql** - SQLite database schema including tables and triggers
- **seed.sql** - Sample seed data for testing
- **README.md** - This file with setup instructions

## Database Schema

### Tables

1. **backlog_items** - Stores all Epics, Stories, Tasks, and Bugs
   - `id` (INTEGER PRIMARY KEY AUTOINCREMENT)
   - `title` (TEXT NOT NULL)
   - `description` (TEXT)
   - `story_points` (INTEGER DEFAULT 1)
   - `priority` (INTEGER DEFAULT 1)
   - `status` (TEXT DEFAULT 'Backlog', CHECK constraint)
   - `type` (TEXT DEFAULT 'Task', CHECK constraint)
   - `sprint_id` (INTEGER, FK to sprints)
   - `parent_id` (INTEGER, self-referencing FK for hierarchy)
   - `created_at`, `updated_at` (DATETIME)

2. **sprints** - Stores sprint information
   - `id` (INTEGER PRIMARY KEY AUTOINCREMENT)
   - `name` (TEXT NOT NULL)
   - `start_date`, `end_date` (DATETIME NOT NULL)
   - `goal` (TEXT)
   - `item_ids` (TEXT DEFAULT '[]', stores JSON array)
   - `is_archived` (INTEGER DEFAULT 0, CHECK constraint for 0/1)
   - `created_at`, `updated_at` (DATETIME)

3. **developers** - Stores developer information (for future use)
   - `id` (INTEGER PRIMARY KEY AUTOINCREMENT)
   - `name` (TEXT NOT NULL)
   - `capacity` (INTEGER DEFAULT 40)
   - `created_at`, `updated_at` (DATETIME)

## Setup Instructions

### Automatic Setup (Recommended)

The database is automatically created from the template when you run the application:

1. Clone the repository
2. Run `dotnet run` or `dotnet watch`
3. Database is created automatically from `taskly_database.template.db`

### Manual Setup

If you need to manually create or reset the database:

**Create from schema:**
```bash
sqlite3 Database/taskly_database.db < Database/Setup/schema.sql
```

**Add seed data (optional):**
```bash
sqlite3 Database/taskly_database.db < Database/Setup/seed.sql
```

**Reset database:**
```bash
rm Database/taskly_database.db
dotnet run  # Auto-creates from template
```

## Hierarchy Structure

The database uses a self-referencing foreign key (`parent_id`) to create the hierarchy:

```
Epic (parent_id = NULL)
  └─ Story (parent_id = Epic.id)
       ├─ Task (parent_id = Story.id)
       └─ Bug (parent_id = Story.id)
```

## Query Examples

### Get all Epics
```sql
SELECT * FROM backlog_items WHERE type = 'Epic' AND parent_id IS NULL;
```

### Get Stories for an Epic
```sql
SELECT * FROM backlog_items WHERE type = 'Story' AND parent_id = 1;
```

### Get Tasks/Bugs for a Story
```sql
SELECT * FROM backlog_items
WHERE (type = 'Task' OR type = 'Bug') AND parent_id = 4;
```

### Get all items in a Sprint
```sql
-- Note: SQLite stores item_ids as JSON text, need to parse in application
SELECT * FROM backlog_items
WHERE sprint_id = 1;
```

### Get current (non-archived) Sprint
```sql
SELECT * FROM sprints WHERE is_archived = 0 LIMIT 1;
```

## Working with SQLite

### Database Location
- **Template**: `Database/taskly_database.template.db` (committed to Git)
- **Your database**: `Database/taskly_database.db` (ignored by Git)

### SQLite CLI Commands

**Open database:**
```bash
sqlite3 Database/taskly_database.db
```

**Show tables:**
```sql
.tables
```

**Show schema:**
```sql
.schema backlog_items
```

**Export data:**
```bash
sqlite3 Database/taskly_database.db .dump > backup.sql
```

### C# Integration

The application uses:
- **Microsoft.Data.Sqlite** for database connections
- **Dapper** for queries and ORM mapping
- **Newtonsoft.Json** for JSON serialization (item_ids in sprints)

## Notes

- The `updated_at` field is automatically updated via triggers
- All foreign keys have `ON DELETE CASCADE` or `ON DELETE SET NULL` for proper cleanup
- SQLite stores booleans as integers (0 = false, 1 = true)
- The schema supports the full Epic → Story → Task/Bug hierarchy
- Each developer has their own local database (not shared via Git)
