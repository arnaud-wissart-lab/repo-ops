using RepoOps.Worker;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;

var builder = Host.CreateApplicationBuilder(ParsePassthroughArguments(args));
builder.Configuration.AddInMemoryCollection(ParseWorkerOverrides(args));
builder.Logging.SetMinimumLevel(ResolveLogLevel(builder.Configuration["LOG_LEVEL"]));
builder.Services.Configure<RepoOpsWorkerOptions>(
    builder.Configuration.GetSection(RepoOpsWorkerOptions.SectionName));
builder.Services.AddOptions<RenovateExecutionOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        configuration.GetSection(RenovateExecutionOptions.SectionName).Bind(options);
        options.Command = configuration["RENOVATE_EXECUTION_COMMAND"] ?? options.Command;
        options.Arguments = configuration["RENOVATE_EXECUTION_ARGUMENTS"] ?? options.Arguments;
        options.OutputPath = configuration["RENOVATE_EXECUTION_OUTPUT_PATH"] ?? options.OutputPath;
        options.WorkingDirectory = configuration["RENOVATE_EXECUTION_WORKING_DIRECTORY"] ?? options.WorkingDirectory;

        if (int.TryParse(configuration["RENOVATE_EXECUTION_TIMEOUT_SECONDS"], out var timeoutSeconds))
        {
            options.TimeoutSeconds = timeoutSeconds;
        }

        if (int.TryParse(configuration["RENOVATE_EXECUTION_MAX_CAPTURED_LINES"], out var maxCapturedLines))
        {
            options.MaxCapturedLines = maxCapturedLines;
        }
    });
builder.Services.AddOptions<GitHubOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        configuration.GetSection(GitHubOptions.SectionName).Bind(options);
        options.Token = configuration["GITHUB_TOKEN"] ?? options.Token;
        options.ApiBaseUrl = configuration["GITHUB_API_BASE_URL"] ?? options.ApiBaseUrl;

        if (int.TryParse(configuration["GITHUB_RECENT_MERGED_WINDOW_DAYS"], out var recentMergedWindowDays))
        {
            options.RecentMergedWindowDays = recentMergedWindowDays;
        }
    });
builder.Services.AddHttpClient<GitHubApiClient>();
builder.Services.AddSingleton<GitHubMaintenanceCollector>();
builder.Services.AddSingleton<RenovateExecutionService>();
builder.Services.AddSingleton<MaintenanceReportBuilder>();
builder.Services.AddSingleton<MaintenanceDigestRenderer>();
builder.Services.AddSingleton<MaintenanceReportPersistenceService>();
builder.Services.AddSingleton<MaintenanceTriggerService>();
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

static string[] ParsePassthroughArguments(IEnumerable<string> args) =>
    args.Where(arg => !arg.StartsWith("--run-once", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--run-renovate", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--emit-json-to-stdout", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--input-source=", StringComparison.OrdinalIgnoreCase))
        .ToArray();

static Dictionary<string, string?> ParseWorkerOverrides(IEnumerable<string> args)
{
    var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    foreach (var arg in args)
    {
        if (string.Equals(arg, "--run-once", StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:ContinuousModeEnabled"] = "false";
            overrides["RepoOps:Worker:RunOnStartup"] = "true";
            continue;
        }

        if (string.Equals(arg, "--run-renovate", StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:TriggerRenovateExecution"] = "true";
            continue;
        }

        if (string.Equals(arg, "--emit-json-to-stdout", StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:EmitJsonToStdout"] = "true";
            continue;
        }

        const string inputSourcePrefix = "--input-source=";
        if (arg.StartsWith(inputSourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:InputSource"] = arg[inputSourcePrefix.Length..];
            continue;
        }

        const string reportOutputPrefix = "--report-output-path=";
        if (arg.StartsWith(reportOutputPrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:ReportOutputPath"] = arg[reportOutputPrefix.Length..];
            continue;
        }

        const string textOutputPrefix = "--summary-text-output-path=";
        if (arg.StartsWith(textOutputPrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:SummaryTextOutputPath"] = arg[textOutputPrefix.Length..];
            continue;
        }

        const string htmlOutputPrefix = "--summary-html-output-path=";
        if (arg.StartsWith(htmlOutputPrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:SummaryHtmlOutputPath"] = arg[htmlOutputPrefix.Length..];
            continue;
        }
    }

    return overrides;
}
