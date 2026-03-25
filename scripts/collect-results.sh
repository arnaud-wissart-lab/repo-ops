#!/usr/bin/env bash

set -euo pipefail

NO_OP=true
INPUT_SOURCE="${INPUT_SOURCE:-non-définie}"

for arg in "$@"; do
  case "$arg" in
    --execute)
      NO_OP=false
      ;;
    --help|-h)
      echo "Usage: ./scripts/collect-results.sh [--execute]"
      exit 0
      ;;
    *)
      echo "[collect-results] Argument ignoré pour le placeholder : $arg"
      ;;
  esac
done

echo "[collect-results] Placeholder actif"
echo "[collect-results] Source attendue plus tard : rapports Renovate, états de PR GitHub et résultats de CI"
echo "[collect-results] INPUT_SOURCE actuel : ${INPUT_SOURCE}"

if [ "$NO_OP" = true ]; then
  echo "[collect-results] Mode sans effet de bord actif, aucune collecte réelle n'est exécutée"
else
  echo "[collect-results] L'exécution réelle n'est pas encore implémentée ; aucun effet de bord n'a été produit"
fi

exit 0
