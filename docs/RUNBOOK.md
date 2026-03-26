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
- vérification post-déploiement : `https://repoops.arnaudwissart.fr`
- timeout du healthcheck : `300` secondes, polling toutes les `5` secondes

Si `.env` est absent :

- le script le crée depuis `.env.example`
- il faut ensuite personnaliser les secrets avant un déploiement réellement exploitable

## Démarrage manuel home hors workflow

```bash
docker compose --env-file .env -p repo-ops-home -f docker-compose.yml up -d --build --remove-orphans
```

## Vérifications opérationnelles

```bash
docker compose --env-file .env -p repo-ops-home -f docker-compose.yml ps
docker compose --env-file .env -p repo-ops-home -f docker-compose.yml logs --tail 120 worker postgres n8n
curl -I -L https://repoops.arnaudwissart.fr
```

## Points de vigilance

- le workflow de déploiement manuel est la voie principale de déploiement ; le bouton local de la démo frontend reste un outil de démonstration et de vérification locale ;
- le dépôt cible doit disposer d’un `.env` réellement renseigné, sans quoi le déploiement pourra démarrer techniquement mais restera inutilisable ;
- le healthcheck public valide l’accessibilité du domaine, pas la complétude fonctionnelle de chaque sous-service.
