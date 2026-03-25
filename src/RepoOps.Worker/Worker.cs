using Microsoft.Extensions.Options;
using RepoOps.Worker.Options;
using RepoOps.Worker.Services;

namespace RepoOps.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    MaintenanceWorkflowService workflowService,
    IOptions<RepoOpsWorkerOptions> options,
    IHostApplicationLifetime applicationLifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var iteration = 0;

        logger.LogInformation(
            "Le worker repo-ops démarre en mode {Mode} avec un intervalle de {IntervalSeconds} seconde(s)",
            settings.ContinuousModeEnabled ? "continu" : "exécution unique",
            settings.LoopIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            iteration++;

            logger.LogInformation("Démarrage du cycle {Iteration}", iteration);

            var report = await workflowService.RunAsync(stoppingToken);

            logger.LogInformation(
                "Cycle {Iteration} terminé avec le statut {Status} pour {RepositoryCount} dépôt(s)",
                iteration,
                report.Summary.Status,
                report.Summary.Counts.ScannedRepositories);

            if (!settings.ContinuousModeEnabled)
            {
                logger.LogInformation("Le worker s'arrête après une exécution unique");
                applicationLifetime.StopApplication();
                break;
            }

            await Task.Delay(
                TimeSpan.FromSeconds(settings.LoopIntervalSeconds),
                stoppingToken);
        }
    }
}
