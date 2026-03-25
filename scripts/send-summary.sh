#!/usr/bin/env bash

set -euo pipefail

NO_OP=true
REPORT_FILE="${REPORT_FILE:-reports/daily-summary.json}"
HTML_TEMPLATE="${HTML_TEMPLATE:-templates/daily-summary.html}"
TEXT_TEMPLATE="${TEXT_TEMPLATE:-templates/daily-summary.txt}"

for arg in "$@"; do
  case "$arg" in
    --execute)
      NO_OP=false
      ;;
    --help|-h)
      echo "Usage: ./scripts/send-summary.sh [--execute]"
      exit 0
      ;;
    *)
      echo "[send-summary] Argument ignoré pour le placeholder : $arg"
      ;;
  esac
done

echo "[send-summary] Placeholder actif"
echo "[send-summary] Rapport attendu : ${REPORT_FILE}"
echo "[send-summary] Templates attendus : ${HTML_TEMPLATE} et ${TEXT_TEMPLATE}"
echo "[send-summary] Destinataires SMTP attendus via SMTP_TO"

if [ "$NO_OP" = true ]; then
  echo "[send-summary] Mode sans effet de bord actif, aucun email n'est envoyé"
else
  echo "[send-summary] L'envoi réel n'est pas encore implémenté ; aucun email n'a été transmis"
fi

exit 0
