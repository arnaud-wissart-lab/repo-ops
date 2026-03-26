using Microsoft.AspNetCore.Http.HttpResults;
using RepoOps.Worker.Clients;
using RepoOps.Worker.Models;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;

var runOnceRequested = args.Any(arg => string.Equals(arg, "--run-once", StringComparison.OrdinalIgnoreCase));
var decideRequested = args.Any(arg => string.Equals(arg, "--decide", StringComparison.OrdinalIgnoreCase));
var generatePromptsRequested = args.Any(arg => string.Equals(arg, "--generate-prompts", StringComparison.OrdinalIgnoreCase));
var executePromptsRequested = args.Any(arg => string.Equals(arg, "--execute-prompts", StringComparison.OrdinalIgnoreCase));
var validateResponsesRequested = args.Any(arg => string.Equals(arg, "--validate-responses", StringComparison.OrdinalIgnoreCase));
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
builder.Services.AddOptions<CodexExecutorOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        configuration.GetSection(CodexExecutorOptions.SectionName).Bind(options);
        options.Mode = configuration["CODEX_EXECUTOR_MODE"] ?? options.Mode;
        options.OutputPath = configuration["CODEX_RESPONSE_OUTPUT_PATH"] ?? options.OutputPath;
        options.DigestOutputPath = configuration["CODEX_RESPONSE_DIGEST_OUTPUT_PATH"] ?? options.DigestOutputPath;
    });
builder.Services.AddOptions<ValidationEngineOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        configuration.GetSection(ValidationEngineOptions.SectionName).Bind(options);
        options.OutputPath = configuration["VALIDATION_OUTPUT_PATH"] ?? options.OutputPath;
        options.DigestOutputPath = configuration["VALIDATION_DIGEST_OUTPUT_PATH"] ?? options.DigestOutputPath;
        options.InputResponsePath = configuration["VALIDATION_INPUT_RESPONSE_PATH"] ?? options.InputResponsePath;
        options.InputValidationPath = configuration["VALIDATION_INPUT_FILE_PATH"] ?? options.InputValidationPath;

        if (bool.TryParse(configuration["VALIDATION_INTERACTIVE_MODE"], out var interactiveMode))
        {
            options.InteractiveMode = interactiveMode;
        }
    });
builder.Services.AddHttpClient<GitHubApiClient>();
builder.Services.AddSingleton<ICodexClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CodexExecutorOptions>>().Value;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    if (!string.Equals(settings.Mode, "Stub", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogWarning(
            "Le mode Codex '{ConfiguredMode}' n'est pas pris en charge dans ce lot. Le client simulé est conservé.",
            settings.Mode);
    }

    return ActivatorUtilities.CreateInstance<StubCodexClient>(serviceProvider);
});
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
builder.Services.AddSingleton<PromptGeneratorService>();
builder.Services.AddSingleton<PromptDigestRenderer>();
builder.Services.AddSingleton<PromptPersistenceService>();
builder.Services.AddSingleton<PromptGenerationWorkflowService>();
builder.Services.AddSingleton<CodexExecutorService>();
builder.Services.AddSingleton<CodexExecutionDigestRenderer>();
builder.Services.AddSingleton<CodexExecutionPersistenceService>();
builder.Services.AddSingleton<CodexExecutionWorkflowService>();
builder.Services.AddSingleton<ValidationEngineService>();
builder.Services.AddSingleton<ValidationDigestRenderer>();
builder.Services.AddSingleton<ValidationPersistenceService>();
builder.Services.AddSingleton<ValidationWorkflowService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/maintenance/run", RunMaintenanceHttpAsync);
app.MapPost("/supervisor/decisions", RunSupervisorHttpAsync);
app.MapPost("/supervisor/prompts", RunPromptGenerationHttpAsync);
app.MapPost("/supervisor/codex/execute", RunCodexExecutionHttpAsync);

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

