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
Il sait également transformer ces décisions en prompts structurés prêts à être utilisés manuellement dans Codex.

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

### Observabilité légère

Le worker ajoute une couche d’observabilité locale sans service externe :

- historique JSON des runs sous `reports/history/` ;
- index léger des derniers runs ;
- métriques simples pour suivre le comportement du système dans le temps ;
- logs structurés autour d’un `runId`.

Les métriques actuellement suivies sont :

- nombre de PR analysées ;
- nombre de PR auto-mergées ;
- nombre de PR bloquées ;
- nombre d’erreurs détectées côté exécution.

### UI de démonstration

Le dépôt inclut aussi une interface web de démonstration en `React + Vite + TypeScript` dans [`demo-ui`](./demo-ui).

Cette UI permet de :

- lancer un run sec ;
- afficher le résumé global ;
- visualiser les décisions du superviseur ;
- consulter les prompts générés ;
- rappeler explicitement les garde-fous de sécurité.

Elle reste volontairement en mode démonstration :

- aucun commit ;
- aucun push ;
- aucune pull request créée ;
- aucune action irréversible.

### Scripts transitoires

Le dossier `scripts/` reste présent pour la transition, mais il ne fait plus partie du flux réel retenu. Les scripts servent encore à des vérifications locales ou à un secours ponctuel, pas à la logique métier principale.

## Consultation de l’historique

Le worker expose un mode CLI léger pour consulter les derniers runs sans dépendance externe :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --show-runs
```

Pour limiter le nombre d’entrées affichées :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --show-runs --show-runs-count=5
```

Avec sortie JSON :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --show-runs --show-runs-count=5 --emit-json-to-stdout
```

Si vous consultez l’historique via le binaire compilé depuis la racine du dépôt, vous pouvez fixer explicitement l’index :

```powershell
$env:WORKER_RUN_HISTORY_INDEX_PATH="$PWD\src\RepoOps.Worker\reports\history\index.json"
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --show-runs --show-runs-count=5
```

Exemple de sortie texte :

```text
Runs affichés : 2
- 2026-03-26 09:00:00 UTC | Success | docker-compose | durée 1842 ms
  PR analysées : 4, auto-mergées : 1, bloquées : 1, erreurs : 0
  Rapport : C:\repo-ops\reports\history\20260326-090000-run-....json
- 2026-03-25 09:00:00 UTC | Partial | http-api | durée 2965 ms
  PR analysées : 3, auto-mergées : 0, bloquées : 2, erreurs : 1
  Rapport : C:\repo-ops\reports\history\20260325-090000-run-....json
```

## Flux réel retenu

1. `n8n` déclenche le workflow quotidien.
2. Le workflow appelle `POST /maintenance/run` sur le worker via le réseau Docker interne.
3. Le worker `.NET` exécute le cycle complet et renvoie directement le JSON du rapport.
4. Le worker écrit également :
   - [`worker-summary.json`](./reports/worker-summary.json)
   - [`worker-summary.txt`](./reports/worker-summary.txt)
   - [`worker-summary.html`](./reports/worker-summary.html)
   - [`reports/history/index.json`](./reports/history/index.json)
   - des snapshots complets horodatés sous [`reports/history/`](./reports/history)
   - [`supervisor-decisions.json`](./reports/supervisor-decisions.json)
   - [`supervisor-decisions.txt`](./reports/supervisor-decisions.txt)
   - [`supervisor-prompts.json`](./reports/supervisor-prompts.json)
   - [`supervisor-prompts.txt`](./reports/supervisor-prompts.txt)
   - [`supervisor-codex-responses.json`](./reports/supervisor-codex-responses.json)
   - [`supervisor-codex-responses.txt`](./reports/supervisor-codex-responses.txt)
   - [`supervisor-validations.json`](./reports/supervisor-validations.json)
   - [`supervisor-validations.txt`](./reports/supervisor-validations.txt)
   - [`supervisor-commit-executions.json`](./reports/supervisor-commit-executions.json)
   - [`supervisor-commit-executions.txt`](./reports/supervisor-commit-executions.txt)
   - [`renovate-execution.json`](./reports/renovate-execution.json) lorsqu'une exécution explicite de `Renovate` est lancée via le worker
   Le worker interroge GitHub avec `GITHUB_TOKEN` pour récupérer les PR Renovate ouvertes, les PR Renovate fusionnées récemment, les fermetures sans fusion récentes, les checks utiles à la qualification opérationnelle, les `Dependabot alerts` ouvertes et corrigées quand elles sont disponibles, ainsi que les informations nécessaires à une décision d’auto-merge contrôlé.
5. `n8n` lit le JSON renvoyé directement par l’API du worker.
6. `n8n` envoie l’email en réutilisant directement le sujet, le texte et le HTML déjà préparés.

Chaque run conserve aussi :

- un `runId` ;
- un statut global ;
- une durée d’exécution ;
- un snapshot de métriques d’observabilité.

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

## Prompt Generator

Le `Prompt Generator` consomme les actions du `Decision Engine` et produit des prompts prêts à être utilisés manuellement avec Codex.

Chaque prompt inclut :

- un contexte clair ;
- un objectif ;
- des contraintes ;
- une sortie attendue ;
- un résumé du problème ;
- l’état des checks ;
- une recommandation.

Templates actuellement gérés :

- `fix-required` : prompt de correction
- `review` : prompt d’analyse
- `auto-merge-eligible` : prompt de validation finale
- `vulnerability-priority` : prompt prioritaire lié à la sécurité

Mode CLI à partir d’un rapport :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --generate-prompts --report-path=reports/worker-summary.json --emit-json-to-stdout
```

