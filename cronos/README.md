# Cronos Example

## Usage

1. Select your desired timezone from the dropdown menu
2. Enter a CRON expression manually or use one of the predefined examples
3. Toggle "Include seconds" if you need second-level precision
4. Click "Try parse" to see the next occurrence time
5. Use predefined examples to learn common CRON patterns

### Predefined Examples Available

- Every minute
- Every 5 minutes
- Daily at 09:00
- Weekdays at noon
- Monthly (1st day at 09:00)
- Every 30 seconds (when seconds are enabled)
- Complex patterns (e.g., specific times during work hours)

## Built With

- [Cronos](https://github.com/HangfireIO/Cronos) - CRON expression parser
- [Ivy](https://github.com/Ivy-Interactive/Ivy) - Web application framework

## Run

```
dotnet watch
```

## Deploy

```
ivy deploy
```