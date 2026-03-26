[CmdletBinding()]
param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [Console]::OutputEncoding

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$dockerComposePath = Join-Path $repoRoot "docker-compose.yml"
$envFilePath = Join-Path $repoRoot ".env"

Write-Host "[deploy] Cible : machine locale"
Write-Host "[deploy] Répertoire : $repoRoot"

if (-not (Test-Path $dockerComposePath)) {
    throw "Le fichier docker-compose.yml est introuvable à la racine du dépôt."
}

if ($DryRun) {
    Write-Host "[deploy] Mode dry-run actif."
    Write-Host "[deploy] Vérification attendue : docker compose config"
    Write-Host "[deploy] Action attendue : docker compose up -d --build"
    if (-not (Test-Path $envFilePath)) {
        Write-Warning "[deploy] Le fichier .env est absent. Le déploiement réel échouerait tant qu'il n'est pas créé."
    }
    exit 0
}

if (-not (Test-Path $envFilePath)) {
    throw "Le fichier .env est requis pour un déploiement réel."
}

Push-Location $repoRoot
try {
    Write-Host "[deploy] Validation de la configuration Docker Compose..."
    docker compose config | Out-Null

    Write-Host "[deploy] Déploiement du socle local..."
    docker compose up -d --build

    Write-Host "[deploy] Déploiement terminé."
}
finally {
    Pop-Location
}

