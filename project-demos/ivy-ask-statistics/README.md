# IVY Ask Statistics

Automated quality testing for the IVY Ask API (`mcp.ivy.app`).

Sends a curated set of questions to the API, measures response time, and reports which questions return answers and which don't. Designed to run before/after releases to catch regressions.

## What it does

- Calls `GET https://mcp.ivy.app/questions?question=<encoded>` for every question in `questions/questions.json`
- Classifies each result:
  - **success** — HTTP 200 with a markdown answer
  - **no_answer** — HTTP 404, API returned `NO_ANSWER_FOUND`
  - **error** — anything else (network issue, bad request, etc.)
- Measures response time per question (avg + P90)
- Saves a full JSON result file to `results/`
- Prints a summary to stdout
- Writes a markdown table to `$GITHUB_STEP_SUMMARY` when running in GitHub Actions

## Quick start

```bash
# Run all 35 questions against production
./run-tests.sh

# Filter by difficulty
DIFFICULTY=hard ./run-tests.sh

# Run against staging
ENV=staging BASE_URL=https://mcp-staging.ivy.app ./run-tests.sh
```

**Requirements:** `jq`, `curl`, `python3` (all standard on macOS and Ubuntu).

## Questions dataset

`questions/questions.json` — 35 questions across 14 widgets, 3 difficulty levels each.

| Difficulty | Description |
|-----------|-------------|
| `easy`    | Basic widget usage — should always return an answer |
| `medium`  | Specific features and configuration |
| `hard`    | Advanced patterns, edge cases, compositions |

To add or edit questions, just edit `questions/questions.json`. Each entry:

```json
{
  "id": "button-easy-1",
  "widget": "button",
  "difficulty": "easy",
  "question": "how to create a Button with an onClick handler in Ivy?"
}
```

- `id` must be unique
- `widget` is used for grouping in the summary
- `difficulty` must be `easy`, `medium`, or `hard`
- `question` should be a single, simple question (no compound questions)

## Results format

Each run writes a timestamped JSON file to `results/` (gitignored):

```
results/results-production-20260328-160000.json
```

Structure:

```json
{
  "meta": {
    "timestamp": "2026-03-28T16:00:00Z",
    "environment": "production",
    "total": 35,
    "success": 30,
    "no_answer": 5,
    "errors": 0,
    "successRatePct": 86,
    "avgResponseMs": 1200,
    "p90ResponseMs": 2400,
    "byDifficulty": [...],
    "byWidget": [...]
  },
  "results": [
    {
      "id": "button-easy-1",
      "widget": "button",
      "difficulty": "easy",
      "question": "how to create a Button with an onClick handler in Ivy?",
      "status": "success",
      "responseTimeMs": 980,
      "httpStatus": "200",
      "timestamp": "2026-03-28T16:00:00Z",
      "environment": "production"
    }
  ]
}
```

## CI / GitHub Actions

The workflow at `.github/workflows/ivy-ask-statistics.yml` supports:

**Manual trigger** — go to Actions → IVY Ask Statistics → Run workflow:
- Choose environment: `production` or `staging`
- Optionally filter by difficulty

**Scheduled** — runs every Monday at 08:00 UTC against production.

Results are uploaded as a GitHub Actions artifact (90-day retention) and displayed as a table in the job summary.

### Staging URL

Set the `IVY_ASK_STAGING_URL` repository secret to override the default staging URL.

## Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ENV` | `production` | Label written to results |
| `BASE_URL` | `https://mcp.ivy.app` | API base URL |
| `DIFFICULTY` | _(all)_ | Filter: `easy`, `medium`, or `hard` |
| `QUESTIONS_FILE` | `questions/questions.json` | Path to questions dataset |
| `RESULTS_DIR` | `results/` | Where to write result files |
