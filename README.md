# Nordic RCC – Electricity Market Time Series API

A small REST API in C# / ASP.NET Core (.NET 10) for managing electricity market time series
(production, consumption and cross-border flows) per Nordic bidding zone.

## Requirements

- .NET 10 SDK

## Run

```bash
dotnet run --project TimeSeriesApi --launch-profile http
```

Open the interactive API reference (Scalar) at `http://localhost:5213/scalar/v1`.
The OpenAPI document is at `/openapi/v1.json`. Sample requests are in `TimeSeriesApi/TimeSeriesApi.http`.
A health check is available at `/health`.

## Test

```bash
dotnet test
```

## Endpoints

| Method | Route | Success | Errors |
|---|---|---|---|
| GET | `/api/timeseries?biddingZone=&type=&from=&to=` | 200 | 400 |
| GET | `/api/timeseries/{id}` | 200 | 404 |
| POST | `/api/timeseries` | 201 + `Location` | 400 |
| PUT | `/api/timeseries/{id}` | 200 | 400, 404 |
| DELETE | `/api/timeseries/{id}` | 204 | 404 |

All errors are returned as RFC 9457 Problem Details, with the same shape (`type`, `title`, `status`, `errors`, `traceId`)
whether the error comes from model validation, our own validator, or an unexpected exception.

### Input rules

- Enums are sent as text (`"DK1"`, `"PT15M"`). Numbers such as `"resolution": 99` are rejected with 400.
- Timestamps (`start` in the body, `from` and `to` in the query) must carry `Z` or an offset, for example
  `2026-11-13T00:00:00Z` or `2026-11-13T01:00:00+01:00`. A timestamp without one is rejected with 400, because it would be
  read in the server's time zone. In a URL, encode `+` as `%2B`.
- `unit` is `MW` or `MWh`. A point needs both `position` and `quantity`; a missing value is a 400, not a silent 0.
- `quantity` must not be negative, and a series can have at most 35,136 points (one leap year of 15-minute values).

### Example

```json
{
  "type": "CrossBorderFlow",
  "biddingZone": "DK2",
  "toBiddingZone": "SE4",
  "resolution": "PT15M",
  "start": "2026-11-13T00:00:00Z",
  "unit": "MW",
  "points": [
    { "position": 1, "quantity": 412.5 },
    { "position": 2, "quantity": 398.0 }
  ]
}
```

## Architecture and design choices

- **Layers:** Controller (HTTP only) → Validator / Mapper (`Services`) → Repository (`Data`).
  The controller stays thin and the business rules can be unit tested in isolation.
- **Controllers over Minimal APIs:** a familiar, explicit structure with automatic model validation
  via `[ApiController]`. Minimal APIs would also work well for an API of this size.
- **Data model inspired by ENTSO-E/CIM:** a series has a start, a resolution (`PT15M`/`PT60M`) and
  1-based positioned points. Timestamps are derived, so gaps or misaligned timestamps cannot occur.
  `End` is computed and never stored.
- **API contract separated from the domain model:** clients cannot set `Id`, `CreatedAt` or `End`,
  and the internal model can evolve without breaking the API.
- **Two-level validation:** DataAnnotations for required fields and formats, and `TimeSeriesValidator`
  for cross-field rules. The validator's errors go through `ModelState`, so both levels produce the same
  `ValidationProblemDetails` format, including `type` and `traceId`.
- **In-memory storage** behind an async `ITimeSeriesRepository` interface, using a `ConcurrentDictionary`.
  Updates use compare-and-swap (`TryUpdate`) so concurrent PUT/DELETE cannot resurrect a deleted series.
  `UpdateAsync` returns the updated copy, so PUT needs no second read that could race with a delete.
  The repository stores and returns copies to protect internal state. Lists are sorted by start, zone and id,
  so the order is deterministic.
  A database (e.g. PostgreSQL) can replace it without changing the controller.
- **Enums as strings in JSON** (`"DK1"`, `"Production"`). Unknown names and numbers are rejected with 400
  (`allowIntegerValues: false`), and the validator checks `Enum.IsDefined` as a second line of defence.
- **Strict timestamps:** a custom `JsonConverter` for the body and a model binder for the query check that the
  raw text has `Z` or an offset before parsing, because the parsed value no longer shows whether an offset was given.
- **`TimeProvider`** instead of `DateTime.UtcNow`, which makes timestamps testable.
- **Tests:** unit tests for the validation rules and the repository (including interval edge cases),
  plus integration tests through the full HTTP pipeline using `WebApplicationFactory`.

## Assumptions

- All timestamps are handled in UTC. Input must have `Z` or an offset and is converted to UTC.
  Alignment to the resolution is checked in UTC.
- Supported bidding zones are the Nordic ones: DK1, DK2, SE1–SE4, NO1–NO5 and FI.
- Supported resolutions are 15 and 60 minutes, and `start` must be aligned to the resolution.
- Values are in MW by default. The unit (`MW` or `MWh`) is stored but not converted.
- Quantities are never negative for any type. A cross-border flow is one series per direction. If signed
  net flows were required, negative values would be allowed for `CrossBorderFlow` only.
- A cross-border flow goes from `biddingZone` to `toBiddingZone`. When filtering by bidding zone,
  a flow matches both its sending and its receiving zone.
- The time filter returns series that **overlap** the half-open interval `[from, to)`.
- PUT is a full replacement of an existing series and does not create new ones. The server owns the ids.
- Data is lost when the application restarts, which is acceptable per the assignment.

## Possible improvements

- Persistent storage (PostgreSQL / TimescaleDB) with indexes on zone and period.
- Pagination for list results (`page`/`pageSize` or cursor based).
- Optimistic concurrency with ETag / `If-Match` (return 412 on stale updates).
- PATCH or point-level endpoints for updating individual values.
- Return only the points inside the requested interval, not the whole series.
- Authentication and authorization (e.g. Entra ID / JWT), with read and write scopes.
- Duplicate detection (same type, zone(s) and overlapping period).
- API versioning, rate limiting, readiness checks and structured logging/OpenTelemetry.
- More resolutions and unit conversion (MW / MWh).
- Time zone handling for the Nordic day (CET/CEST): a local day is 23 or 25 hours long on the DST switch.