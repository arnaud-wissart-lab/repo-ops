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
Il calcule également des décisions d’auto-merge contrôlé sur les PR Renovate ouvertes et collecte une première vue des vulnérabilités via les `Dependabot alerts`.
Il embarque désormais une première couche de superviseur IA fondée sur des règles simples, qui transforme le rapport en décisions structurées sans exécuter ces décisions.

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
- déclenchement HTTP du worker ;
- lecture du rapport produit ;
- envoi de l’email.

Le workflow versionné ne reconstruit plus la synthèse métier. Il consomme la réponse JSON et les contenus déjà produits par le worker.

### Scripts transitoires

Le dossier `scripts/` reste présent pour la transition, mais il ne fait plus partie du flux réel retenu. Les scripts servent encore à des vérifications locales ou à un secours ponctuel, pas à la logique métier principale.

## Flux réel retenu

1. `n8n` déclenche le workflow quotidien.
2. Le workflow appelle `POST /maintenance/run` sur le worker via le réseau Docker interne.
3. Le worker `.NET` exécute le cycle complet et renvoie directement le JSON du rapport.
4. Le worker écrit également :
   - [`worker-summary.json`](./reports/worker-summary.json)
   - [`worker-summary.txt`](./reports/worker-summary.txt)
   - [`worker-summary.html`](./reports/worker-summary.html)
   - [`supervisor-decisions.json`](./reports/supervisor-decisions.json)
   - [`supervisor-decisions.txt`](./reports/supervisor-decisions.txt)
   - [`renovate-execution.json`](./reports/renovate-execution.json) lorsqu'une exécution explicite de `Renovate` est lancée via le worker
   Le worker interroge GitHub avec `GITHUB_TOKEN` pour récupérer les PR Renovate ouvertes, les PR Renovate fusionnées récemment, les fermetures sans fusion récentes, les checks utiles à la qualification opérationnelle, les `Dependabot alerts` ouvertes et corrigées quand elles sont disponibles, ainsi que les informations nécessaires à une décision d’auto-merge contrôlé.
5. `n8n` lit le JSON renvoyé directement par l’API du worker.
6. `n8n` envoie l’email en réutilisant directement le sujet, le texte et le HTML déjà préparés.

## Superviseur IA de premier niveau

Le superviseur introduit dans ce lot reste volontairement limité :

- il prend en entrée le rapport JSON du worker ;
- il applique des règles déterministes et explicables ;
- il produit une liste d’actions structurées et un digest dédié ;
- il n’exécute aucun merge, aucune PR et aucune commande externe.

Les types d’action actuellement produits sont :

- `AutoMergeEligible`
- `Review`
- `FixRequired`
- `Ignore`

Les règles initiales sont volontairement simples :

- PR `patch` avec checks verts et décision d’auto-merge positive : `AutoMergeEligible`
- PR `minor` : `Review`
- PR `major` : `Review` en priorité haute
- checks en échec : `FixRequired`
- vulnérabilité critique corrélée : priorité haute

Mode CLI :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --decide --report-path=reports/worker-summary.json --emit-json-to-stdout
```

Mode HTTP :

```powershell
Invoke-WebRequest -Uri "http://127.0.0.1:8080/supervisor/decisions" -Method Post -ContentType "application/json" -InFile ".\reports\worker-summary.json" | Select-Object -ExpandProperty Content
```

## Auto-merge contrôlé

Le worker applique une politique simple et prudente :

- seules les PR d’origine `Renovate` sont évaluées ;
- les checks doivent être verts ;
- la PR ne doit pas être en brouillon ;
- GitHub doit indiquer `mergeable = true` et `mergeable_state = clean` ;
- les mises à jour `major` restent en revue manuelle ;
- par défaut, seules les mises à jour `patch` sont éligibles à l’auto-merge ;
- des overrides par dépôt peuvent restreindre ou autoriser explicitement la politique.

La décision calculée pour chaque PR est l’une des suivantes :

- `AutoMerge`
- `ManualReview`
- `Blocked`
- `Failed`

Le merge réel reste désactivé par défaut. Le système produit d’abord la décision et, si l’option est activée, peut ensuite exécuter le merge via l’API GitHub.

### Overrides par dépôt

Le mécanisme retenu reste simple :

- politique globale via `RepoOps:AutoMerge` et les variables `AUTOMERGE_*` ;
- overrides optionnels par dépôt via un fichier JSON pointé par `AUTOMERGE_POLICY_FILE_PATH` ;
- exemple versionné : [automerge.policies.example.json](C:\Users\ArnaudW\source\repos\repo-ops\config\automerge.policies.example.json).

Chaque override peut préciser :

- `AllowAutoMerge`
- `ReviewRequired`
- `ReadOnly`
- `AllowedUpdateTypes`
- `MergeMethod`

## Couche sécurité

Le worker collecte une première vue des vulnérabilités connues via les `Dependabot alerts` GitHub :

- nombre d’alertes ouvertes ;
- nombre d’alertes corrigées si l’API les expose ;
- répartition par sévérité `critical`, `high`, `medium` et `low` ;
- liste d’alertes importantes visibles immédiatement dans le digest ;
- corrélation prudente avec certaines PR Renovate quand le package et une version corrigée connue sont visibles de manière fiable.

Cette corrélation est volontairement conservatrice. Si l’information n’est pas assez sûre, la PR n’est pas marquée comme corrective.

## Exécution explicite de Renovate

La stratégie retenue dans ce lot est la suivante :

- le workflow quotidien `n8n` ne déclenche pas `Renovate` ;
- l’exécution explicite de `Renovate` est supervisée par le worker `.NET` ;
- le worker lance la commande `docker compose --profile maintenance run --rm renovate`, capture le résultat, persiste un artefact dédié, puis l’intègre aux rapports suivants.

Commande recommandée pour lancer un cycle explicite complet avec supervision côté worker :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --run-once --run-renovate --emit-json-to-stdout --input-source=manual-renovate
```

