# JobQueue

A small distributed job queue in C#/.NET: an HTTP API accepts jobs, workers process them from a Redis stream,
a sweeper retries failures, and every step is traced with OpenTelemetry. Job state lives in SQL Server, and
clients can watch status changes live over SignalR.

> **Scope note:** job processing itself is a placeholder (the worker moves a job through
> `Processing` to `Completed`). The project is about the queueing, retry, observability and deployment around it.

## How it works

```
POST /jobs ──► API ──► SQL Server (job row, status Pending)
                 └───► Redis stream "job-stream"
                              │  consumer group "job-workers"
                              ▼
                         Worker(s) ──► Processing ──► Completed
                              │            └─ on error: Pending + RetryAt, or DeadLetter after 3 attempts
                              ▼
                  status events ──► Redis ──► API ──► SignalR hub "hubs/job-status"

RetrySweepWorker: every 5 s, finds jobs whose RetryAt has passed and puts them back on the stream.
```

- **Duplicate-safe create:** the client supplies the job id; a duplicate id is rejected with `409 Conflict` and the existing job, so no second job is created.
- **Retries:** failed jobs are retried with a 5-minute delay plus random jitter, up to 3 attempts, then `DeadLetter`.
- **Validation:** payloads must be present and no larger than 256 KB (FluentValidation).
- **Tracing:** the API puts its trace context on the stream entry, so a job's spans connect from the request
  through the worker. Spans are exported over OTLP to `localhost:4317`; a collector config is in `Observability/`
  (the compose file does not start a collector, so run one yourself to see traces).
- **Health checks:** `GET /health/live` and `GET /health/ready` (SQL Server and Redis).

## API

| Method | Path | Purpose |
|---|---|---|
| POST | `/jobs` | Create a job (`id`, `payload`); `201`, or `409` if the id exists |
| GET | `/jobs/{id}/status` | Current status |
| GET | `/health/live`, `/health/ready` | Liveness and readiness |
| WS | `/hubs/job-status` | SignalR stream of status changes |

Statuses: `Pending`, `Processing`, `Completed`, `Failed`, `DeadLetter`.

## Run it locally

Needs Docker.

```bash
cp .env.example .env        # set SA_PASSWORD to a local password
docker compose up --build   # SQL Server, Redis, migrator, API, worker, retry sweeper
```

The API listens on `http://localhost:8080`.

## Layout

| Project | Role |
|---|---|
| `JobQueue.API` | Minimal API, SignalR hub, Redis subscriber |
| `JobQueue.Worker` | Reads the stream, processes jobs |
| `JobQueue.RetrySweepWorker` | Re-queues jobs that are due for retry |
| `JobQueue.Application` / `.Core` / `.Infrastructure` | Services, domain types, EF Core and Redis |
| `JobQueue.Migrator` | Applies EF Core migrations |
| `JobQueue.Application.Tests`, `JobQueue.Integration.Tests` | Unit tests; integration tests with containers |

## CI and deployment

- GitHub Actions: `dotnet format` lint check, build, unit tests and integration tests on .NET 10.
- `scripts/build-and-push.sh` builds the four images for linux/amd64 and pushes them to Azure Container Registry.

## Known gaps

- A job can be lost if SQL Server is unreachable at the moment a failure is recorded (logged as critical; the code
  notes an outbox pattern as the intended fix).
- No authentication on the API.
