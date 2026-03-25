var builder = DistributedApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var timeZone = configuration["TZ"] ?? "Europe/Paris";
var postgresDatabase = configuration["POSTGRES_DB"] ?? "n8n";
var postgresUser = configuration["POSTGRES_USER"] ?? "n8n";
var n8nPort = int.TryParse(configuration["N8N_PORT"], out var parsedN8nPort) ? parsedN8nPort : 5678;
var workerPort = int.TryParse(configuration["WORKER_HTTP_PORT"], out var parsedWorkerPort) ? parsedWorkerPort : 8080;
var n8nProtocol = configuration["N8N_PROTOCOL"] ?? "http";
var n8nHost = configuration["N8N_HOST"] ?? "localhost";
var hasPostgresPassword = !string.IsNullOrWhiteSpace(configuration["POSTGRES_PASSWORD"]);
var hasN8nEncryptionKey = !string.IsNullOrWhiteSpace(configuration["N8N_ENCRYPTION_KEY"]);

var postgresPassword = !hasPostgresPassword
    ? builder.AddParameter("postgres-password", secret: true)
    : builder.AddParameter("postgres-password", configuration["POSTGRES_PASSWORD"]!, secret: true);
var n8nEncryptionKey = !hasN8nEncryptionKey
    ? builder.AddParameter("n8n-encryption-key", secret: true)
    : builder.AddParameter("n8n-encryption-key", configuration["N8N_ENCRYPTION_KEY"]!, secret: true);

if (!hasPostgresPassword || !hasN8nEncryptionKey)
{
    Console.WriteLine("Des secrets requis ne sont pas fournis dans l'environnement.");
    Console.WriteLine("Configurer POSTGRES_PASSWORD et N8N_ENCRYPTION_KEY via user-secrets ou via le tableau de bord Aspire.");
}

var postgres = builder.AddContainer("postgres", "postgres", "16-alpine")
    .WithEnvironment("TZ", timeZone)
    .WithEnvironment("POSTGRES_DB", postgresDatabase)
    .WithEnvironment("POSTGRES_USER", postgresUser)
    .WithEnvironment("POSTGRES_PASSWORD", postgresPassword)
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=scram-sha-256 --auth-local=scram-sha-256");

var worker = builder.AddProject<Projects.RepoOps_Worker>("worker")
    .WithEnvironment("TZ", timeZone)
    .WithEnvironment("LOG_LEVEL", configuration["LOG_LEVEL"] ?? "info")
    .WithEnvironment("GITHUB_TOKEN", configuration["GITHUB_TOKEN"] ?? string.Empty)
    .WithEnvironment("GITHUB_API_BASE_URL", configuration["GITHUB_API_BASE_URL"] ?? "https://api.github.com/")
    .WithEnvironment("GITHUB_RECENT_MERGED_WINDOW_DAYS", configuration["GITHUB_RECENT_MERGED_WINDOW_DAYS"] ?? "7")
    .WithEnvironment("RENOVATE_REPOSITORIES", configuration["RENOVATE_REPOSITORIES"] ?? string.Empty)
    .WithEnvironment("AUTOMERGE_ENABLED", configuration["AUTOMERGE_ENABLED"] ?? "false")
    .WithEnvironment("AUTOMERGE_DRY_RUN_ENABLED", configuration["AUTOMERGE_DRY_RUN_ENABLED"] ?? "true")
    .WithEnvironment("AUTOMERGE_ALLOWED_UPDATE_TYPES", configuration["AUTOMERGE_ALLOWED_UPDATE_TYPES"] ?? "patch")
    .WithEnvironment("AUTOMERGE_ALLOWED_MERGEABLE_STATES", configuration["AUTOMERGE_ALLOWED_MERGEABLE_STATES"] ?? "clean")
    .WithEnvironment("AUTOMERGE_MERGE_METHOD", configuration["AUTOMERGE_MERGE_METHOD"] ?? "squash")
    .WithEnvironment("AUTOMERGE_POLICY_FILE_PATH", configuration["AUTOMERGE_POLICY_FILE_PATH"] ?? string.Empty)
    .WithEnvironment("WORKER_HTTP_PORT", workerPort.ToString())
    .WithEnvironment("RepoOps__Worker__HttpPort", workerPort.ToString())
    .WithEnvironment("RepoOps__Worker__ExecutionTimeoutSeconds", configuration["WORKER_EXECUTION_TIMEOUT_SECONDS"] ?? "1800")
    .WithEnvironment("RepoOps__Worker__InputSource", "aspire-apphost")
    .WithEnvironment("RepoOps__Worker__EmitJsonToStdout", "false")
    .WithHttpEndpoint(port: workerPort, targetPort: workerPort, name: "http");

var n8n = builder.AddContainer("n8n", "docker.n8n.io/n8nio/n8n", "1")
    .WithEnvironment("TZ", timeZone)
    .WithEnvironment("GENERIC_TIMEZONE", timeZone)
    .WithEnvironment("LOG_LEVEL", configuration["LOG_LEVEL"] ?? "info")
    .WithEnvironment("N8N_HOST", n8nHost)
    .WithEnvironment("N8N_PORT", n8nPort.ToString())
    .WithEnvironment("N8N_PROTOCOL", n8nProtocol)
    .WithEnvironment("N8N_EDITOR_BASE_URL", n8nProtocol + "://" + n8nHost + ":" + n8nPort)
    .WithEnvironment("N8N_ENCRYPTION_KEY", n8nEncryptionKey)
    .WithEnvironment("DB_TYPE", "postgresdb")
    .WithEnvironment("DB_POSTGRESDB_HOST", "postgres")
    .WithEnvironment("DB_POSTGRESDB_PORT", "5432")
    .WithEnvironment("DB_POSTGRESDB_DATABASE", postgresDatabase)
    .WithEnvironment("DB_POSTGRESDB_USER", postgresUser)
    .WithEnvironment("DB_POSTGRESDB_PASSWORD", postgresPassword)
    .WithEnvironment("RENOVATE_REPOSITORIES", configuration["RENOVATE_REPOSITORIES"] ?? string.Empty)
    .WithHttpEndpoint(port: n8nPort, targetPort: n8nPort, name: "http");

n8n.WaitFor(postgres);

// Renovate reste volontairement hors AppHost dans ce lot.
// Le runtime Compose conserve la responsabilité de son déclenchement explicite.

builder.Build().Run();
