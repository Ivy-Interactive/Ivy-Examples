-- Create sample tables for dashboard demonstration

CREATE TABLE IF NOT EXISTS events (
    id UUID DEFAULT generateUUIDv4(),
    event_type String,
    user_id UInt64,
    timestamp DateTime,
    data String
) ENGINE = MergeTree()
ORDER BY (timestamp, event_type);

CREATE TABLE IF NOT EXISTS users (
    id UInt64,
    name String,
    email String,
    created_at DateTime,
    status String
) ENGINE = MergeTree()
ORDER BY (id, created_at);

CREATE TABLE IF NOT EXISTS sessions (
    id UUID DEFAULT generateUUIDv4(),
    user_id UInt64,
    started_at DateTime,
    ended_at Nullable(DateTime),
    duration_seconds Nullable(UInt32)
) ENGINE = MergeTree()
ORDER BY (started_at, user_id);

CREATE TABLE IF NOT EXISTS metrics (
    id UUID DEFAULT generateUUIDv4(),
    metric_name String,
    value Float64,
    recorded_at DateTime,
    tags String
) ENGINE = MergeTree()
ORDER BY (recorded_at, metric_name);

CREATE TABLE IF NOT EXISTS logs (
    id UUID DEFAULT generateUUIDv4(),
    level String,
    message String,
    source String,
    timestamp DateTime
) ENGINE = MergeTree()
ORDER BY (timestamp, level);

CREATE TABLE IF NOT EXISTS transactions (
    id UUID DEFAULT generateUUIDv4(),
    user_id UInt64,
    amount Decimal(18, 2),
    currency String,
    status String,
    created_at DateTime
) ENGINE = MergeTree()
ORDER BY (created_at, user_id);

-- Insert sample data (millions and hundreds of thousands of rows)
INSERT INTO events (event_type, user_id, timestamp, data) 
SELECT 
    ['page_view', 'click', 'purchase', 'login', 'logout'][rand() % 5 + 1] as event_type,
    (rand() % 500000) + 1 as user_id,
    now() - (rand() % 86400) as timestamp,
    '{"key": "value"}' as data
FROM numbers(1000000);

INSERT INTO users (id, name, email, created_at, status)
SELECT 
    number as id,
    concat('user_', toString(number)) as name,
    concat('user', toString(number), '@example.com') as email,
    now() - (rand() % 2592000) as created_at,
    ['active', 'inactive', 'suspended'][rand() % 3 + 1] as status
FROM numbers(500000);

INSERT INTO sessions (user_id, started_at, ended_at, duration_seconds)
SELECT 
    (rand() % 500000) + 1 as user_id,
    now() - (rand() % 604800) as started_at,
    now() - (rand() % 604800) + (rand() % 3600) as ended_at,
    (rand() % 3600) as duration_seconds
FROM numbers(500000);

INSERT INTO metrics (metric_name, value, recorded_at, tags)
SELECT 
    ['cpu_usage', 'memory_usage', 'request_count', 'error_rate'][rand() % 4 + 1] as metric_name,
    rand() / 1000000.0 as value,
    now() - (rand() % 2592000) as recorded_at,
    '{"host": "server1"}' as tags
FROM numbers(2000000);

INSERT INTO logs (level, message, source, timestamp)
SELECT 
    ['INFO', 'WARN', 'ERROR', 'DEBUG'][rand() % 4 + 1] as level,
    concat('Log message ', toString(number)) as message,
    ['app', 'api', 'db', 'cache'][rand() % 4 + 1] as source,
    now() - (rand() % 2592000) as timestamp
FROM numbers(1000000);

INSERT INTO transactions (user_id, amount, currency, status, created_at)
SELECT 
    (rand() % 500000) + 1 as user_id,
    (rand() % 10000 + 10) / 100.0 as amount,
    ['USD', 'EUR', 'GBP'][rand() % 3 + 1] as currency,
    ['completed', 'pending', 'failed'][rand() % 3 + 1] as status,
    now() - (rand() % 2592000) as created_at
FROM numbers(300000);

