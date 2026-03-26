#!/usr/bin/env bash
set -euo pipefail

log() {
  printf '[deploy-home] %s\n' "$1"
}

fail() {
  local line_number="$1"
  local command_text="$2"
  printf '[deploy-home] Échec local à la ligne %s sur la commande: %s\n' "$line_number" "$command_text" >&2
}

require_env() {
  local var_name="$1"
  if [ -z "${!var_name:-}" ]; then
    printf '[deploy-home] Variable requise absente: %s\n' "$var_name" >&2
    exit 1
  fi
}

DEBUG_ENABLED=0
debug() {
  if [ "$DEBUG_ENABLED" -eq 1 ]; then
    printf '[deploy-home][debug] %s\n' "$1"
  fi
}

trap 'fail "$LINENO" "$BASH_COMMAND"' ERR

: "${SSH_PORT:=22}"
: "${DEPLOY_REF:=main}"
: "${DEPLOY_ENVIRONMENT:=home}"
: "${DEPLOY_DEBUG:=0}"
: "${GITHUB_TOKEN:=}"

require_env "SSH_HOST"
require_env "SSH_USER"
require_env "SSH_PRIVATE_KEY"
require_env "GITHUB_REPOSITORY"

case "$DEPLOY_DEBUG" in
  1 | true)
    DEBUG_ENABLED=1
    ;;
  0 | false)
    DEBUG_ENABLED=0
    ;;
  *)
    log "Valeur DEPLOY_DEBUG invalide: '${DEPLOY_DEBUG}' (attendu: 0, 1, true, false)."
    exit 1
    ;;
esac

REPO_SLUG="${GITHUB_REPOSITORY}"
REPO_TOKEN="${GITHUB_TOKEN}"

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    log "Commande requise introuvable sur le runner: ${cmd}"
    exit 1
  fi
}

if [ "$DEPLOY_ENVIRONMENT" != "home" ]; then
  log "Environnement '${DEPLOY_ENVIRONMENT}' non reconnu pour ce script (attendu: home)."
  exit 1
fi

require_cmd ssh
require_cmd mktemp
require_cmd chmod
require_cmd base64

log "Déploiement de ${REPO_SLUG}@${DEPLOY_REF} vers ${SSH_USER}@${SSH_HOST}:${SSH_PORT}."
debug "Mode debug activé."
debug "Contexte local: dépôt=${REPO_SLUG}, environnement=${DEPLOY_ENVIRONMENT}, ref=${DEPLOY_REF}."

ssh_key_file="$(mktemp)"
cleanup() {
  rm -f "$ssh_key_file"
}
trap cleanup EXIT

umask 077
printf '%s\n' "$SSH_PRIVATE_KEY" >"$ssh_key_file"
chmod 600 "$ssh_key_file"
debug "Clé SSH temporaire créée dans ${ssh_key_file}."

ssh_opts=(
  -i "$ssh_key_file"
  -p "$SSH_PORT"
  -o BatchMode=yes
  -o StrictHostKeyChecking=accept-new
  -o ConnectTimeout=10
)

debug "Test de connectivité SSH vers ${SSH_USER}@${SSH_HOST}:${SSH_PORT}."
ssh "${ssh_opts[@]}" "${SSH_USER}@${SSH_HOST}" "echo '[remote] Connexion SSH établie.'"

ssh "${ssh_opts[@]}" "${SSH_USER}@${SSH_HOST}" \
  bash -se -- "$DEPLOY_REF" "$REPO_SLUG" "$REPO_TOKEN" "$DEBUG_ENABLED" <<'REMOTE_SCRIPT'
set -euo pipefail

log() {
  printf '[remote] %s\n' "$1"
}

fail() {
  local line_number="$1"
  local command_text="$2"
  printf '[remote] Échec à la ligne %s sur la commande: %s\n' "$line_number" "$command_text" >&2
}

DEPLOY_DEBUG_MODE="${4:-0}"
debug() {
  if [ "$DEPLOY_DEBUG_MODE" -eq 1 ]; then
    printf '[remote][debug] %s\n' "$1"
  fi
}

trap 'fail "$LINENO" "$BASH_COMMAND"' ERR

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    log "Commande requise introuvable sur la machine cible: ${cmd}"
    exit 1
  fi
}

