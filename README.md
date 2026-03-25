# repo-ops

`repo-ops` est un socle d’automatisation destiné à maintenir plusieurs dépôts GitHub publics personnels de manière progressive, traçable et réversible. Le dépôt est centré sur une couche métier `.NET`, conserve `Docker Compose` comme base d’exécution réelle, utilise `n8n` pour l’orchestration simple et garde `Renovate` comme outil dédié à la maintenance des dépendances.

## Objectif

Le premier objectif est de centraliser :

- la détection de dépendances obsolètes ou vulnérables ;
- l’ouverture de pull requests de maintenance ;
- l’orchestration planifiée ;
- la consolidation d’un résumé d’exécution ;
- l’envoi d’une synthèse quotidienne.

La stratégie reste prudente : démarrer sur quelques dépôts seulement via `RENOVATE_REPOSITORIES`, puis élargir progressivement.

Cycle cible :

`scan -> PR -> CI GitHub -> consolidation -> synthèse email`

## Couches du socle

### Docker Compose

[`docker-compose.yml`](./docker-compose.yml) reste la base d’exécution réelle du socle en local.

Par défaut, il démarre :

- `worker`, future couche métier principale ;
- `postgres`, utilisé par `n8n` ;
- `n8n`, pour les cron et l’envoi d’email.

`Renovate` est conservé dans la stack mais n’est plus lancé en boucle permanente. Il est déclenché explicitement à la demande.

### Worker .NET

[`src/RepoOps.Worker`](./src/RepoOps.Worker) porte la logique métier cible du socle :

- production d’un JSON stable ;
- génération du texte de synthèse ;
- génération d’un HTML simple ;
- persistance des artefacts ;
- première collecte GitHub réelle ;
- future base pour la supervision.

Dans ce lot, le worker devient la source de vérité du reporting et interroge réellement GitHub pour une première collecte ciblée.

### Aspire AppHost

[`src/RepoOps.AppHost`](./src/RepoOps.AppHost) ajoute une couche de pilotage local et de visualisation dans Visual Studio.

Cette couche permet de visualiser localement :

- le worker ;
- `postgres` ;
- `n8n`.

Dans cette phase, `Renovate` reste volontairement hors du cockpit Aspire afin de ne pas transformer l’AppHost en orchestrateur de maintenance.

### n8n

`n8n` conserve un rôle ciblé :

- déclenchements planifiés ;
- déclenchement du worker via un signal simple ;
- lecture du rapport produit ;
- envoi de l’email.

Le workflow versionné ne reconstruit plus la synthèse métier. Il consomme les artefacts produits par le worker.

### Scripts transitoires

Le dossier `scripts/` reste présent pour la transition, mais il ne fait plus partie du flux réel retenu. Les scripts servent encore à des vérifications locales ou à un secours ponctuel, pas à la logique métier principale.

## Flux réel retenu

1. `n8n` déclenche le workflow quotidien.
2. Le workflow crée un fichier de déclenchement partagé dans `runtime/`.
3. Le worker `.NET`, maintenu en veille légère, détecte ce trigger et exécute un cycle.
4. Le worker écrit :
   - [`worker-summary.json`](./reports/worker-summary.json)
   - [`worker-summary.txt`](./reports/worker-summary.txt)
   - [`worker-summary.html`](./reports/worker-summary.html)
   Le worker interroge GitHub avec `GITHUB_TOKEN` pour récupérer les PR Renovate ouvertes, les PR Renovate fusionnées récemment, les fermetures sans fusion récentes et les checks utiles à la qualification opérationnelle.
5. `n8n` supprime d'abord les anciens artefacts, puis lit le JSON frais produit par le worker.
6. `n8n` envoie l’email en réutilisant directement le sujet, le texte et le HTML déjà préparés.

## Exécution explicite de Renovate

`Renovate` n’est pas un daemon dans cette architecture. Il s’exécute explicitement quand vous en avez besoin :

```powershell
docker compose --profile maintenance run --rm renovate
```

Pour une vérification minimale de l’image et de la configuration :

```powershell
docker compose --profile maintenance run --rm renovate --version
```

## Structure du dépôt

- [`RepoOps.sln`](./RepoOps.sln) regroupe les projets `.NET` du dépôt.
- [`src/RepoOps.AppHost`](./src/RepoOps.AppHost) contient l’AppHost Aspire pour le pilotage local.
- [`src/RepoOps.Worker`](./src/RepoOps.Worker) contient le Worker Service `.NET`.
- [`docker-compose.yml`](./docker-compose.yml) décrit la stack locale réellement exécutée.
- [`.env.example`](./.env.example) centralise les variables attendues.
- [`AGENTS.md`](./AGENTS.md) fixe les règles de travail pour les contributions futures.
- [`renovate/config.js`](./renovate/config.js) porte la configuration self-hosted d’administration.
- [`docs/architecture.md`](./docs/architecture.md) décrit les responsabilités et les flux.
- [`docs/rollout-plan.md`](./docs/rollout-plan.md) découpe l’adoption en phases.
- [`n8n/README.md`](./n8n/README.md) décrit le rôle des workflows et leur import.
- [`n8n/workflows/repo-ops-daily-maintenance.json`](./n8n/workflows/repo-ops-daily-maintenance.json) fournit le workflow quotidien importable.
- le dossier `scripts/` contient les passerelles et utilitaires transitoires ;
- le dossier `templates/` contient les modèles historiques de synthèse et de tâches.

