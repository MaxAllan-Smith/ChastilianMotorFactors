using ChastilianMotorFactors.Application;
using ChastilianMotorFactors.Infrastructure;
using Serilog;

// Bootstrap logger captures startup errors before configuration is loaded.
// It is replaced by the fully configured logger once the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Serilog settings (levels, sinks) are read from the "Serilog" section of appsettings.
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddControllers();
    // Account pages (login, registration) used by the authorization server.
    builder.Services.AddRazorPages();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    WebApplication app = builder.Build();

    // Configure the HTTP request pipeline.

    // Logs one summary event per HTTP request (method, path, status code, elapsed time).
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapRazorPages();

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // HostAbortedException is thrown deliberately by tooling (e.g. EF migrations) and is not a crash.
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}