Cette commande suppose qu'un fichier `.env` valable est présent à la racine du dépôt ou que les variables attendues par `docker compose` sont déjà chargées dans l'environnement du shell.

Commande directe conservée pour une exécution brute de `Renovate` sans enrichissement du rapport :

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
- [`config/automerge.policies.example.json`](C:\Users\ArnaudW\source\repos\repo-ops\config\automerge.policies.example.json) montre le format d’override par dépôt.
- [`n8n/README.md`](./n8n/README.md) décrit le rôle des workflows et leur import.
- [`n8n/workflows/repo-ops-daily-maintenance.json`](./n8n/workflows/repo-ops-daily-maintenance.json) fournit le workflow quotidien importable.
- [`tests/RepoOps.Worker.Tests`](C:\Users\ArnaudW\source\repos\repo-ops\tests\RepoOps.Worker.Tests) couvre la logique métier d’auto-merge.
- le dossier `scripts/` contient les passerelles et utilitaires transitoires ;
- le dossier `templates/` contient les modèles historiques de synthèse et de tâches.

## Démarrage rapide avec Docker Compose

1. Copier le fichier d’exemple et renseigner les variables requises :

   ```powershell
   Copy-Item .env.example .env
   ```

2. Définir au minimum :
   - `GITHUB_TOKEN`
     Ce jeton doit aussi permettre la lecture des `Dependabot alerts` si vous voulez enrichir la section sécurité.
   - `WORKER_HTTP_PORT`
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
- la couche sécurité dépend des `Dependabot alerts` exposées par l’API GitHub et des permissions effectives du jeton ;
- les PR ouvertes sont comptées à partir des PR Renovate détectées, pas à partir d’un historique d’exécution `Renovate` propre au dépôt ;
- les PR fusionnées sont lues dans une fenêtre glissante configurable, limitée au dernier lot de PR fermées renvoyé par l’API ;
- la qualification des PR ouvertes repose sur la combinaison des check-runs et du statut combiné GitHub sur la tête de PR ;
- les PR sans check décisif sont classées dans `pullRequestStatuses.blocked` avec une qualification incomplète ;
- le type de version utilisé pour la décision d’auto-merge est déduit des labels GitHub ou du titre de PR lorsqu’une comparaison sémantique est possible ;
- la corrélation entre PR Renovate et vulnérabilité reste volontairement stricte et peut manquer des cas pourtant pertinents ;
- l’auto-merge réel reste volontairement très conservateur : `mergeable_state = clean`, checks verts et politique explicite requise ;
- le merge réel n’est tenté que si `AUTOMERGE_ENABLED=true` et `AUTOMERGE_DRY_RUN_ENABLED=false` ;
- les overrides par dépôt reposent sur un matching exact `owner/repo` ;
- l’exécution explicite de `Renovate` supervisée par le worker doit être lancée depuis l’hôte, pas depuis le conteneur `worker` ;
- la qualification du run `Renovate` repose encore sur des heuristiques de logs `stdout` et `stderr` ;
- le worker expose désormais une API locale simple, mais sans authentification dédiée à ce stade car elle reste confinée au réseau Docker interne ;
- le superviseur IA actuel est uniquement un moteur de décisions à règles fixes ; il n’orchestre encore aucun agent ni aucune exécution de tâche ;
- la configuration SMTP et les destinataires restent à finaliser manuellement dans `n8n` ;
- le workflow quotidien exploite le dernier résultat connu de `Renovate`, mais ne le relance pas automatiquement ;
- la sortie du superviseur est distincte du rapport principal et n’est pas encore consommée par `n8n`.

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
Invoke-WebRequest -Uri "http://127.0.0.1:8080/health" | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "http://127.0.0.1:8080/maintenance/run" -Method Post -ContentType "application/json" -Body '{"inputSource":"validation-http","triggerRenovateExecution":false}' | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "http://127.0.0.1:8080/supervisor/decisions" -Method Post -ContentType "application/json" -InFile ".\reports\worker-summary.json" | Select-Object -ExpandProperty Content
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --run-once --emit-json-to-stdout --input-source=validation-cli
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --decide --report-path=reports/worker-summary.json --emit-json-to-stdout
dotnet run --project .\src\RepoOps.Worker -- --run-once --run-renovate --emit-json-to-stdout --input-source=validation-renovate
$env:AUTOMERGE_ENABLED="true"
$env:AUTOMERGE_DRY_RUN_ENABLED="true"
dotnet run --project .\src\RepoOps.Worker -- --run-once --enable-auto-merge --emit-json-to-stdout --input-source=validation-automerge
dotnet test .\RepoOps.sln
docker compose --profile maintenance run --rm renovate --version
docker compose down
```

Exemple de validation courte sans `.env` réel, en forçant le worker à appeler `docker compose` avec `--env-file .env.example` :

```powershell
$env:RENOVATE_EXECUTION_ARGUMENTS="compose --env-file .env.example --profile maintenance run --rm renovate --version"
dotnet run --project .\src\RepoOps.Worker -- --run-once --run-renovate --emit-json-to-stdout --input-source=validation-renovate-envfile
```

Exemple de configuration minimale pour une exécution locale ciblée :

```powershell
$env:GITHUB_TOKEN="ghp_votre_jeton"
$env:RENOVATE_REPOSITORIES="owner/repo-a,owner/repo-b"
Invoke-WebRequest -Uri "http://127.0.0.1:8080/maintenance/run" -Method Post -ContentType "application/json" -Body '{"inputSource":"test-http","triggerRenovateExecution":false}' | Select-Object -ExpandProperty Content
```

Exemple de simulation d’auto-merge sans merge réel :

```powershell
$env:GITHUB_TOKEN="ghp_votre_jeton"
$env:RENOVATE_REPOSITORIES="owner/repo-a"
$env:AUTOMERGE_ENABLED="true"
$env:AUTOMERGE_DRY_RUN_ENABLED="true"
$env:AUTOMERGE_ALLOWED_UPDATE_TYPES="patch"
dotnet run --project .\src\RepoOps.Worker -- --run-once --enable-auto-merge --emit-json-to-stdout --input-source=test-automerge-dryrun
```

Exemple d’activation explicite du merge réel :

```powershell
$env:GITHUB_TOKEN="ghp_votre_jeton"
$env:RENOVATE_REPOSITORIES="owner/repo-a"
$env:AUTOMERGE_ENABLED="true"
$env:AUTOMERGE_DRY_RUN_ENABLED="false"
$env:AUTOMERGE_ALLOWED_UPDATE_TYPES="patch"
dotnet run --project .\src\RepoOps.Worker -- --run-once --enable-auto-merge --disable-auto-merge-dry-run --emit-json-to-stdout --input-source=test-automerge-reel
```

Exemple d’override par dépôt :

```json
{
  "RepoOps": {
    "AutoMerge": {
      "RepositoryPolicies": [
        {
          "Repository": "owner/repo-pilote",
          "AllowAutoMerge": true,
          "ReviewRequired": false,
          "ReadOnly": false,
          "MergeMethod": "squash",
          "AllowedUpdateTypes": [ "patch" ]
        }
      ]
    }
  }
}
```

Exemple de statut attendu en sortie :

```json
{
  "summary": {
    "status": "Success|Partial|Failed"
  },
  "vulnerabilities": {
    "status": "Available|Partial|Unavailable",
    "openAlerts": 0,
    "fixedAlerts": 0,
    "criticalCount": 0,
    "highCount": 0,
    "mediumCount": 0,
    "lowCount": 0,
    "prioritizedPullRequests": [],
    "importantAlerts": [],
    "notes": [],
    "repositories": []
  },
  "renovateExecution": {
    "status": "NotTriggered|Succeeded|NoUpdatesDetected|PullRequestsUpdated|Failed",
    "triggerRequested": false,
    "includedFromLatestKnownExecution": true,
    "mode": "daily-report-last-known",
    "command": "docker compose --profile maintenance run --rm renovate"
  },
  "autoMerge": {
    "policyFilePath": "config/automerge.policies.json",
    "enabled": false,
    "dryRunEnabled": true,
    "mergeMethod": "squash",
    "allowedUpdateTypes": [ "patch" ],
    "allowedMergeableStates": [ "clean" ],
    "readyForMerge": [],
    "manualReviewPullRequests": [],
    "blockedPullRequests": [],
    "failedPullRequests": [],
    "autoMergedPullRequests": [],
    "evaluations": [
      {
        "repository": "owner/repo-a",
        "decision": "ManualReview",
        "policySource": "repository:owner/repo-a",
        "reasons": []
      }
    ]
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
[renovate] INFO: Repository started
```

Exemple de sortie du superviseur :

```json
{
  "sourceReportStatus": "Partial",
  "summary": {
    "totalActions": 3,
    "reviewActions": 1,
    "autoMergeEligibleActions": 1,
    "fixRequiredActions": 1,
    "ignoreActions": 0,
    "highPriorityActions": 2
  },
  "actions": [
    {
      "type": "AutoMergeEligible",
      "repository": "owner/repo-a",
      "pullRequestNumber": 42,
      "priority": "High",
      "reason": "La mise à jour patch est prête, avec checks verts et décision d'auto-merge positive. La PR est corrélée à une vulnérabilité critique et doit être priorisée."
    }
  ]
}
```
