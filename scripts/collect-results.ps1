[CmdletBinding()]
param(
    [switch]$Execute
)

$inputSource = if ($env:INPUT_SOURCE) { $env:INPUT_SOURCE } else { "non-définie" }

Write-Host "[collect-results] Placeholder actif"
Write-Host "[collect-results] Source attendue plus tard : rapports Renovate, états de PR GitHub et résultats de CI"
Write-Host "[collect-results] INPUT_SOURCE actuel : $inputSource"

if (-not $Execute) {
    Write-Host "[collect-results] Mode sans effet de bord actif, aucune collecte réelle n'est exécutée"
}
else {
    Write-Host "[collect-results] L'exécution réelle n'est pas encore implémentée ; aucun effet de bord n'a été produit"
}

exit 0
