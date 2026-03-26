# RUNBOOK Exploitation

Ce document regroupe les détails d’exploitation volontairement exclus du README vitrine.

## Déploiement manuel via GitHub Actions

Workflow : [`.github/workflows/deploy-manual.yml`](../.github/workflows/deploy-manual.yml)

Paramètres `workflow_dispatch` :

- `environment` (défaut : `home`)
- `ref` (défaut : `main`)
- `debug` (défaut : `true`)

Runner cible :

- labels : `[self-hosted, linux, ci]`

Secrets requis côté GitHub :

- `SSH_HOST`
- `SSH_USER`
- `SSH_PRIVATE_KEY`
- `SSH_PORT` (optionnel, défaut `22`)

Script appelé par le workflow :

- [`scripts/deploy-home.sh`](../scripts/deploy-home.sh)

Le workflow ajoute aussi un préflight minimal :

- présence des secrets SSH ;
- présence des commandes `ssh`, `ssh-keyscan`, `bash` et `git` sur le runner ;
- visibilité du script de déploiement ;
- ajout explicite de `known_hosts`.

## CI GitHub Actions

Workflow : [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

Le workflow vérifie :

- la restauration, le build et les tests de [`RepoOps.sln`](../RepoOps.sln) ;
- la cohérence du format whitespace ;
- le build du frontend de démonstration dans [`demo-ui`](../demo-ui) ;
- la validité de [`docker-compose.yml`](../docker-compose.yml) avec [`.env.example`](../.env.example).

## Cible `home` actuelle

Le script de déploiement utilise :

- dossier applicatif : `/home/arnaud/apps/repo-ops`
- compose : `docker-compose.yml`
- fichier d’environnement : `.env`
- fichier d’exemple : `.env.example`
- projet compose : `repo-ops-home`
- publication locale de la démo : `127.0.0.1:8084`
- vérification locale post-déploiement : `http://127.0.0.1:8084`
- vérification post-déploiement : `https://repoops.arnaudwissart.fr`
- timeout du healthcheck : `300` secondes, polling toutes les `5` secondes

Si `.env` est absent :

- le script le crée depuis `.env.example`
- il faut ensuite personnaliser les secrets avant un déploiement réellement exploitable

Si `.env` existe déjà mais ne contient pas `DEMO_UI_PORT` :

- le script ajoute automatiquement `DEMO_UI_PORT=8084`
- cela évite les publications Docker sur port aléatoire lors des mises à jour d’un ancien environnement

## Démarrage manuel home hors workflow

```bash
docker compose --env-file .env -p repo-ops-home -f docker-compose.yml up -d --build --remove-orphans
```

## Vérifications opérationnelles

```bash
docker compose --env-file .env -p repo-ops-home -f docker-compose.yml ps
docker compose --env-file .env -p repo-ops-home -f docker-compose.yml logs --tail 120 demo-ui worker postgres n8n
curl -fsSL http://127.0.0.1:8084 | grep "RepoOps Live Demo"
curl -fsSL https://repoops.arnaudwissart.fr | grep "RepoOps Live Demo"
```

## Points de vigilance

- le workflow de déploiement manuel est la voie principale de déploiement ; le bouton local de la démo frontend reste un outil de démonstration et de vérification locale ;
- le dépôt cible doit disposer d’un `.env` réellement renseigné, sans quoi le déploiement pourra démarrer techniquement mais restera inutilisable ;
- le healthcheck public ne se contente plus d’un simple `200` : il vérifie la présence du marqueur `RepoOps Live Demo`.