git_with_auth() {
  if [ -n "$REPO_TOKEN" ]; then
    local auth_header
    auth_header="$(printf 'x-access-token:%s' "$REPO_TOKEN" | base64 | tr -d '\n')"
    git -c "http.extraheader=AUTHORIZATION: basic ${auth_header}" "$@"
    return
  fi

  git "$@"
}

DEPLOY_REF="$1"
REPO_SLUG="$2"
REPO_TOKEN="${3:-}"
REPO_URL="https://github.com/${REPO_SLUG}.git"

APP_DIR="/home/arnaud/apps/repo-ops"
COMPOSE_FILE="docker-compose.yml"
ENV_FILE=".env"
ENV_EXAMPLE_FILE=".env.example"
COMPOSE_PROJECT="repo-ops-home"
LOCAL_DEMO_UI_URL="http://127.0.0.1:8084"
HEALTHCHECK_URL="https://repoops.arnaudwissart.fr"
HEALTHCHECK_TIMEOUT_SECONDS=300
HEALTHCHECK_POLL_SECONDS=5
EXPECTED_MARKER="RepoOps Live Demo"
EXPECTED_SERVICES=("demo-ui" "worker" "postgres" "n8n")

APP_PARENT_DIR="$(dirname "$APP_DIR")"
COMPOSE_FILE_PATH="${APP_DIR}/${COMPOSE_FILE}"
ENV_FILE_PATH="${APP_DIR}/${ENV_FILE}"
ENV_EXAMPLE_PATH="${APP_DIR}/${ENV_EXAMPLE_FILE}"

require_cmd git
require_cmd docker
require_cmd curl
require_cmd base64

ensure_env_key() {
  local file_path="$1"
  local key="$2"
  local value="$3"

  if grep -qE "^${key}=" "$file_path"; then
    return
  fi

  printf '\n%s=%s\n' "$key" "$value" >> "$file_path"
  log "Variable ${key} absente du .env, valeur par défaut ajoutée: ${value}"
}

log "Script distant initialisé."
debug "Contexte distant: ref=${DEPLOY_REF}, repo=${REPO_SLUG}, app_dir=${APP_DIR}."

compose_cmd=()
if docker compose version >/dev/null 2>&1; then
  compose_cmd=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
  compose_cmd=(docker-compose)
else
  log "docker compose est introuvable (ni plugin Docker, ni binaire docker-compose)."
  exit 1
fi

log "Préparation du dossier ${APP_DIR}"
mkdir -p "$APP_PARENT_DIR"
debug "Parent du dossier applicatif: ${APP_PARENT_DIR}."

if [ ! -d "$APP_DIR/.git" ]; then
  log "Repository absent, clonage initial."
  if ! git_with_auth clone "$REPO_URL" "$APP_DIR"; then
    log "Clonage impossible. Si le dépôt est privé, vérifier que GITHUB_TOKEN est transmis."
    exit 1
  fi
fi

cd "$APP_DIR"
debug "Répertoire courant distant: $(pwd)"
git remote set-url origin "$REPO_URL"

log "Mise à jour Git et résolution de la référence ${DEPLOY_REF}"
git_with_auth fetch --prune --tags origin

if [[ "$DEPLOY_REF" =~ ^[0-9a-f]{7,40}$ ]]; then
  log "Référence détectée comme SHA, checkout détaché."
  git checkout --detach "$DEPLOY_REF"
elif git rev-parse -q --verify "refs/tags/${DEPLOY_REF}" >/dev/null; then
  log "Référence détectée comme tag, checkout détaché."
  git checkout --detach "refs/tags/${DEPLOY_REF}"
else
  log "Référence détectée comme branche, alignement sur origin/${DEPLOY_REF}."
  git checkout -B "$DEPLOY_REF" "origin/${DEPLOY_REF}"
  git reset --hard "origin/${DEPLOY_REF}"
fi

deployed_commit="$(git rev-parse --short HEAD)"
deployed_date_utc="$(date -u '+%Y-%m-%dT%H:%M:%SZ')"
deployed_host="$(hostname -f 2>/dev/null || hostname 2>/dev/null || echo "unknown-host")"
log "Commit déployé: ${deployed_commit}"
log "Contexte déploiement: host=${deployed_host}, date_utc=${deployed_date_utc}, ref=${DEPLOY_REF}"

