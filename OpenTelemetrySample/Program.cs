using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetrySample.Data;
using OpenTelemetrySample.Data.Entities;
using OpenTelemetrySample.Models;
using OpenTelemetrySample.Services;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.Extensions;
using WeatherForecast.Models;

var builder = WebApplication.CreateBuilder(args);
ConfigureOpenTelemetry(builder);

// Add services to the container.
builder.Services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    client.BaseAddress = new(builder.Configuration.GetValue<string>("AppSettings:OpenWeatherMapBaseUrl")!);
});

builder.Services.AddDbContext<ApplicationDbContext>((services, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection"));
});

builder.Services.AddValidation();

builder.Services.AddDefaultProblemDetails();
builder.Services.AddDefaultExceptionHandler();

builder.Services.AddOpenApi();

var app = builder.Build();

await using var scope = app.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await db.Database.MigrateAsync();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRouting();

app.MapOpenApi();
app.MapSwaggerUI(setupAction: options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", app.Environment.ApplicationName);
});

app.MapGet("/api/ping", (ILogger<Program> logger) =>
{
    logger.LogInformation("Ping received");
    return TypedResults.Ok();
});

app.MapPost("/api/pong", (ILogger<Program> logger) =>
{
    logger.LogInformation("Pong received");
    return TypedResults.Ok();
});

app.MapPut("/api/pung", (ILogger<Program> logger) =>
{
    logger.LogInformation("Pung received");
    return TypedResults.Ok();
});

app.MapGet("/api/weather/current", async (string city, IWeatherService weatherService, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    logger.LogInformation("Getting current weather for city {City}", city);

    var weather = await weatherService.GetCurrentWeatherAsync(city, cancellationToken);
    var response = new Weather(weather);

    return TypedResults.Ok(response);
});

app.MapGet("/api/weather/daily", async (string city, int days, ILogger<Program> logger, IWeatherService weatherService, CancellationToken cancellationToken) =>
{
    logger.LogInformation("Getting weather forecast for city {City} for {Days} days", city, days);

    var weather = await weatherService.GetWeatherForecastAsync(city, days, cancellationToken);
    return TypedResults.Ok(weather);
});

var notes = app.MapGroup("/api/notes");

notes.MapGet(string.Empty, async (bool? completed, ApplicationDbContext db, CancellationToken cancellationToken) =>
{
    var notes = await db.Notes.AsNoTracking().WhereIf(completed.HasValue, n => n.Completed == completed!.Value)
        .OrderByDescending(n => n.Date).ToListAsync(cancellationToken);

    return TypedResults.Ok(notes);
});

notes.MapGet("/{id:guid}", async Task<Results<Ok<Note>, NotFound>> (Guid id, ApplicationDbContext db, CancellationToken cancellationToken) =>
{
    var note = await db.Notes.AsNoTracking().SingleOrDefaultAsync(note => note.Id == id, cancellationToken);
    return note is null ? TypedResults.NotFound() : TypedResults.Ok(note);
}).WithName("GetNoteById");

notes.MapPost(string.Empty, async (NoteRequest request, ApplicationDbContext db, CancellationToken cancellationToken) =>
{
    var note = new Note
    {
        Date = request.Date,
        Text = request.Text,
        Completed = request.Completed
    };

    db.Notes.Add(note);
    await db.SaveChangesAsync(cancellationToken);

    return TypedResults.CreatedAtRoute(note, "GetNoteById", new { id = note.Id });
}).ProducesValidationProblem();

notes.MapPut("{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, NoteRequest request, ApplicationDbContext db, CancellationToken cancellationToken) =>
{
    var note = await db.Notes.FindAsync([id], cancellationToken);
    if (note is null)
    {
        return TypedResults.NotFound();
    }

    note.Date = request.Date;
    note.Text = request.Text;
    note.Completed = request.Completed;

    await db.SaveChangesAsync(cancellationToken);

    return TypedResults.NoContent();
}).ProducesValidationProblem();

notes.MapDelete("{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, ApplicationDbContext db, CancellationToken cancellationToken) =>
{
    var rowDeleted = await db.Notes.Where(note => note.Id == id).ExecuteDeleteAsync(cancellationToken);

    return rowDeleted == 1 ? TypedResults.NoContent() : TypedResults.NotFound();
});

app.Run();

static IHostApplicationBuilder ConfigureOpenTelemetry(IHostApplicationBuilder builder)
{
    var otelExporter = builder.Configuration.GetValue<string>("AppSettings:OtlpExporter")!;

    builder.Logging.AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;

        options.AddOtlpExporter(options =>
        {
            options.Endpoint = new(otelExporter);
        });
    });

    builder.Services.AddOpenTelemetry().ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = request => !request.Request.Path.StartsWithSegments("/swagger") && !request.Request.Path.StartsWithSegments("/openapi")
                    && !request.Request.Path.StartsWithSegments("/_framework") && !request.Request.Path.StartsWithSegments("/_vs");

                options.RecordException = true;
            });

            // .NET 11 provides native support for tracing ASP.NET Core,
            // so you don't need to use AddAspNetCoreInstrumentation.
            // Just uncomment the following line to use the native support instead of the instrumentation package.
            // tracing.AddSource("Microsoft.AspNetCore");

            tracing.AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.Filter = (provider, command) => true;
            });

            tracing.AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            });

            tracing.AddConsoleExporter();

            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new(otelExporter);
            });
        });

    return builder;
}