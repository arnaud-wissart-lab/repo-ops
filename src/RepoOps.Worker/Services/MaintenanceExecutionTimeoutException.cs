namespace RepoOps.Worker.Services;

public sealed class MaintenanceExecutionTimeoutException(string message, Exception? innerException = null)
    : Exception(message, innerException);
