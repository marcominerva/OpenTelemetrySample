# OpenTelemetry Sample

An observability sample using **OpenTelemetry** in an **ASP.NET Core Web API on .NET 11**. It demonstrates how to correlate HTTP requests, calls to external services, and database operations through distributed tracing and structured logging.

The application exposes Minimal APIs to retrieve weather data from OpenWeatherMap and manage notes stored in SQL Server. Traces are exported to the console and through OTLP; logs are exported through OTLP.

## Table of contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Quick start](#quick-start)
- [Available APIs](#available-apis)
- [Observing telemetry](#observing-telemetry)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## Features

- Tracing of ASP.NET Core requests, `HttpClient` calls, and Entity Framework Core operations.
- Trace export to the console and an OTLP/gRPC endpoint.
- Log export with formatted messages and scopes through OpenTelemetry.
- Current weather and daily forecasts, with temperatures in degrees Celsius.
- Notes CRUD with filtering by completion status, input validation, and persistence through Entity Framework Core and SQL Server.
- Automatic database migrations at startup.
- OpenAPI documentation and Swagger UI.
- Centralized error handling through Problem Details.

> The project configures logs and traces, but not a metrics pipeline. This is an educational sample: the APIs do not include authentication or authorization and should not be exposed publicly without appropriate safeguards.

## Prerequisites

- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0), required by the project's `net11.0` target.
- A reachable SQL Server instance or **SQL Server Express LocalDB** on Windows, which is used by the default configuration.
- An **OTLP/gRPC** receiver, such as the [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/), connected to your preferred observability backend. The default endpoint is `http://localhost:4317`.
- An [OpenWeatherMap](https://openweathermap.org/api) API key to use the weather endpoints. Daily forecasts require a plan that grants access to `forecast/daily`.

Visual Studio with .NET 11 support and the ASP.NET development workload is optional: you can also run the application from the CLI.

## Configuration

Default settings are defined in `OpenTelemetrySample\appsettings.json`. To customize them, create `OpenTelemetrySample\appsettings.local.json`, which is loaded as an override and is already excluded from version control:

```json
{
    "ConnectionStrings": {
        "SqlConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MinimalDB;Integrated Security=True;TrustServerCertificate=True"
    },
    "AppSettings": {
        "OtlpExporter": "http://localhost:4317",
        "OpenWeatherMapBaseUrl": "https://api.openweathermap.org/data/2.5/",
        "OpenWeatherMapAppId": "YOUR_OPENWEATHERMAP_API_KEY"
    }
}
```

| Setting | Description |
| --- | --- |
| `ConnectionStrings:SqlConnection` | SQL Server connection string. The default database is `MinimalDB` on LocalDB. |
| `AppSettings:OtlpExporter` | OTLP/gRPC receiver address for logs and traces. |
| `AppSettings:OpenWeatherMapBaseUrl` | Base URL for the OpenWeatherMap APIs, including a trailing slash. |
| `AppSettings:OpenWeatherMapAppId` | API key for weather requests. |

Do not store API keys or credentials in version-controlled files. An OpenWeatherMap key is not required to use the notes APIs or the demo endpoints.

> The application explicitly sets the exporter endpoint from `AppSettings:OtlpExporter`. To change its destination, update this setting: do not rely solely on the `OTEL_EXPORTER_OTLP_ENDPOINT` variable in the launch profile. The local file is loaded after the default configuration sources, so it takes precedence over previously loaded values.

## Quick start

Run these commands in PowerShell:

```powershell
git clone https://github.com/marcominerva/OpenTelemetrySample.git
Set-Location OpenTelemetrySample
```

1. Configure `appsettings.local.json` as described above.
2. Make sure SQL Server or LocalDB is available and the OTLP receiver is listening on the configured endpoint.
3. If you have not already trusted the HTTPS development certificate, run:

   ```powershell
   dotnet dev-certs https --trust
   ```

4. Start the application from the repository root:

   ```powershell
   dotnet run --project .\OpenTelemetrySample\OpenTelemetrySample.csproj --launch-profile https
   ```

The command restores dependencies and builds the project. Entity Framework Core migrations are applied at startup: the database user must have the permissions required to create the database and update its schema.

- **Swagger UI:** <https://localhost:7268/swagger>
- **OpenAPI document:** <https://localhost:7268/openapi/v1.json>
- **Profile HTTP URL:** <http://localhost:5143> (requests are redirected to HTTPS).

In Visual Studio, open `OpenTelemetrySample.slnx` and run the project using the `https` profile.

## Available APIs

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/api/ping` | Writes an informational log and returns `200 OK`. |
| `POST` | `/api/pong` | Writes an informational log and returns `200 OK`. |
| `PUT` | `/api/pung` | Writes an informational log and returns `200 OK`. |
| `GET` | `/api/weather/current?city=Rome` | Returns the current weather for the specified city. |
| `GET` | `/api/weather/daily?city=Rome&days=5` | Returns daily forecasts; `days` is clamped to the range from 1 to 16. |
| `GET` | `/api/notes` | Lists notes from newest to oldest, with an optional `?completed=true` or `?completed=false` filter. |
| `GET` | `/api/notes/{id}` | Retrieves a note by GUID; returns `404` if it does not exist. |
| `POST` | `/api/notes` | Creates a note and returns `201 Created` with a reference to the resource. |
| `PUT` | `/api/notes/{id}` | Updates a note; returns `204` or `404`. |
| `DELETE` | `/api/notes/{id}` | Deletes a note; returns `204` or `404`. |

To create or update a note, use a JSON body like this in Swagger UI:

```json
{
    "date": "2026-11-13T15:00:00Z",
    "text": "Explore OpenTelemetry traces",
    "completed": false
}
```

If omitted, `date` defaults to the current UTC time. `text` is required: request validation allows 5 to 500 characters, while the database column is limited to 100. To satisfy both constraints, use text between 5 and 100 characters long.

## Observing telemetry

1. Open Swagger UI and send a request to `/api/ping` to generate a log and an HTTP trace.
2. Create or read a note to observe database operations as well.
3. Call a weather endpoint to observe the outgoing HTTP call to OpenWeatherMap.
4. View traces in the console, and logs and traces in the backend connected to the OTLP receiver.

The OpenTelemetry resource uses the application name as `service.name`, normally `OpenTelemetrySample`. ASP.NET Core instrumentation excludes requests to `/swagger`, `/openapi`, `/_framework`, and `/_vs` from traces to reduce development tooling noise.

The repository does not include a Collector configuration or a visualization backend: these components must be set up separately. Console traces remain available even without an active OTLP receiver, but logs and traces will not be available in the remote backend.

## Troubleshooting

- **Startup or migration errors:** check the SQL Server instance, connection string, and permissions. LocalDB is only available on Windows; for other environments, configure a reachable SQL Server instance.
- **Missing telemetry in the backend:** check `AppSettings:OtlpExporter`, receiver connectivity, and OTLP/gRPC support on the configured port. Also check the Collector's forwarding pipeline.
- **Weather endpoint errors:** check the API key, city name, and OpenWeatherMap plan permissions, especially for daily forecasts.
- **HTTPS certificate trust errors:** trust the development certificate using `dotnet dev-certs https --trust`.
- **Errors when saving long notes:** respect the database's 100-character limit, even though HTTP validation accepts up to 500 characters.

## Contributing

Bug reports and suggestions are welcome through the repository's [issues](https://github.com/marcominerva/OpenTelemetrySample/issues). To contribute a pull request, describe your changes, follow the style defined in `.editorconfig`, and verify the build from the repository root:

```powershell
dotnet build .\OpenTelemetrySample\OpenTelemetrySample.csproj
```

## License

Distributed under the [MIT](LICENSE.txt) license.