## Démarrage rapide avec Docker Compose

1. Copier le fichier d’exemple et renseigner les variables requises :

   ```powershell
   Copy-Item .env.example .env
   ```

2. Définir au minimum :
   - `GITHUB_TOKEN`
   - `GITHUB_API_BASE_URL` si vous ciblez autre chose que `github.com`
   - `GITHUB_RECENT_MERGED_WINDOW_DAYS` si vous voulez ajuster la fenêtre des fusions récentes
   - `RENOVATE_REPOSITORIES`
   - `N8N_ENCRYPTION_KEY`
   - `POSTGRES_PASSWORD`
   - les variables SMTP si l’envoi d’email doit être activé rapidement

3. Démarrer la stack principale :

   ```powershell
   docker compose up -d --build
   ```

4. Accéder à `n8n` via l’URL exposée par `N8N_EDITOR_BASE_URL`, puis créer le compte propriétaire initial demandé par l’interface.

5. Importer le workflow versionné dans `n8n`.

6. Dans le workflow importé, remplacer les marqueurs `__CONFIGURER_FROM_DANS_N8N__` et `__CONFIGURER_TO_DANS_N8N__`, puis associer le credential SMTP.

7. Déclencher `Renovate` explicitement lorsque nécessaire :

   ```powershell
   docker compose --profile maintenance run --rm renovate
   ```

## Démarrage local avec Aspire et Visual Studio

1. Ouvrir [`RepoOps.sln`](./RepoOps.sln) dans Visual Studio 2022 ou plus récent avec le support Aspire installé.
2. Configurer `POSTGRES_PASSWORD`, `N8N_ENCRYPTION_KEY` et `GITHUB_TOKEN` via des variables d’environnement locales ou via `dotnet user-secrets` sur le projet AppHost si vous voulez activer la collecte GitHub réelle.
3. Définir [`src/RepoOps.AppHost`](./src/RepoOps.AppHost) comme projet de démarrage.
4. Lancer l’application pour ouvrir le tableau de bord Aspire et observer les ressources locales.

Exemple avec `dotnet user-secrets` :

```powershell
dotnet user-secrets --project .\src\RepoOps.AppHost set POSTGRES_PASSWORD "mot-de-passe-local"
dotnet user-secrets --project .\src\RepoOps.AppHost set N8N_ENCRYPTION_KEY "cle-locale-longue-et-stable"
dotnet user-secrets --project .\src\RepoOps.AppHost set GITHUB_TOKEN "ghp_votre_jeton"
```

Cette couche Aspire sert au pilotage local. Pour les exécutions réelles de la stack, conserver `docker compose`.

## Limites actuelles

- le worker interroge désormais GitHub, mais seulement sur un premier périmètre REST limité ;
- les PR ouvertes sont comptées à partir des PR Renovate détectées, pas à partir d’un historique d’exécution `Renovate` propre au dépôt ;
- les PR fusionnées sont lues dans une fenêtre glissante configurable, limitée au dernier lot de PR fermées renvoyé par l’API ;
- la qualification des PR ouvertes repose sur la combinaison des check-runs et du statut combiné GitHub sur la tête de PR ;
- les PR sans check décisif sont classées dans `pullRequestStatuses.blocked` avec une qualification incomplète ;
- la collecte des vulnérabilités reste encore placeholder ;
- le déclenchement de maintenance repose encore sur un fichier partagé simple ;
- le worker fonctionne encore en veille par scrutation légère, pas via une API dédiée ;
- la configuration SMTP et les destinataires restent à finaliser manuellement dans `n8n` ;
- `Renovate` n’est pas encore orchestré par une planification dédiée dans le dépôt ;
- le superviseur IA n’est pas implémenté.

## Vérifications locales

Commandes minimales à exécuter après avoir renseigné `.env` :

```powershell
Copy-Item .env.example .env
docker compose config
docker compose up -d --build --wait
docker compose ps
docker compose logs --tail=100 worker postgres n8n
docker compose exec postgres sh -lc 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
docker compose exec n8n n8n import:workflow --input=/files/workflows/repo-ops-daily-maintenance.json
dotnet build .\RepoOps.sln
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --run-once --emit-json-to-stdout --input-source=validation-locale
docker compose --profile maintenance run --rm renovate --version
docker compose down
```

Exemple de configuration minimale pour une exécution locale ciblée :

```powershell
$env:GITHUB_TOKEN="ghp_votre_jeton"
$env:RENOVATE_REPOSITORIES="owner/repo-a,owner/repo-b"
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --run-once --emit-json-to-stdout --input-source=test-local
```

Exemple de statut attendu en sortie :

```json
{
  "summary": {
    "status": "Success|Partial|Failed"
  },
  "pullRequestStatuses": {
    "readyForReview": [],
    "blocked": [],
    "failedChecks": [],
    "mergedRecently": [],
    "closedWithoutMerge": []
  }
}
```

Exemple de log utile en cas de qualification partielle :

```text
[github] Check-runs indisponibles pour owner/repo#123 : GitHub a répondu avec le statut HTTP 404
```
