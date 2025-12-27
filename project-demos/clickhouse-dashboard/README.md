# ClickHouse Dashboard

A simple dashboard for displaying ClickHouse table statistics, built with Ivy Framework.

## Starting ClickHouse

1. Start ClickHouse using Docker Compose:
```bash
docker-compose up -d
```

**Note:** If the container was already running, restart it to apply new settings:
```bash
docker-compose down
docker-compose up -d
```

2. Verify that ClickHouse is running:
```bash
docker ps
```

3. Test the connection (optional):
```bash
curl http://localhost:8123
```

**Connection parameters:**
- Host: `localhost`
- Port: `8123`
- Username: `default`
- Password: `default`

## Running the Application

```bash
dotnet build ClickHouseDashboard.cs
dotnet run ClickHouseDashboard.cs
```

Or simply:
```bash
dotnet run ClickHouseDashboard.cs
```

The application will automatically connect to ClickHouse on `localhost:8123` and display statistics for all tables.

## Data Structure

Docker Compose will automatically create test tables on first startup:
- `events` - events (1,000,000 rows)
- `users` - users (500,000 rows)
- `sessions` - sessions (500,000 rows)
- `metrics` - metrics (2,000,000 rows)
- `logs` - logs (1,000,000 rows)
- `transactions` - transactions (300,000 rows)

**Total: ~5.3 million rows**

## Stopping

```bash
docker-compose down
```

To remove all data:
```bash
docker-compose down -v
```
