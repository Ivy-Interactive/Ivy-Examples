PRAGMA foreign_keys = ON;

CREATE TABLE Authors (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL COLLATE NOCASE UNIQUE
);

CREATE TABLE Genres (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL COLLATE NOCASE UNIQUE
);

CREATE TABLE Books (
    Id         TEXT    PRIMARY KEY,
    Title      TEXT    NOT NULL,
    AuthorId   INTEGER NOT NULL REFERENCES Authors (Id) ON DELETE RESTRICT,
    GenreId    INTEGER NOT NULL REFERENCES Genres  (Id) ON DELETE RESTRICT,
    Status     TEXT    NOT NULL,
    Rating     INTEGER,
    TotalPages INTEGER,
    PagesRead  INTEGER,
    Notes      TEXT,
    AddedAt    TEXT    NOT NULL,
    FinishedAt TEXT
);

CREATE INDEX IX_Books_AuthorId ON Books (AuthorId);
CREATE INDEX IX_Books_GenreId  ON Books (GenreId);
CREATE INDEX IX_Books_Status   ON Books (Status);
