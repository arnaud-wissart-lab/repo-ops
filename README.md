# repo-ops

`repo-ops` est un socle d’automatisation destiné à maintenir plusieurs dépôts GitHub publics personnels de manière progressive, traçable et réversible. Le dépôt est désormais recentré autour d’une couche métier `.NET`, tout en conservant `Docker Compose` comme base d’exécution réelle, `n8n` pour l’orchestration simple et `Renovate` pour la maintenance des dépendances.

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

`docker-compose.yml` reste la base d’exécution réelle du socle en local. Il démarre :

- `worker`, futur cœur métier `.NET` ;
- `postgres`, utilisé par `n8n` ;
- `n8n`, pour les cron, les notifications et l’orchestration simple ;
- `renovate`, pour les scans et l’ouverture de PR.

### Worker .NET

[`src/RepoOps.Worker`](./src/RepoOps.Worker) porte la future logique métier du socle :

- collecte des résultats ;
- consolidation ;
- génération de synthèse ;
- future base pour la supervision plus avancée.

À ce stade, le worker fournit un squelette exécutable, typé et journalisé. Il produit un rapport placeholder local sans simuler de branchement GitHub réel.

### Aspire AppHost

[`src/RepoOps.AppHost`](./src/RepoOps.AppHost) ajoute une couche de pilotage local et de visualisation dans Visual Studio. Cette couche est utile pour :

- lancer et observer localement le worker ;
- visualiser `postgres` et `n8n` dans le tableau de bord Aspire ;
- préparer une expérience de développement plus cohérente autour de la stack `.NET`.

Aspire n’est pas le runtime de production du socle. La référence d’exécution réelle reste `Docker Compose`.

### n8n

`n8n` conserve un rôle ciblé :

- déclenchements planifiés ;
- enchaînements simples ;
- notifications ;
- import de workflows JSON versionnés.

Le workflow quotidien versionné reste actuellement appuyé sur les scripts du dossier `scripts/` pour préserver une compatibilité immédiate avec l’existant.

### Scripts transitoires

Le dossier `scripts/` reste présent comme couche de transition. Ces scripts ne constituent plus la cible principale du dépôt. Ils servent encore à :

- fournir un contrat simple pour `n8n` ;
- permettre des tests locaux sans dépendre d’une implémentation métier complète ;
- accompagner la migration progressive vers le worker `.NET`.

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
- [`n8n/workflows/repo-ops-daily-maintenance.json`](./n8n/workflows/repo-ops-daily-maintenance.json) fournit un premier workflow quotidien importable.
- le dossier `scripts/` contient les passerelles transitoires de collecte et d’envoi ;
- le dossier `templates/` contient les modèles de synthèse et de tâches.

## Démarrage rapide avec Docker Compose

1. Copier le fichier d’exemple et renseigner les variables requises :

   ```powershell
   Copy-Item .env.example .env
   ```

2. Définir au minimum :
   - `GITHUB_TOKEN`
   - `RENOVATE_REPOSITORIES`
   - `N8N_ENCRYPTION_KEY`
   - `POSTGRES_PASSWORD`
   - les variables SMTP si l’envoi d’email doit être activé rapidement

3. Démarrer la stack :

   ```powershell
   docker compose up -d
   ```

4. Accéder à `n8n` via l’URL exposée par `N8N_EDITOR_BASE_URL`, puis créer le compte propriétaire initial demandé par l’interface.

5. Vérifier d’abord le comportement de `Renovate` en mode prudent avec `RENOVATE_DRY_RUN=full`, puis retirer ce mode une fois l’allowlist validée.

6. Consulter les rapports placeholder produits par le worker dans `reports/`.

## Démarrage local avec Aspire et Visual Studio

1. Ouvrir [`RepoOps.sln`](./RepoOps.sln) dans Visual Studio 2022 ou plus récent avec le support Aspire installé.
2. Configurer au minimum `POSTGRES_PASSWORD` et `N8N_ENCRYPTION_KEY` via des variables d’environnement locales ou via `dotnet user-secrets` sur le projet AppHost.
3. Définir [`src/RepoOps.AppHost`](./src/RepoOps.AppHost) comme projet de démarrage.
4. Lancer l’application pour ouvrir le tableau de bord Aspire et observer les ressources locales.

Exemple avec `dotnet user-secrets` :

```powershell
dotnet user-secrets --project .\src\RepoOps.AppHost set POSTGRES_PASSWORD "changez-moi"
dotnet user-secrets --project .\src\RepoOps.AppHost set N8N_ENCRYPTION_KEY "changez-moi-aussi"
```

Cette couche Aspire sert au pilotage local. Pour les exécutions réelles de la stack, conserver `docker compose`.

## Vérifications locales

Commandes minimales à exécuter après avoir renseigné `.env` :

```powershell
Copy-Item .env.example .env
docker compose config
docker compose up -d --build --wait
docker compose ps
docker compose logs --tail=100 worker postgres n8n renovate
docker compose exec postgres sh -lc 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
docker compose exec n8n n8n import:workflow --input=/files/workflows/repo-ops-daily-maintenance.json
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\collect-results.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\send-summary.ps1
dotnet build .\src\RepoOps.Worker\RepoOps.Worker.csproj
dotnet build .\src\RepoOps.AppHost\RepoOps.AppHost.csproj
docker compose run --rm --entrypoint /bin/sh renovate -lc "renovate --version"
docker compose down
```

## Prochaines étapes

La trajectoire naturelle du dépôt reste l’ajout d’un superviseur IA de delivery capable de piloter des tâches incrémentales sur plusieurs dépôts. Cette brique n’est pas implémentée maintenant. Le socle actuel prépare surtout :

- une couche métier `.NET` claire et extensible ;
- une séparation nette entre runtime réel et pilotage local ;
- un terrain propre pour de futures validations et responsabilités spécialisées.
