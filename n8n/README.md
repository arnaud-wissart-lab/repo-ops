# n8n

Ce dossier accueille la partie orchestration du socle `repo-ops`.

## Rôle des workflows

Les workflows `n8n` versionnés dans ce dépôt servent à orchestrer la maintenance quotidienne sans embarquer d’identifiants ni de configuration propre à une instance locale.

Le flux retenu dans ce lot est volontairement simple :

- déclenchement quotidien ;
- création d’un trigger pour le worker `.NET` ;
- lecture du rapport JSON produit par le worker ;
- envoi d’un email à partir du digest déjà préparé.
- aucune exécution automatique de `Renovate` dans le workflow quotidien.

## Flux réel retenu

1. un `Cron` quotidien ou un déclenchement manuel démarre le workflow ;
2. un nœud `Code` prépare le contexte minimal ;
3. un nœud `Execute Command` purge les anciens artefacts puis écrit un fichier de déclenchement dans `/files/runtime` ;
4. un second nœud `Execute Command` attend ensuite un rapport JSON frais produit dans `/files/reports` ;
5. un nœud `Code` parse le JSON ;
6. un dernier nœud `Code` se limite à exposer le sujet, le texte et le HTML déjà fournis par le worker ;
7. un nœud `Email Send` envoie le récapitulatif.

Le rapport lu par `n8n` peut déjà inclure une section `renovateExecution` issue d’un run explicite supervisé en dehors du workflow quotidien.

## Prérequis

- la stack Docker du dépôt doit être démarrée ;
- un compte propriétaire doit avoir été créé dans `n8n` ;
- le nœud `Execute Command` doit rester autorisé dans l’instance locale ;
- un credential SMTP `Send Email` doit être créé manuellement dans `n8n` ;
- les champs `from` et `to` du workflow portent volontairement les marqueurs `__CONFIGURER_FROM_DANS_N8N__` et `__CONFIGURER_TO_DANS_N8N__` tant qu’ils n’ont pas été remplacés manuellement.

## Importer le workflow dans n8n

### Depuis l’interface

1. démarrer la stack avec `docker compose up -d` ;
2. ouvrir `n8n` via l’URL locale configurée ;
3. créer un nouveau workflow puis utiliser l’option d’import JSON ;
4. sélectionner [`workflows/repo-ops-daily-maintenance.json`](./workflows/repo-ops-daily-maintenance.json) ;
5. ouvrir le nœud `Envoyer le récapitulatif` et lui associer un credential SMTP ;
6. remplacer les marqueurs `__CONFIGURER_FROM_DANS_N8N__` et `__CONFIGURER_TO_DANS_N8N__` ;
7. lancer un test via `Déclenchement manuel`.

### Depuis la CLI n8n dans le conteneur

```powershell
docker compose exec n8n n8n import:workflow --input=/files/workflows/repo-ops-daily-maintenance.json
```

## Limites actuelles

- `n8n` ne déclenche pas encore le worker par une API dédiée ; le signal repose sur un fichier partagé ;
- la configuration SMTP reste manuelle ;
- les destinataires du workflow doivent être remplacés manuellement avant activation ;
- le workflow suppose que le worker écrit correctement ses artefacts dans `reports/` ;
- le déclenchement du worker repose encore sur un fichier partagé plutôt que sur une API dédiée.
- si vous voulez relancer `Renovate`, faites-le explicitement via le worker `.NET` ou via `docker compose`, pas depuis ce workflow.
