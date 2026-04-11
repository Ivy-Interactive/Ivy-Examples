INSERT INTO Authors (Id, Name) VALUES
    (1, 'Andrew Hunt & David Thomas'),
    (2, 'James Clear'),
    (3, 'Patrick Rothfuss'),
    (4, 'Douglas Adams'),
    (5, 'Frank Herbert'),
    (6, 'Robert C. Martin'),
    (7, 'Andy Weir'),
    (8, 'Yuval Noah Harari'),
    (9, 'Matt Haig'),
    (10, 'Stephen Hawking');

INSERT INTO Genres (Id, Name) VALUES
    (1, 'Technology'),
    (2, 'Self-Help'),
    (3, 'Fantasy'),
    (4, 'Sci-Fi'),
    (5, 'Non-Fiction'),
    (6, 'Fiction');

INSERT INTO Books (Id, Title, AuthorId, GenreId, Status, Rating, TotalPages, PagesRead, Notes, AddedAt, FinishedAt) VALUES
    ('a1000000-0000-0000-0000-000000000001', 'The Pragmatic Programmer', 1, 1, 'Completed', 5, 352, 352,
     'A must-read for every developer.', '2024-12-12T00:00:00Z', '2025-03-02T00:00:00Z'),
    ('a1000000-0000-0000-0000-000000000002', 'Atomic Habits', 2, 2, 'Completed', 5, 320, 320,
     NULL, '2024-09-23T00:00:00Z', '2025-01-01T00:00:00Z'),
    ('a1000000-0000-0000-0000-000000000003', 'The Name of the Wind', 3, 3, 'Completed', 4, 662, 662,
     NULL, '2024-04-12T00:00:00Z', '2024-05-17T00:00:00Z'),
    ('a1000000-0000-0000-0000-000000000004', 'The Hitchhiker''s Guide to the Galaxy', 4, 4, 'Completed', 5, 193, 193,
     NULL, '2023-07-24T00:00:00Z', '2023-08-13T00:00:00Z'),
    ('a1000000-0000-0000-0000-000000000005', 'Dune', 5, 4, 'Reading', NULL, 688, 312,
     NULL, '2025-03-12T00:00:00Z', NULL),
    ('a1000000-0000-0000-0000-000000000006', 'Clean Code', 6, 1, 'Reading', NULL, 431, 180,
     'Taking notes as I go.', '2025-03-27T00:00:00Z', NULL),
    ('a1000000-0000-0000-0000-000000000007', 'Project Hail Mary', 7, 4, 'Paused', NULL, 476, 95,
     'Paused — will get back to it.', '2025-02-11T00:00:00Z', NULL),
    ('a1000000-0000-0000-0000-000000000008', 'Sapiens', 8, 5, 'WantToRead', NULL, 443, NULL,
     NULL, '2025-04-01T00:00:00Z', NULL),
    ('a1000000-0000-0000-0000-000000000009', 'The Midnight Library', 9, 6, 'WantToRead', NULL, NULL, NULL,
     NULL, '2025-04-06T00:00:00Z', NULL),
    ('a1000000-0000-0000-0000-000000000010', 'A Brief History of Time', 10, 5, 'WantToRead', NULL, 212, NULL,
     NULL, '2025-04-08T00:00:00Z', NULL);
