using RepoOps.Worker;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(ResolveLogLevel(builder.Configuration["LOG_LEVEL"]));
builder.Services.Configure<RepoOpsWorkerOptions>(
    builder.Configuration.GetSection(RepoOpsWorkerOptions.SectionName));
builder.Services.AddSingleton<MaintenanceWorkflowService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

static LogLevel ResolveLogLevel(string? value) => value?.ToLowerInvariant() switch
{
    "trace" => LogLevel.Trace,
    "debug" => LogLevel.Debug,
    "warning" => LogLevel.Warning,
    "error" => LogLevel.Error,
    "critical" => LogLevel.Critical,
    "none" => LogLevel.None,
    _ => LogLevel.Information
};