if [ ! -f "$COMPOSE_FILE_PATH" ]; then
  log "Fichier compose introuvable: ${COMPOSE_FILE_PATH}"
  exit 1
fi

if [ ! -f "$ENV_EXAMPLE_PATH" ]; then
  log "Fichier exemple introuvable: ${ENV_EXAMPLE_PATH}"
  exit 1
fi

if [ ! -f "$ENV_FILE_PATH" ]; then
  cp "$ENV_EXAMPLE_PATH" "$ENV_FILE_PATH"
  chmod 600 "$ENV_FILE_PATH"
  log "Fichier .env créé depuis .env.example. Personnaliser les secrets avant un déploiement réel."
fi

ensure_env_key "$ENV_FILE_PATH" "DEMO_UI_PORT" "8084"

log "Validation de la configuration Compose"
"${compose_cmd[@]}" --env-file "$ENV_FILE_PATH" -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE_PATH" config >/dev/null
debug "Validation Compose OK."

log "Build et démarrage de la stack home via docker compose"
"${compose_cmd[@]}" --env-file "$ENV_FILE_PATH" -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE_PATH" up -d --build --remove-orphans

log "Vérification de l'état compose"
"${compose_cmd[@]}" --env-file "$ENV_FILE_PATH" -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE_PATH" ps

log "Vérification locale de la démo via ${LOCAL_DEMO_UI_URL}"
local_demo_attempts=$((HEALTHCHECK_TIMEOUT_SECONDS / HEALTHCHECK_POLL_SECONDS))
if [ "$local_demo_attempts" -lt 1 ]; then
  local_demo_attempts=1
fi

local_demo_ready="false"
for ((attempt=1; attempt<=local_demo_attempts; attempt+=1)); do
  local_demo_payload="$(curl -fsSL --connect-timeout 3 --max-time 10 "$LOCAL_DEMO_UI_URL" || true)"

  if printf '%s' "$local_demo_payload" | grep -q "$EXPECTED_MARKER"; then
    local_demo_ready="true"
    log "Démo locale OK via ${LOCAL_DEMO_UI_URL} (tentative ${attempt}/${local_demo_attempts})."
    break
  fi

  log "Démo locale indisponible ou marqueur absent (tentative ${attempt}/${local_demo_attempts})."
  sleep "$HEALTHCHECK_POLL_SECONDS"
done

if [ "$local_demo_ready" != "true" ]; then
  log "La démo locale n'a pas pu être validée via ${LOCAL_DEMO_UI_URL}."
  "${compose_cmd[@]}" --env-file "$ENV_FILE_PATH" -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE_PATH" logs --tail 120 "${EXPECTED_SERVICES[@]}" || true
  exit 1
fi

health_attempts=$((HEALTHCHECK_TIMEOUT_SECONDS / HEALTHCHECK_POLL_SECONDS))
if [ "$health_attempts" -lt 1 ]; then
  health_attempts=1
fi

healthy="false"
for ((attempt=1; attempt<=health_attempts; attempt+=1)); do
  public_payload="$(curl -fsSL --connect-timeout 3 --max-time 10 "$HEALTHCHECK_URL" || true)"

  if printf '%s' "$public_payload" | grep -q "$EXPECTED_MARKER"; then
    healthy="true"
    log "Healthcheck public OK via ${HEALTHCHECK_URL} (tentative ${attempt}/${health_attempts})."
    break
  fi

  log "Healthcheck public indisponible ou marqueur absent (tentative ${attempt}/${health_attempts})."
  sleep "$HEALTHCHECK_POLL_SECONDS"
done

if [ "$healthy" != "true" ]; then
  log "Le healthcheck public a échoué via ${HEALTHCHECK_URL}."
  "${compose_cmd[@]}" --env-file "$ENV_FILE_PATH" -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE_PATH" logs --tail 120 "${EXPECTED_SERVICES[@]}" || true
  exit 1
fi

log "Déploiement terminé avec succès."
REMOTE_SCRIPT

log "Script terminé."
