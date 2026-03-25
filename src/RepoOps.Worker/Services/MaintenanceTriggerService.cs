using Microsoft.Extensions.Options;
using RepoOps.Worker.Options;

namespace RepoOps.Worker.Services;

public sealed class MaintenanceTriggerService(IOptions<RepoOpsWorkerOptions> options)
{
    public bool TryConsumeTrigger()
    {
        var triggerPath = options.Value.TriggerFilePath;

        if (string.IsNullOrWhiteSpace(triggerPath))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(triggerPath);

        if (!File.Exists(fullPath))
        {
            return false;
        }

        File.Delete(fullPath);
        return true;
    }
}
