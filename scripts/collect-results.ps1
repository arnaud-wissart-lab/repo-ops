[CmdletBinding()]
param(
    [switch]$Execute,
    [ValidateSet("Text", "Json")]
    [string]$Format = "Text",
    [string]$Repositories,
    [string]$InputSource = "workflow-quotidien-n8n"
)

$repositoriesCsv = if ($Repositories) {
    $Repositories
}
elseif ($env:REPOSITORIES_CSV) {
    $env:REPOSITORIES_CSV
}
elseif ($env:RENOVATE_REPOSITORIES) {
    $env:RENOVATE_REPOSITORIES
}
else {
    ""
}

$scannedRepositories = @()
if ($repositoriesCsv) {
    $scannedRepositories = $repositoriesCsv.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ }
}

$logMessages = @(
    "[collect-results] Exécution placeholder sans effet de bord.",
    "[collect-results] Les dépôts ciblés proviennent de RENOVATE_REPOSITORIES si la variable est définie.",
    "[collect-results] Ce script est une compatibilité transitoire pour n8n avant bascule vers le worker .NET."
)

[Console]::Error.WriteLine("[collect-results] Placeholder actif")
[Console]::Error.WriteLine("[collect-results] Ce script reste une passerelle transitoire le temps que la logique métier migre vers le worker .NET")
[Console]::Error.WriteLine("[collect-results] Source attendue plus tard : journaux Renovate, états de PR GitHub et résultats de CI")
[Console]::Error.WriteLine("[collect-results] Source d'entrée actuelle : $InputSource")

if (-not $Execute) {
    [Console]::Error.WriteLine("[collect-results] Mode sans effet de bord actif, aucune collecte réelle n'est exécutée")
}
else {
    [Console]::Error.WriteLine("[collect-results] Exécution placeholder : le contrat JSON est produit sans appel externe")
}

$payload = [ordered]@{
    status = "placeholder"
    mode = "daily-maintenance"
    inputSource = $InputSource
    runDateIso = (Get-Date).ToUniversalTime().ToString("o")
    scannedRepositories = $scannedRepositories
    createdPullRequests = @()
    mergedPullRequests = @()
    failedPullRequests = @()
    remainingVulnerabilities = @()
    manualActions = @(
        "Brancher la collecte réelle sur l'API GitHub ou sur les journaux Renovate.",
        "Configurer le nœud Email Send avec un credential SMTP dans n8n.",
        "Remplacer les adresses email placeholder dans le nœud de préparation du contexte."
    )
    logMessages = $logMessages
    notes = @(
        "Le script retourne un contrat JSON stable pour permettre l'import et le test du workflow n8n.",
        "Aucune donnée GitHub réelle n'est encore interrogée à ce stade.",
        "La cible à moyen terme est de déléguer cette responsabilité au worker .NET."
    )
    counts = [ordered]@{
        scannedRepositories = $scannedRepositories.Count
        createdPullRequests = 0
        mergedPullRequests = 0
        failedPullRequests = 0
        remainingVulnerabilities = 0
    }
}

if ($Format -eq "Json") {
    $payload | ConvertTo-Json -Depth 6
}
else {
    Write-Host "Statut : placeholder"
    Write-Host "Source : $InputSource"
    Write-Host "Date d'exécution : $($payload.runDateIso)"
    Write-Host "Dépôts scannés : $(if ($scannedRepositories.Count -gt 0) { $scannedRepositories -join ', ' } else { 'aucun dépôt configuré' })"
    Write-Host "Aucune collecte réelle n'est encore branchée"
}

exit 0