Mode CLI à partir des décisions déjà générées :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --generate-prompts --decisions-path=reports/supervisor-decisions.json --emit-json-to-stdout
```

Mode HTTP à partir des décisions :

```powershell
Invoke-WebRequest -Uri "http://127.0.0.1:8080/supervisor/prompts" -Method Post -ContentType "application/json" -InFile ".\reports\supervisor-decisions.json" | Select-Object -ExpandProperty Content
```

Note pratique :

- sous `docker compose`, les artefacts sont écrits dans `.\reports\` ;
- avec `dotnet run --project .\src\RepoOps.Worker`, les chemins par défaut aboutissent sous `.\src\RepoOps.Worker\reports\` tant qu’ils ne sont pas surchargés.

Usage avec Codex :

- générer les prompts ;
- relire la priorité et le contexte ;
- choisir explicitement le prompt à utiliser ;
- coller ce prompt dans Codex pour obtenir une analyse, une revue ou une proposition de correction ;
- garder la décision humaine sur l’exécution réelle.

## Codex Executor contrôlé

L’exécuteur contrôlé consomme les prompts déjà générés et produit une réponse structurée, sans jamais exécuter automatiquement de modification.

Principes retenus :

- le worker reste le point d’entrée ;
- l’intégration passe par une interface `ICodexClient` ;
- le mode actif par défaut est `Stub` ;
- aucune API externe n’est appelée tant qu’aucun client réel n’est branché ;
- chaque réponse porte `requiresHumanValidation=true` et `readyForExecution=false`.

Sorties produites :

- [`supervisor-codex-responses.json`](./reports/supervisor-codex-responses.json)
- [`supervisor-codex-responses.txt`](./reports/supervisor-codex-responses.txt)

Types de réponse actuellement structurés :

- `Analysis`
- `ProposedFix`
- `Refactor`

Mode CLI à partir des prompts déjà générés :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --execute-prompts --prompts-path=reports/supervisor-prompts.json --emit-json-to-stdout
```

Mode HTTP à partir d’un JSON de prompts :

```powershell
Invoke-WebRequest -Uri "http://127.0.0.1:8080/supervisor/codex/execute" -Method Post -ContentType "application/json" -InFile ".\reports\supervisor-prompts.json" | Select-Object -ExpandProperty Content
```

Exemple de réponse structurée :

```json
{
  "executorMode": "Stub",
  "summary": {
    "totalResponses": 1,
    "analysisResponses": 1,
    "proposedFixResponses": 0,
    "refactorResponses": 0
  },
  "responses": [
    {
      "actionId": "owner-repo-a-42-review",
      "repository": "owner/repo-a",
      "pullRequestNumber": 42,
      "promptType": "review",
      "responseType": "Analysis",
      "confidenceLevel": "Medium",
      "requiresHumanValidation": true,
      "readyForExecution": false
    }
  ]
}
```