if (generatePromptsRequested)
{
    var emitJsonToStdout = app.Configuration.GetValue<bool>($"{RepoOpsWorkerOptions.SectionName}:EmitJsonToStdout");
    var decisionsPath = app.Configuration[$"{RepoOpsWorkerOptions.SectionName}:SupervisorInputDecisionPath"];
    var reportPath = app.Configuration[$"{RepoOpsWorkerOptions.SectionName}:SupervisorInputReportPath"];

    try
    {
        if (!string.IsNullOrWhiteSpace(decisionsPath))
        {
            await RunPromptGenerationFromDecisionPathAsync(
                app.Services,
                decisionsPath,
                emitJsonToStdout,
                CancellationToken.None);
        }
        else
        {
            await RunPromptGenerationFromReportPathAsync(
                app.Services,
                string.IsNullOrWhiteSpace(reportPath) ? app.Configuration[$"{RepoOpsWorkerOptions.SectionName}:ReportOutputPath"] : reportPath,
                emitJsonToStdout,
                CancellationToken.None);
        }

        return;
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Le générateur de prompts CLI a échoué");
        Environment.ExitCode = 1;
        return;
    }
}

if (executePromptsRequested)
{
    var emitJsonToStdout = app.Configuration.GetValue<bool>($"{RepoOpsWorkerOptions.SectionName}:EmitJsonToStdout");
    var promptsPath = app.Configuration[$"{CodexExecutorOptions.SectionName}:InputPromptPath"];

    try
    {
        await RunCodexExecutionFromPromptPathAsync(
            app.Services,
            promptsPath,
            emitJsonToStdout,
            CancellationToken.None);
        return;
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "L'exécuteur contrôlé CLI a échoué");
        Environment.ExitCode = 1;
        return;
    }
}

