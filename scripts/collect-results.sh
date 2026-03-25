#!/bin/sh

set -eu

NO_OP=true
OUTPUT_FORMAT="text"
INPUT_SOURCE="${INPUT_SOURCE:-workflow-quotidien-n8n}"
REPOSITORIES_CSV="${REPOSITORIES_CSV:-${RENOVATE_REPOSITORIES:-}}"

log() {
  printf '%s\n' "[collect-results] $*" >&2
}

trim() {
  printf '%s' "$1" | sed 's/^[[:space:]]*//; s/[[:space:]]*$//'
}

to_json_array() {
  input_csv="$1"
  output="["
  first=true

  old_ifs="${IFS}"
  IFS=','
  for raw_value in $input_csv; do
    value="$(trim "$raw_value")"
    if [ -n "$value" ]; then
      if [ "$first" = true ]; then
        first=false
      else
        output="${output}, "
      fi
      output="${output}\"$value\""
    fi
  done
  IFS="${old_ifs}"

  output="${output}]"
  printf '%s' "$output"
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --execute)
      NO_OP=false
      ;;
    --format)
      shift
      OUTPUT_FORMAT="${1:-text}"
      ;;
    --repositories)
      shift
      REPOSITORIES_CSV="${1:-}"
      ;;
    --input-source)
      shift
      INPUT_SOURCE="${1:-workflow-quotidien-n8n}"
      ;;
    --help|-h)
      echo "Usage: ./scripts/collect-results.sh [--execute] [--format json|text] [--repositories owner/repo-a,owner/repo-b] [--input-source source]"
      exit 0
      ;;
    *)
      log "Argument ignoré pour le placeholder : $1"
      ;;
  esac
  shift
done

RUN_DATE_ISO="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
SCANNED_REPOSITORIES_JSON="$(to_json_array "$REPOSITORIES_CSV")"

log "Placeholder actif"
log "Ce script reste une passerelle transitoire le temps que la logique métier migre vers le worker .NET"
log "Le workflow n8n lit désormais en priorité le rapport JSON produit par le worker"
log "Source attendue plus tard : journaux Renovate, états de PR GitHub et résultats de CI"
log "Source d'entrée actuelle : ${INPUT_SOURCE}"

if [ "$NO_OP" = true ]; then
  log "Mode sans effet de bord actif, aucune collecte réelle n'est exécutée"
else
  log "Exécution placeholder : le contrat JSON est produit sans appel externe"
fi

if [ "$OUTPUT_FORMAT" = "json" ]; then
  cat <<EOF
{
  "status": "placeholder",
  "mode": "daily-maintenance",
  "inputSource": "${INPUT_SOURCE}",
  "runDateIso": "${RUN_DATE_ISO}",
  "scannedRepositories": ${SCANNED_REPOSITORIES_JSON},
  "createdPullRequests": [],
  "mergedPullRequests": [],
  "failedPullRequests": [],
  "remainingVulnerabilities": [],
  "manualActions": [
    "Brancher la collecte réelle sur l'API GitHub ou sur les journaux Renovate.",
    "Configurer le nœud Email Send avec un credential SMTP dans n8n.",
    "Remplacer les adresses email placeholder dans le nœud de préparation du contexte."
  ],
  "logMessages": [
    "[collect-results] Exécution placeholder sans effet de bord.",
    "[collect-results] Les dépôts ciblés proviennent de RENOVATE_REPOSITORIES si la variable est définie.",
    "[collect-results] Ce script est une compatibilité transitoire pour n8n avant bascule vers le worker .NET."
  ],
  "notes": [
    "Le script retourne un contrat JSON stable pour permettre l'import et le test du workflow n8n.",
    "Aucune donnée GitHub réelle n'est encore interrogée à ce stade.",
    "La cible à moyen terme est de déléguer cette responsabilité au worker .NET."
  ],
  "counts": {
    "scannedRepositories": $(printf '%s' "$SCANNED_REPOSITORIES_JSON" | tr -cd '"' | wc -c | awk '{ print $1 / 2 }'),
    "createdPullRequests": 0,
    "mergedPullRequests": 0,
    "failedPullRequests": 0,
    "remainingVulnerabilities": 0
  }
}
EOF
else
  echo "Statut : placeholder"
  echo "Source : ${INPUT_SOURCE}"
  echo "Date d'exécution : ${RUN_DATE_ISO}"
  echo "Dépôts scannés : ${REPOSITORIES_CSV:-aucun dépôt configuré}"
  echo "Aucune collecte réelle n'est encore branchée"
fi

exit 0
