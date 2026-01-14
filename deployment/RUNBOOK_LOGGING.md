# Observability and Alerting Runbook

This runbook describes how to operate `ReportTree.Server` and `reporttree.client` with structured logging, metrics, and health probes.

## Structured logging

- Logs are emitted via Serilog in JSON format to standard output.
- Every request is tagged with `CorrelationId` (also returned in the `X-Correlation-ID` header).
- Additional enrichers include machine name, process id, and thread id to simplify host attribution.

### Collection

1. Ensure your container orchestrator tails stdout/stderr (e.g., `docker logs`, Kubernetes `kubectl logs`).
2. Forward logs to your preferred aggregator (Grafana Loki, Elasticsearch, Azure Monitor) by scraping container logs.
3. Filter or group by the `CorrelationId` property to follow a single request across services and the frontend’s error reports.

## Metrics

- OpenTelemetry metrics are exposed at `/metrics` in Prometheus format.
- Default instruments include:
  - HTTP server request duration/status (`aspnetcore` instrumentation).
  - HTTP client dependency calls.
  - Runtime and process metrics (GC, CPU, memory).

### Scraping

- Configure Prometheus (or an OTel Collector) to scrape `http://<host>:<PORT>/metrics`.
- Use relabeling to attach deployment metadata (environment, pod, node) for cardinality control.

## Health and readiness

- Liveness: `GET /health` (returns `200 OK` when the process is up).
- Readiness: `GET /ready` (checks LiteDB connectivity; returns non-2xx on failure).
- The readiness probe runs `LiteDbHealthCheck`, which calls `LiteDatabase.GetCollectionNames()` to confirm the DB file is reachable and not locked.
- Failures typically indicate file permission issues, disk unavailability, or a concurrent file lock.
- Kubernetes example:
  ```yaml
  livenessProbe:
    httpGet:
      path: /health
      port: 8080
  readinessProbe:
    httpGet:
      path: /ready
      port: 8080
  ```

## Alert thresholds

| Signal | Threshold | Action |
| --- | --- | --- |
| `http_server_request_duration_seconds_bucket` | p95 > 1.5s for 5m | Investigate downstream dependencies and rate limiting configuration. |
| `http_server_request_duration_seconds_bucket` | p99 > 3s for 5m | Scale out pods/containers; verify DB access and PowerBI availability. |
| 5xx rate | > 2% of requests for 10m | Check recent deployments, inspect `/ready`, and review Serilog error traces filtered by `CorrelationId`. |
| Ready check failures | Any non-2xx for >2 minutes | Assume LiteDB unavailable; validate disk permissions and file locks. |

## Frontend error reporting

- The Vue app sends API and render errors to `VITE_MONITORING_ENDPOINT` (if configured) and echoes the correlation ID from server responses.
- If the sink is unset, errors log to the browser console; set the endpoint to forward errors to your observability stack (e.g., Sentry/Seq).

## Troubleshooting workflow

1. **Start with probes**: confirm `/health` and `/ready` succeed.
2. **Check metrics**: inspect Prometheus graphs for latency/5xx spikes and correlate with resource saturation.
3. **Correlate logs**: pick a failing request’s `X-Correlation-ID`, search Serilog logs, and match with frontend reports.
4. **Validate configuration**: ensure `PORT`, `Jwt` secrets, and `LiteDb` paths are set correctly in the environment.