## Validation Engine

Le `Validation Engine` ajoute une couche de contrôle humain explicite entre les réponses structurées issues de l’exécuteur contrôlé et une éventuelle exécution future.

Principes retenus :

- aucune action n’est exécutée dans ce lot ;
- une validation produit uniquement une décision humaine structurée ;
- une action approuvée passe à `readyForExecution=true` sans effet automatique ;
- le flux reste strictement manuel et auditable.

Décisions possibles :

- `Approved`
- `Rejected`
- `NeedsReview`

Sorties produites :

- [`supervisor-validations.json`](./reports/supervisor-validations.json)
- [`supervisor-validations.txt`](./reports/supervisor-validations.txt)

Mode CLI interactif :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --validate-responses --responses-path=reports/supervisor-codex-responses.json --interactive=true --emit-json-to-stdout
```

Mode CLI non interactif à partir d’un fichier de validation :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --validate-responses --responses-path=reports/supervisor-codex-responses.json --validation-input-path=reports/supervisor-validations.json --emit-json-to-stdout
```

Workflow humain retenu :

1. produire le rapport ;
2. produire les décisions superviseur ;
3. produire les prompts ;
4. produire les réponses structurées du client Codex simulé ;
5. relire ces réponses ;
6. approuver, rejeter ou marquer à revoir ;
7. préparer un futur `readyForExecution` sans exécution effective.

Exemple de validation structurée :

```json
{
  "summary": {
    "totalActions": 1,
    "approvedActions": 1,
    "rejectedActions": 0,
    "needsReviewActions": 0,
    "readyForExecutionActions": 1
  },
  "decisions": [
    {
      "actionId": "owner-repo-a-42-review",
      "decision": "Approved",
      "comment": "Validation humaine explicite après relecture.",
      "readyForExecution": true
    }
  ]
}
```

## Commit Engine

Le `Commit Engine` ajoute une première couche d’exécution contrôlée après validation humaine explicite.

Principes retenus :

- aucune exécution sans décision `Approved` et `readyForExecution=true` ;
- aucun push direct vers `main` ou `master` ;
- branche dédiée obligatoire pour chaque action ;
- mode `dry-run` activé par défaut ;
- aucun commit automatique tant qu’un patch unifié structuré n’est pas disponible dans la réponse associée ;
- aucun workspace local n’est découvert automatiquement : un mapping dépôt -> chemin local doit être fourni explicitement ;
- toute exécution passe par un clone temporaire dédié ;
- le patch est contrôlé avant application et refusé s’il est ambigu, invalide ou incohérent ;
- une validation avant commit est tentée dans le clone temporaire, avec `dotnet build --nologo` par défaut pour un dépôt `.NET`.

Sorties produites :

- [`supervisor-commit-executions.json`](./reports/supervisor-commit-executions.json)
- [`supervisor-commit-executions.txt`](./reports/supervisor-commit-executions.txt)

Le moteur prend en entrée :

- les validations humaines structurées ;
- les réponses structurées du client Codex ;
- un fichier de mapping local des workspaces.

Flux sécurisé retenu :

1. résolution du dépôt source et de son remote ;
2. clonage dans un répertoire temporaire dédié ;
3. contrôle strict du patch et des fichiers ciblés ;
4. application du patch dans le clone temporaire ;
5. validation avant commit ;
6. dry-run détaillé ou, si explicitement activé, `commit + push + pull request`.

Exemple de mapping local :

```json
{
  "repositories": [
    {
      "repository": "owner/repo-a",
      "localPath": "C:/dev/repo-a",
      "baseBranch": "main"
    }
  ]
}
```

Exemple d’exécution en dry-run :

```powershell
dotnet run --project .\src\RepoOps.Worker -- --execute-validated --commit-validation-result-path=reports/supervisor-validations.json --commit-responses-path=reports/supervisor-codex-responses.json --commit-workspace-map-path=config/repository-workspaces.example.json --emit-json-to-stdout
```

Activation réelle, uniquement sur un dépôt pilote et après revue humaine :

```powershell
$env:COMMIT_ENGINE_ALLOW_REAL_EXECUTION="true"
$env:COMMIT_ENGINE_DRY_RUN_ENABLED="false"
dotnet run --project .\src\RepoOps.Worker -- --execute-validated --enable-real-commit-execution --commit-validation-result-path=reports/supervisor-validations.json --commit-responses-path=reports/supervisor-codex-responses.json --commit-workspace-map-path=config/repository-workspaces.json --emit-json-to-stdout
```

