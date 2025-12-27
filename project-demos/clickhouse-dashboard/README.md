# ClickHouse Dashboard

Простий дашборд для відображення статистики таблиць ClickHouse, побудований на Ivy Framework.

## Запуск ClickHouse

1. Запустіть ClickHouse через Docker Compose:
```bash
docker-compose up -d
```

**Примітка:** Якщо контейнер вже був запущений раніше, перезапустіть його для застосування нових налаштувань:
```bash
docker-compose down
docker-compose up -d
```

2. Перевірте, що ClickHouse запущений:
```bash
docker ps
```

3. Перевірте підключення (опціонально):
```bash
curl http://localhost:8123
```

**Параметри підключення:**
- Host: `localhost`
- Port: `8123`
- Username: `default`
- Password: `default`

## Запуск додатку

```bash
dotnet build ClickHouseDashboard.cs
dotnet run ClickHouseDashboard.cs
```

Або просто:
```bash
dotnet run ClickHouseDashboard.cs
```

Додаток автоматично підключиться до ClickHouse на `localhost:8123` і відобразить статистику всіх таблиць.

## Структура даних

Docker Compose автоматично створить тестові таблиці при першому запуску:
- `events` - події (1,000,000 рядків)
- `users` - користувачі (500,000 рядків)
- `sessions` - сесії (500,000 рядків)
- `metrics` - метрики (2,000,000 рядків)
- `logs` - логи (1,000,000 рядків)
- `transactions` - транзакції (300,000 рядків)

**Загалом: ~5.3 мільйони рядків**

## Зупинка

```bash
docker-compose down
```

Для видалення всіх даних:
```bash
docker-compose down -v
```
