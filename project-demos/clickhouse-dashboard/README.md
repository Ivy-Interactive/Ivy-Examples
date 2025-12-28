# ClickHouse Dashboard

A simple dashboard for displaying ClickHouse table statistics, built with Ivy Framework.

## Running the Application

Simply run:
```bash
dotnet run ClickHouseDashboard.cs
```

The application will **automatically**:
- Check if ClickHouse is running on port 8123
- Start ClickHouse via Docker Compose if it's not running
- Wait for ClickHouse to become available (up to 30 seconds)
- Connect and display statistics for all tables

**No manual Docker commands needed!** The application handles everything automatically.

### Manual Docker Control (Optional)

If you prefer to manage ClickHouse manually:

**Start ClickHouse:**
```bash
docker-compose up -d
```

**Stop ClickHouse:**
```bash
docker-compose down
```

**Stop and remove all data (fresh start):**
```bash
docker-compose down -v
```

This removes the Docker volume with all ClickHouse data. On next startup, `init.sql` will create fresh tables with new random data.

**Connection parameters:**
- Host: `localhost`
- Port: `8123`
- Username: `default`
- Password: `default`

## Data Structure

Docker Compose will automatically create test tables on first startup:
- `events` - events (1,000,000 rows)
- `users` - users (500,000 rows)
- `sessions` - sessions (500,000 rows)
- `metrics` - metrics (2,000,000 rows)
- `logs` - logs (1,000,000 rows)
- `transactions` - transactions (300,000 rows)