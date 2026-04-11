namespace BookLibrary.Services;

public enum BookStatus { WantToRead, Reading, Completed, Paused }

public record Book
{
    public Guid      Id         { get; init; }
    public string    Title      { get; init; } = "";
    public string    Author     { get; init; } = "";
    public string    Genre      { get; init; } = "";
    public BookStatus Status    { get; init; } = BookStatus.WantToRead;
    public int?      Rating     { get; init; }
    public int?      TotalPages { get; init; }
    public int?      PagesRead  { get; init; }
    public string?   Notes      { get; init; }
    public DateTime  AddedAt    { get; init; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; init; }
}

public static class BookStore
{
    private const string DbFileName = "db.sqlite";

    public static event Action? DataChanged;

    public static async Task<List<Book>> GetAllAsync(IVolume volume)
    {
        await using var conn = await OpenAsync(volume);
        const string sql = """
            SELECT b.Id, b.Title, b.Status, b.Rating, b.TotalPages, b.PagesRead, b.Notes, b.AddedAt, b.FinishedAt,
                   a.Name AS Author, g.Name AS Genre
            FROM Books b
            INNER JOIN Authors a ON a.Id = b.AuthorId
            INNER JOIN Genres g  ON g.Id = b.GenreId
            ORDER BY b.AddedAt DESC
            """;
        await using var cmd = new SqliteCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync();
        var list = new List<Book>();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public static async Task<Book?> GetByIdAsync(IVolume volume, Guid id)
    {
        await using var conn = await OpenAsync(volume);
        const string sql = """
            SELECT b.Id, b.Title, b.Status, b.Rating, b.TotalPages, b.PagesRead, b.Notes, b.AddedAt, b.FinishedAt,
                   a.Name AS Author, g.Name AS Genre
            FROM Books b
            INNER JOIN Authors a ON a.Id = b.AuthorId
            INNER JOIN Genres g  ON g.Id = b.GenreId
            WHERE b.Id = @Id
            """;
        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public static async Task AddAsync(IVolume volume, Book book)
    {
        await using var conn = await OpenAsync(volume);
        await using var tx = conn.BeginTransaction();
        var authorId = await GetOrCreateAuthorIdAsync(conn, tx, book.Author);
        var genreId  = await GetOrCreateGenreIdAsync(conn, tx, book.Genre);
        const string sql = """
            INSERT INTO Books
                (Id, Title, AuthorId, GenreId, Status, Rating, TotalPages, PagesRead, Notes, AddedAt, FinishedAt)
            VALUES
                (@Id, @Title, @AuthorId, @GenreId, @Status, @Rating, @TotalPages, @PagesRead, @Notes, @AddedAt, @FinishedAt)
            """;
        await using var cmd = new SqliteCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@Id",         book.Id.ToString());
        cmd.Parameters.AddWithValue("@Title",      book.Title);
        cmd.Parameters.AddWithValue("@AuthorId",   authorId);
        cmd.Parameters.AddWithValue("@GenreId",    genreId);
        cmd.Parameters.AddWithValue("@Status",     book.Status.ToString());
        cmd.Parameters.AddWithValue("@Rating",     book.Rating     is not null ? book.Rating     : DBNull.Value);
        cmd.Parameters.AddWithValue("@TotalPages", book.TotalPages is not null ? book.TotalPages : DBNull.Value);
        cmd.Parameters.AddWithValue("@PagesRead",  book.PagesRead  is not null ? book.PagesRead  : DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes",      book.Notes      is not null ? book.Notes      : DBNull.Value);
        cmd.Parameters.AddWithValue("@AddedAt",    book.AddedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@FinishedAt", book.FinishedAt is not null ? book.FinishedAt.Value.ToString("O") : DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
        await tx.CommitAsync();
        DataChanged?.Invoke();
    }

    public static async Task UpdateAsync(IVolume volume, Book book)
    {
        await using var conn = await OpenAsync(volume);
        await using var tx = conn.BeginTransaction();
        var authorId = await GetOrCreateAuthorIdAsync(conn, tx, book.Author);
        var genreId  = await GetOrCreateGenreIdAsync(conn, tx, book.Genre);
        const string sql = """
            UPDATE Books SET
                Title      = @Title,
                AuthorId   = @AuthorId,
                GenreId    = @GenreId,
                Status     = @Status,
                Rating     = @Rating,
                TotalPages = @TotalPages,
                PagesRead  = @PagesRead,
                Notes      = @Notes,
                FinishedAt = @FinishedAt
            WHERE Id = @Id
            """;
        await using var cmd = new SqliteCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@Id",         book.Id.ToString());
        cmd.Parameters.AddWithValue("@Title",      book.Title);
        cmd.Parameters.AddWithValue("@AuthorId",   authorId);
        cmd.Parameters.AddWithValue("@GenreId",    genreId);
        cmd.Parameters.AddWithValue("@Status",     book.Status.ToString());
        cmd.Parameters.AddWithValue("@Rating",     book.Rating     is not null ? book.Rating     : DBNull.Value);
        cmd.Parameters.AddWithValue("@TotalPages", book.TotalPages is not null ? book.TotalPages : DBNull.Value);
        cmd.Parameters.AddWithValue("@PagesRead",  book.PagesRead  is not null ? book.PagesRead  : DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes",      book.Notes      is not null ? book.Notes      : DBNull.Value);
        cmd.Parameters.AddWithValue("@FinishedAt", book.FinishedAt is not null ? book.FinishedAt.Value.ToString("O") : DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
        await tx.CommitAsync();
        DataChanged?.Invoke();
    }

    public static async Task DeleteAsync(IVolume volume, Guid id)
    {
        await using var conn = await OpenAsync(volume);
        await using var cmd = new SqliteCommand("DELETE FROM Books WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        await cmd.ExecuteNonQueryAsync();
        DataChanged?.Invoke();
    }

    public static string StatusLabel(BookStatus s) => s switch
    {
        BookStatus.WantToRead => "Want to Read",
        BookStatus.Reading    => "Reading",
        BookStatus.Completed  => "Completed",
        BookStatus.Paused     => "Paused",
        _                     => s.ToString()
    };

    public static string RatingStars(int? rating) =>
        rating is null ? "—" : new string('★', rating.Value) + new string('☆', 5 - rating.Value);

    // ── Private ───────────────────────────────────────────────────────────

    private static async Task<SqliteConnection> OpenAsync(IVolume volume)
    {
        var absolutePath = volume.GetAbsolutePath(DbFileName);
        var dir = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(absolutePath))
        {
            var bundled = Path.Combine(System.AppContext.BaseDirectory, DbFileName);
            if (File.Exists(bundled))
                File.Copy(bundled, absolutePath);
        }

        if (!File.Exists(absolutePath))
            throw new InvalidOperationException(
                $"Database not found. Run: ./Scripts/generate-db.sh (creates {DbFileName}) and rebuild, or copy {DbFileName} next to the app.");

        var conn = new SqliteConnection($"Data Source=\"{absolutePath}\"");
        await conn.OpenAsync();
        await using (var pragma = new SqliteCommand("PRAGMA foreign_keys = ON;", conn))
            await pragma.ExecuteNonQueryAsync();
        return conn;
    }

    private static async Task<int> GetOrCreateAuthorIdAsync(SqliteConnection conn, SqliteTransaction tx, string name)
    {
        name = name.Trim();
        await using (var sel = new SqliteCommand("SELECT Id FROM Authors WHERE Name = @N COLLATE NOCASE", conn, tx))
        {
            sel.Parameters.AddWithValue("@N", name);
            var o = await sel.ExecuteScalarAsync();
            if (o is long l) return (int)l;
            if (o is int i) return i;
        }
        await using (var ins = new SqliteCommand("INSERT INTO Authors (Name) VALUES (@N)", conn, tx))
        {
            ins.Parameters.AddWithValue("@N", name);
            await ins.ExecuteNonQueryAsync();
        }
        await using var idCmd = new SqliteCommand("SELECT last_insert_rowid()", conn, tx);
        return Convert.ToInt32((long)(await idCmd.ExecuteScalarAsync())!);
    }

    private static async Task<int> GetOrCreateGenreIdAsync(SqliteConnection conn, SqliteTransaction tx, string name)
    {
        name = name.Trim();
        await using (var sel = new SqliteCommand("SELECT Id FROM Genres WHERE Name = @N COLLATE NOCASE", conn, tx))
        {
            sel.Parameters.AddWithValue("@N", name);
            var o = await sel.ExecuteScalarAsync();
            if (o is long l) return (int)l;
            if (o is int i) return i;
        }
        await using (var ins = new SqliteCommand("INSERT INTO Genres (Name) VALUES (@N)", conn, tx))
        {
            ins.Parameters.AddWithValue("@N", name);
            await ins.ExecuteNonQueryAsync();
        }
        await using var idCmd = new SqliteCommand("SELECT last_insert_rowid()", conn, tx);
        return Convert.ToInt32((long)(await idCmd.ExecuteScalarAsync())!);
    }

    private static Book Map(SqliteDataReader r) => new()
    {
        Id         = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        Title      = r.GetString(r.GetOrdinal("Title")),
        Author     = r.GetString(r.GetOrdinal("Author")),
        Genre      = r.GetString(r.GetOrdinal("Genre")),
        Status     = Enum.Parse<BookStatus>(r.GetString(r.GetOrdinal("Status"))),
        Rating     = r.IsDBNull(r.GetOrdinal("Rating"))     ? null : r.GetInt32(r.GetOrdinal("Rating")),
        TotalPages = r.IsDBNull(r.GetOrdinal("TotalPages")) ? null : r.GetInt32(r.GetOrdinal("TotalPages")),
        PagesRead  = r.IsDBNull(r.GetOrdinal("PagesRead"))  ? null : r.GetInt32(r.GetOrdinal("PagesRead")),
        Notes      = r.IsDBNull(r.GetOrdinal("Notes"))      ? null : r.GetString(r.GetOrdinal("Notes")),
        AddedAt    = DateTime.Parse(r.GetString(r.GetOrdinal("AddedAt"))),
        FinishedAt = r.IsDBNull(r.GetOrdinal("FinishedAt")) ? null : DateTime.Parse(r.GetString(r.GetOrdinal("FinishedAt"))),
    };
}
