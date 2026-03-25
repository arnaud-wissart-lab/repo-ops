[CmdletBinding()]
param(
    [switch]$Execute
)

$reportFile = if ($env:REPORT_FILE) { $env:REPORT_FILE } else { "reports/daily-summary.json" }
$htmlTemplate = if ($env:HTML_TEMPLATE) { $env:HTML_TEMPLATE } else { "templates/daily-summary.html" }
$textTemplate = if ($env:TEXT_TEMPLATE) { $env:TEXT_TEMPLATE } else { "templates/daily-summary.txt" }

Write-Host "[send-summary] Placeholder actif"
Write-Host "[send-summary] Rapport attendu : $reportFile"
Write-Host "[send-summary] Templates attendus : $htmlTemplate et $textTemplate"
Write-Host "[send-summary] Destinataires SMTP attendus via SMTP_TO"
Write-Host "[send-summary] Dans le lot actuel, l'envoi réel est pris en charge en priorité par le workflow n8n"

if (-not $Execute) {
    Write-Host "[send-summary] Mode sans effet de bord actif, aucun email n'est envoyé"
}
else {
    Write-Host "[send-summary] L'envoi CLI n'est pas encore implémenté ; aucun email n'a été transmis"
}

exit 0