Le mode réel reste inutilisable tant qu’une réponse structurée ne contient pas `proposedUnifiedDiff`. Le client `Stub` actuel n’en produit pas. Cette étape doit donc être vue comme une enveloppe d’exécution contrôlée prête à être branchée sur un client plus riche, pas comme une automatisation autonome.

Exemple de logs auditables :

```text
Clone temporaire : C:\Temp\repo-ops-commit-engine\repo-test-1234
Fetch de origin/main
Création de branche repo-ops/fix-pr-101
Fichiers ciblés : README.md, src/Service.cs
Validation avant commit : Succeeded
Commande de validation : dotnet build --nologo
Dry-run : création de branche repo-ops/fix-pr-101
Dry-run : commit 'fix(maintenance): applique la correction validée'
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
- [`demo-ui`](./demo-ui) contient l’interface web de démonstration.
- [`.env.example`](./.env.example) centralise les variables attendues.
- [`AGENTS.md`](./AGENTS.md) fixe les règles de travail pour les contributions futures.
- [`renovate/config.js`](./renovate/config.js) porte la configuration self-hosted d’administration.
- [`docs/architecture.md`](./docs/architecture.md) décrit les responsabilités et les flux.
- [`docs/rollout-plan.md`](./docs/rollout-plan.md) découpe l’adoption en phases.
- [`config/automerge.policies.example.json`](C:\Users\ArnaudW\source\repos\repo-ops\config\automerge.policies.example.json) montre le format d’override par dépôt.
- [`config/repository-workspaces.example.json`](C:\Users\ArnaudW\source\repos\repo-ops\config\repository-workspaces.example.json) montre le format de mapping dépôt -> workspace local pour le `Commit Engine`.
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

## Démonstration web

L’interface de démonstration vit dans [`demo-ui`](./demo-ui) et utilise un proxy Vite vers le worker local.

Lancement :

```powershell
dotnet run --project .\src\RepoOps.Worker
cd .\demo-ui
npm install
npm run dev
```

L’URL de développement est généralement :

```text
http://127.0.0.1:5173
```

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
- le générateur de prompts prépare des prompts prêts à l’emploi, mais ne contacte jamais Codex et ne déclenche aucune exécution ;
- l’exécuteur contrôlé produit aujourd’hui des réponses simulées et structurées via un client `Stub`, sans appeler réellement Codex ;
- aucune réponse générée n’est exécutable directement ; une validation humaine reste obligatoire avant toute utilisation ;
- le moteur de validation humaine prépare un état `readyForExecution`, mais aucune exécution automatique n’existe encore dans le dépôt ;
- la configuration SMTP et les destinataires restent à finaliser manuellement dans `n8n` ;
- le workflow quotidien exploite le dernier résultat connu de `Renovate`, mais ne le relance pas automatiquement ;
- les sorties du superviseur et des prompts restent distinctes du rapport principal et ne sont pas encore consommées par `n8n`.
- le `Commit Engine` reste en `dry-run` par défaut et exige un mapping explicite des workspaces locaux ;
- le client `Stub` du `Codex Executor` ne produit pas encore de `proposedUnifiedDiff`, ce qui entraîne des opérations ignorées tant qu’un patch structuré n’est pas fourni ;
- la création réelle de branche, de commit, de push et de pull request n’a de sens que sur un dépôt pilote explicitement déclaré dans le mapping local ;
- le `Commit Engine` refuse désormais les dépôts sources sales, les patchs ambigus et les validations avant commit en échec ;
- le `Commit Engine` clone le dépôt dans un répertoire temporaire et nettoie ce workspace à la fin de l’exécution ;
- en cas d’échec après `push`, la branche distante peut déjà exister et doit être vérifiée manuellement.
- l’observabilité reste volontairement locale et basée sur des fichiers JSON ; il n’y a pas encore de tableau de bord dédié ni d’agrégation temporelle avancée ;
- le compteur d’erreurs reste volontairement simple et reflète surtout les erreurs d’exécution structurées déjà connues par le worker.

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
Invoke-WebRequest -Uri "http://127.0.0.1:8080/supervisor/prompts" -Method Post -ContentType "application/json" -InFile ".\src\RepoOps.Worker\reports\supervisor-decisions.json" | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "http://127.0.0.1:8080/supervisor/codex/execute" -Method Post -ContentType "application/json" -InFile ".\src\RepoOps.Worker\reports\supervisor-prompts.json" | Select-Object -ExpandProperty Content
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --run-once --emit-json-to-stdout --input-source=validation-cli
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --decide --report-path=reports/worker-summary.json --emit-json-to-stdout
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --generate-prompts --decisions-path=reports/supervisor-decisions.json --emit-json-to-stdout
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --execute-prompts --prompts-path=reports/supervisor-prompts.json --emit-json-to-stdout
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --validate-responses --responses-path=reports/supervisor-codex-responses.json --validation-input-path=reports/supervisor-validations.json --emit-json-to-stdout
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --execute-validated --commit-validation-result-path=reports/supervisor-validations.json --commit-responses-path=reports/supervisor-codex-responses.json --commit-workspace-map-path=config/repository-workspaces.example.json --emit-json-to-stdout
dotnet .\src\RepoOps.Worker\bin\Debug\net10.0\RepoOps.Worker.dll --show-runs --show-runs-count=5
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

Exemple de prompt généré :

```json
{
  "promptType": "fix-required",
  "repository": "owner/repo-a",
  "pullRequestNumber": 101,
  "priority": "High",
  "context": {
    "problemSummary": "Les checks GitHub sont en échec ou la décision d'auto-merge a échoué.",
    "checksStatus": "en échec",
    "recommendation": "Traiter la cause d'échec ou le sujet de sécurité avant toute autre action."
  },
  "promptText": "Contexte\n- Dépôt cible : owner/repo-a\n- Cible précise : owner/repo-a#101\n..."
}
```

Exemple de réponse structurée issue de l’exécuteur contrôlé :

```json
{
  "executorMode": "Stub",
  "responses": [
    {
      "actionId": "owner-repo-a-101-fix-required",
      "repository": "owner/repo-a",
      "pullRequestNumber": 101,
      "responseType": "ProposedFix",
      "confidenceLevel": "Medium",
      "requiresHumanValidation": true,
      "readyForExecution": false,
      "summary": "Réponse simulée orientée correction, à relire avant toute modification manuelle."
    }
  ]
}
```

Exemple de fichier de validation humaine :

```json
{
  "decisions": [
    {
      "actionId": "owner-repo-a-101-fix-required",
      "decision": "Approved",
      "comment": "Accord humain après relecture du diagnostic.",
      "timestampUtc": "2026-03-26T11:00:00Z"
    },
    {
      "actionId": "owner-repo-b-42-review",
      "decision": "NeedsReview",
      "comment": "Une revue fonctionnelle complémentaire reste nécessaire.",
      "timestampUtc": "2026-03-26T11:05:00Z"
    }
  ]
}
```

Exemple de sortie du `Commit Engine` en dry-run :

```json
{
  "dryRunEnabled": true,
  "summary": {
    "totalOperations": 1,
    "successfulOperations": 0,
    "failedOperations": 0,
    "skippedOperations": 1,
    "pullRequestsCreated": 0,
    "dryRunOperations": 1
  },
  "operations": [
    {
      "actionId": "owner-repo-a-101-fix-required",
      "repository": "owner/repo-a",
      "branchName": "repo-ops/fix-pr-101",
      "status": "Skipped",
      "dryRun": true
    }
  ]
}
```

Procédure prudente pour passer en mode réel sur un dépôt pilote :

1. préparer un fichier de mapping local limité au dépôt pilote ;
2. vérifier que le dépôt source local est propre ;
3. conserver `COMMIT_ENGINE_DRY_RUN_ENABLED=true` pour un premier passage ;
4. relire le rapport JSON et le digest texte du `Commit Engine` ;
5. activer ensuite seulement :

```powershell
$env:COMMIT_ENGINE_ALLOW_REAL_EXECUTION="true"
$env:COMMIT_ENGINE_DRY_RUN_ENABLED="false"
```

6. relancer le même jeu d’entrées validées ;
7. contrôler la branche créée et la PR générée avant toute suite.
