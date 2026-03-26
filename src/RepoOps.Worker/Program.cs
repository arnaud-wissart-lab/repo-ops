using Microsoft.AspNetCore.Http.HttpResults;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;

var runOnceRequested = args.Any(arg => string.Equals(arg, "--run-once", StringComparison.OrdinalIgnoreCase));
var decideRequested = args.Any(arg => string.Equals(arg, "--decide", StringComparison.OrdinalIgnoreCase));
var builder = WebApplication.CreateBuilder(ParsePassthroughArguments(args));

builder.Configuration.AddInMemoryCollection(ParseWorkerOverrides(args));
AddOptionalAutoMergePolicyFile(builder.Configuration);
builder.Logging.SetMinimumLevel(ResolveLogLevel(builder.Configuration["LOG_LEVEL"]));
ConfigureWorkerUrls(builder);

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
builder.Services.AddOptions<AutoMergeOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        configuration.GetSection(AutoMergeOptions.SectionName).Bind(options);

        if (bool.TryParse(configuration["AUTOMERGE_ENABLED"], out var enabled))
        {
            options.Enabled = enabled;
        }

        if (bool.TryParse(configuration["AUTOMERGE_DRY_RUN_ENABLED"], out var dryRunEnabled))
        {
            options.DryRunEnabled = dryRunEnabled;
        }

        options.MergeMethod = configuration["AUTOMERGE_MERGE_METHOD"] ?? options.MergeMethod;
        options.PolicyFilePath = configuration["AUTOMERGE_POLICY_FILE_PATH"] ?? options.PolicyFilePath;

        var allowedUpdateTypes = configuration["AUTOMERGE_ALLOWED_UPDATE_TYPES"];
        if (!string.IsNullOrWhiteSpace(allowedUpdateTypes))
        {
            options.AllowedUpdateTypes = allowedUpdateTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var allowedMergeableStates = configuration["AUTOMERGE_ALLOWED_MERGEABLE_STATES"];
        if (!string.IsNullOrWhiteSpace(allowedMergeableStates))
        {
            options.AllowedMergeableStates = allowedMergeableStates
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
builder.Services.AddSingleton<VulnerabilityAssessmentService>();
builder.Services.AddSingleton<PullRequestDecisionService>();
builder.Services.AddSingleton<PullRequestAutoMergeService>();
builder.Services.AddSingleton<RenovateExecutionService>();
builder.Services.AddSingleton<MaintenanceReportBuilder>();
builder.Services.AddSingleton<MaintenanceDigestRenderer>();
builder.Services.AddSingleton<MaintenanceReportPersistenceService>();
builder.Services.AddSingleton<MaintenanceWorkflowService>();
builder.Services.AddSingleton<SupervisorDecisionEngine>();
builder.Services.AddSingleton<SupervisorDecisionDigestRenderer>();
builder.Services.AddSingleton<SupervisorDecisionPersistenceService>();
builder.Services.AddSingleton<SupervisorDecisionWorkflowService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/maintenance/run", RunMaintenanceHttpAsync);
app.MapPost("/supervisor/decisions", RunSupervisorHttpAsync);

if (runOnceRequested)
{
    var request = ResolveCliRequest(app.Configuration);
    var emitJsonToStdout = app.Configuration.GetValue<bool>($"{RepoOpsWorkerOptions.SectionName}:EmitJsonToStdout");

    try
    {
        await RunMaintenanceAsync(
            app.Services,
            request,
            emitJsonToStdout,
            CancellationToken.None);
        return;
    }
    catch (MaintenanceExecutionTimeoutException exception)
    {
        app.Logger.LogError(exception, "Le cycle CLI a dépassé le délai autorisé");
        Environment.ExitCode = 1;
        return;
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Le cycle CLI a échoué");
        Environment.ExitCode = 1;
        return;
    }
}

if (decideRequested)
{
    var emitJsonToStdout = app.Configuration.GetValue<bool>($"{RepoOpsWorkerOptions.SectionName}:EmitJsonToStdout");
    var reportPath = app.Configuration[$"{RepoOpsWorkerOptions.SectionName}:SupervisorInputReportPath"];

    try
    {
        await RunSupervisorFromReportPathAsync(
            app.Services,
            reportPath,
            emitJsonToStdout,
            CancellationToken.None);
        return;
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Le moteur de décisions CLI a échoué");
        Environment.ExitCode = 1;
        return;
    }
}

app.Run();

static async Task<Results<JsonHttpResult<MaintenanceRunReport>, ProblemHttpResult>> RunMaintenanceHttpAsync(
    MaintenanceRunRequest? request,
    IServiceProvider services,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    var effectiveRequest = request is null
        ? new MaintenanceRunRequest()
        : new MaintenanceRunRequest
        {
            InputSource = string.IsNullOrWhiteSpace(request.InputSource) ? "http-api" : request.InputSource,
            TriggerRenovateExecution = request.TriggerRenovateExecution
        };

    try
    {
        var report = await RunMaintenanceAsync(
            services,
            effectiveRequest,
            emitJsonToStdout: false,
            cancellationToken);

        var statusCode = report.Summary.Status switch
        {
            "Partial" => StatusCodes.Status207MultiStatus,
            "Failed" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status200OK
        };

        return TypedResults.Json(report, statusCode: statusCode);
    }
    catch (MaintenanceExecutionTimeoutException exception)
    {
        logger.LogError(exception, "Le cycle HTTP a dépassé le délai autorisé");

        return TypedResults.Problem(
            title: "Délai dépassé",
            detail: exception.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Le cycle HTTP a échoué");

        return TypedResults.Problem(
            title: "Erreur interne",
            detail: "Le worker n'a pas pu produire le rapport demandé.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}

static async Task<Results<JsonHttpResult<SupervisorDecisionResult>, ProblemHttpResult>> RunSupervisorHttpAsync(
    MaintenanceRunReport report,
    IServiceProvider services,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    try
    {
        var result = await RunSupervisorAsync(
            services,
            report,
            emitJsonToStdout: false,
            cancellationToken);

        return TypedResults.Json(result, statusCode: StatusCodes.Status200OK);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Le moteur de décisions HTTP a échoué");

        return TypedResults.Problem(
            title: "Erreur interne",
            detail: "Le superviseur n'a pas pu produire les décisions demandées.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}

static async Task<MaintenanceRunReport> RunMaintenanceAsync(
    IServiceProvider services,
    MaintenanceRunRequest request,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<MaintenanceWorkflowService>();

    return await workflowService.RunAsync(request, emitJsonToStdout, cancellationToken);
}

static async Task<SupervisorDecisionResult> RunSupervisorAsync(
    IServiceProvider services,
    MaintenanceRunReport report,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<SupervisorDecisionWorkflowService>();

    return await workflowService.RunAsync(report, emitJsonToStdout, cancellationToken);
}

static async Task<SupervisorDecisionResult> RunSupervisorFromReportPathAsync(
    IServiceProvider services,
    string? reportPath,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<SupervisorDecisionWorkflowService>();

    return await workflowService.RunFromReportPathAsync(reportPath, emitJsonToStdout, cancellationToken);
}

static MaintenanceRunRequest ResolveCliRequest(IConfiguration configuration)
{
    return new MaintenanceRunRequest
    {
        InputSource = configuration[$"{RepoOpsWorkerOptions.SectionName}:InputSource"] ?? "worker-cli",
        TriggerRenovateExecution = configuration.GetValue<bool>($"{RepoOpsWorkerOptions.SectionName}:TriggerRenovateExecution")
    };
}

static void ConfigureWorkerUrls(WebApplicationBuilder builder)
{
    if (!string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"]))
    {
        return;
    }

    var configuredPort = builder.Configuration["WORKER_HTTP_PORT"]
        ?? builder.Configuration[$"{RepoOpsWorkerOptions.SectionName}:HttpPort"];
    var port = int.TryParse(configuredPort, out var parsedPort) ? parsedPort : 8080;

    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

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

static void AddOptionalAutoMergePolicyFile(ConfigurationManager configuration)
{
    var configuredPath = configuration["AUTOMERGE_POLICY_FILE_PATH"];

    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return;
    }

    configuration.AddJsonFile(configuredPath, optional: true, reloadOnChange: false);
}

static string[] ParsePassthroughArguments(IEnumerable<string> args) =>
    args.Where(arg => !arg.StartsWith("--run-once", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--decide", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--run-renovate", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--enable-auto-merge", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--disable-auto-merge-dry-run", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--automerge-policy-file=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--emit-json-to-stdout", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--input-source=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--report-path=", StringComparison.OrdinalIgnoreCase))
        .ToArray();

static Dictionary<string, string?> ParseWorkerOverrides(IEnumerable<string> args)
{
    var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    foreach (var arg in args)
    {
        if (string.Equals(arg, "--run-once", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (string.Equals(arg, "--decide", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (string.Equals(arg, "--run-renovate", StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:TriggerRenovateExecution"] = "true";
            continue;
        }

        if (string.Equals(arg, "--enable-auto-merge", StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:AutoMerge:Enabled"] = "true";
            continue;
        }

        if (string.Equals(arg, "--disable-auto-merge-dry-run", StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:AutoMerge:DryRunEnabled"] = "false";
            continue;
        }

        const string autoMergePolicyFilePrefix = "--automerge-policy-file=";
        if (arg.StartsWith(autoMergePolicyFilePrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["AUTOMERGE_POLICY_FILE_PATH"] = arg[autoMergePolicyFilePrefix.Length..];
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

        const string reportPathPrefix = "--report-path=";
        if (arg.StartsWith(reportPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:SupervisorInputReportPath"] = arg[reportPathPrefix.Length..];
            continue;
        }
    }

    return overrides;
}