if (validateResponsesRequested)
{
    var emitJsonToStdout = app.Configuration.GetValue<bool>($"{RepoOpsWorkerOptions.SectionName}:EmitJsonToStdout");
    var responsePath = app.Configuration[$"{ValidationEngineOptions.SectionName}:InputResponsePath"];
    var validationPath = app.Configuration[$"{ValidationEngineOptions.SectionName}:InputValidationPath"];
    var interactiveMode = app.Configuration.GetValue<bool>($"{ValidationEngineOptions.SectionName}:InteractiveMode");

    try
    {
        await RunValidationFromPathsAsync(
            app.Services,
            responsePath,
            validationPath,
            interactiveMode,
            emitJsonToStdout,
            CancellationToken.None);
        return;
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Le moteur de validation CLI a échoué");
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

static async Task<Results<JsonHttpResult<GeneratedPromptResult>, ProblemHttpResult>> RunPromptGenerationHttpAsync(
    SupervisorDecisionResult decisions,
    IServiceProvider services,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    try
    {
        var result = await RunPromptGenerationAsync(
            services,
            decisions,
            emitJsonToStdout: false,
            cancellationToken);

        return TypedResults.Json(result, statusCode: StatusCodes.Status200OK);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Le générateur de prompts HTTP a échoué");

        return TypedResults.Problem(
            title: "Erreur interne",
            detail: "Le générateur de prompts n'a pas pu produire les prompts demandés.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}

static async Task<Results<JsonHttpResult<CodexExecutionResult>, ProblemHttpResult>> RunCodexExecutionHttpAsync(
    GeneratedPromptResult prompts,
    IServiceProvider services,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    try
    {
        var result = await RunCodexExecutionAsync(
            services,
            prompts,
            emitJsonToStdout: false,
            cancellationToken);

        return TypedResults.Json(result, statusCode: StatusCodes.Status200OK);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "L'exécuteur contrôlé HTTP a échoué");

        return TypedResults.Problem(
            title: "Erreur interne",
            detail: "L'exécuteur contrôlé n'a pas pu produire de réponses exploitables.",
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

static async Task<GeneratedPromptResult> RunPromptGenerationAsync(
    IServiceProvider services,
    SupervisorDecisionResult decisions,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<PromptGenerationWorkflowService>();

    return await workflowService.RunAsync(decisions, emitJsonToStdout, cancellationToken);
}

static async Task<GeneratedPromptResult> RunPromptGenerationFromDecisionPathAsync(
    IServiceProvider services,
    string? decisionsPath,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<PromptGenerationWorkflowService>();

    return await workflowService.RunFromDecisionPathAsync(decisionsPath, emitJsonToStdout, cancellationToken);
}

static async Task<GeneratedPromptResult> RunPromptGenerationFromReportPathAsync(
    IServiceProvider services,
    string? reportPath,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<PromptGenerationWorkflowService>();

    return await workflowService.RunFromReportPathAsync(reportPath ?? string.Empty, emitJsonToStdout, cancellationToken);
}

static async Task<CodexExecutionResult> RunCodexExecutionAsync(
    IServiceProvider services,
    GeneratedPromptResult prompts,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<CodexExecutionWorkflowService>();

    return await workflowService.RunAsync(prompts, emitJsonToStdout, cancellationToken);
}

static async Task<CodexExecutionResult> RunCodexExecutionFromPromptPathAsync(
    IServiceProvider services,
    string? promptsPath,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<CodexExecutionWorkflowService>();

    return await workflowService.RunFromPromptPathAsync(promptsPath, emitJsonToStdout, cancellationToken);
}

static async Task<ValidationResult> RunValidationFromPathsAsync(
    IServiceProvider services,
    string? responsePath,
    string? validationPath,
    bool interactiveMode,
    bool emitJsonToStdout,
    CancellationToken cancellationToken)
{
    await using var scope = services.CreateAsyncScope();
    var workflowService = scope.ServiceProvider.GetRequiredService<ValidationWorkflowService>();

    return await workflowService.RunFromPathsAsync(
        responsePath,
        validationPath,
        interactiveMode,
        emitJsonToStdout,
        cancellationToken);
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
        && !arg.StartsWith("--generate-prompts", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--execute-prompts", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--validate-responses", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--run-renovate", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--enable-auto-merge", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--disable-auto-merge-dry-run", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--automerge-policy-file=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--emit-json-to-stdout", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--input-source=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--report-path=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--decisions-path=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--prompts-path=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--responses-path=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--validation-input-path=", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--interactive", StringComparison.OrdinalIgnoreCase)
        && !arg.StartsWith("--interactive=", StringComparison.OrdinalIgnoreCase))
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

        if (string.Equals(arg, "--generate-prompts", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (string.Equals(arg, "--execute-prompts", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (string.Equals(arg, "--validate-responses", StringComparison.OrdinalIgnoreCase))
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

        const string decisionsPathPrefix = "--decisions-path=";
        if (arg.StartsWith(decisionsPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Worker:SupervisorInputDecisionPath"] = arg[decisionsPathPrefix.Length..];
            continue;
        }

        const string promptsPathPrefix = "--prompts-path=";
        if (arg.StartsWith(promptsPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Codex:InputPromptPath"] = arg[promptsPathPrefix.Length..];
            continue;
        }

        const string responsesPathPrefix = "--responses-path=";
        if (arg.StartsWith(responsesPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Validation:InputResponsePath"] = arg[responsesPathPrefix.Length..];
            continue;
        }

        const string validationInputPathPrefix = "--validation-input-path=";
        if (arg.StartsWith(validationInputPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Validation:InputValidationPath"] = arg[validationInputPathPrefix.Length..];
            continue;
        }

        if (string.Equals(arg, "--interactive", StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Validation:InteractiveMode"] = "true";
            continue;
        }

        const string interactivePrefix = "--interactive=";
        if (arg.StartsWith(interactivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            overrides["RepoOps:Validation:InteractiveMode"] = arg[interactivePrefix.Length..];
            continue;
        }
    }

    return overrides;
}
