-- Taskly Database Schema for SQLite
-- This schema creates tables for managing Epics, Stories, Tasks, Bugs, and Sprints

-- Enable foreign key support (must be done per connection)
PRAGMA foreign_keys = ON;

-- Backlog Items Table (Epics, Stories, Tasks, Bugs)
CREATE TABLE backlog_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    description TEXT,
    story_points INTEGER NOT NULL DEFAULT 0,
    priority INTEGER NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'Backlog' CHECK (status IN ('Backlog', 'Todo', 'InProgress', 'Review', 'Done')),
    type TEXT NOT NULL CHECK (type IN ('Epic', 'Story', 'Task', 'Bug')),
    sprint_id INTEGER,
    parent_id INTEGER,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (parent_id) REFERENCES backlog_items(id) ON DELETE CASCADE,
    FOREIGN KEY (sprint_id) REFERENCES sprints(id) ON DELETE SET NULL
);

-- Sprints Table
CREATE TABLE sprints (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    goal TEXT,
    item_ids TEXT DEFAULT '[]',  -- JSON array of backlog item IDs
    is_archived INTEGER DEFAULT 0 CHECK (is_archived IN (0, 1)),  -- Boolean as 0/1
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Developers Table (for future use)
CREATE TABLE developers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    capacity INTEGER NOT NULL DEFAULT 40,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for better query performance
CREATE INDEX idx_backlog_items_type ON backlog_items(type);
CREATE INDEX idx_backlog_items_status ON backlog_items(status);
CREATE INDEX idx_backlog_items_sprint_id ON backlog_items(sprint_id);
CREATE INDEX idx_backlog_items_parent_id ON backlog_items(parent_id);
CREATE INDEX idx_sprints_is_archived ON sprints(is_archived);

-- Triggers to automatically update updated_at timestamp
CREATE TRIGGER update_backlog_items_updated_at
    AFTER UPDATE ON backlog_items
    FOR EACH ROW
BEGIN
    UPDATE backlog_items SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

CREATE TRIGGER update_sprints_updated_at
    AFTER UPDATE ON sprints
    FOR EACH ROW
BEGIN
    UPDATE sprints SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

CREATE TRIGGER update_developers_updated_at
    AFTER UPDATE ON developers
    FOR EACH ROW
BEGIN
    UPDATE developers SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;
