# repo-ops

`repo-ops` est un socle d’automatisation destiné à maintenir plusieurs dépôts GitHub publics personnels de manière progressive, traçable et réversible. Le premier objectif est de centraliser la détection de dépendances obsolètes ou vulnérables, l’ouverture de pull requests de maintenance, l’orchestration planifiée et la synthèse quotidienne par email.

## Composants

- `Renovate` self-hosted exécute les scans de dépendances et ouvre des pull requests de mise à jour sur une allowlist explicite de dépôts.
- `n8n` orchestre les déclenchements, les collectes de résultats et les envois de synthèse.
- Les scripts du dossier `scripts/` préparent la collecte consolidée et l’envoi du résumé.
- Les templates du dossier `templates/` fournissent la base HTML et texte brut des emails quotidiens.

## Stratégie de déploiement

Le démarrage se fait volontairement sur quelques dépôts seulement, via `RENOVATE_REPOSITORIES`. L’objectif n’est pas de brancher immédiatement tous les dépôts personnels, mais d’installer une boucle fiable sur une allowlist courte, puis d’élargir progressivement le périmètre.

Cycle cible :

`scan -> PR -> CI GitHub -> synthèse email`

## Démarrage rapide

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

6. Pour une exécution ponctuelle hors boucle planifiée, lancer :

   ```powershell
   docker compose run --rm --entrypoint /bin/sh renovate -lc "exec renovate"
   ```

## Structure du dépôt

- [`docker-compose.yml`](./docker-compose.yml) décrit la stack locale.
- [`renovate/config.js`](./renovate/config.js) porte la configuration self-hosted d’administration.
- [`docs/architecture.md`](./docs/architecture.md) décrit les responsabilités et les flux.
- [`docs/rollout-plan.md`](./docs/rollout-plan.md) découpe l’adoption en phases.

## Prochaines étapes

Une évolution naturelle du socle est l’ajout d’un superviseur IA de delivery capable de prioriser les dépôts, d’interpréter les échecs de CI et de proposer des actions manuelles plus fines. Cette brique est volontairement hors périmètre du premier jet afin de garder une base simple, lisible et directement exploitable.
