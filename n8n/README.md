# n8n

Ce dossier accueille la partie orchestration du socle `repo-ops`.

## Rôle des workflows

Les workflows `n8n` versionnés dans ce dépôt servent à orchestrer la maintenance quotidienne sans embarquer d’identifiants ni de configuration propre à une instance locale.

Le premier workflow livré couvre la chaîne minimale suivante :

- déclenchement quotidien ;
- préparation du contexte d’exécution ;
- appel d’un script de collecte local ;
- génération d’une synthèse textuelle et HTML ;
- envoi d’un email récapitulatif.

Dans l’architecture actuelle, ce workflow reste volontairement simple et s’appuie encore sur les scripts transitoires du dépôt. La cible à moyen terme est de déléguer la collecte et la consolidation au worker `.NET`, tout en conservant `n8n` pour les cron et les notifications.

## Workflow quotidien

Le fichier [`workflows/repo-ops-daily-maintenance.json`](./workflows/repo-ops-daily-maintenance.json) contient un workflow de base prévu pour être importé dans `n8n`.

Logique du workflow :

1. un `Cron` quotidien déclenche le traitement chaque matin ;
2. un déclenchement manuel permet de tester le flux sans attendre le prochain créneau ;
3. un nœud de préparation construit le contexte commun de l’exécution ;
4. un nœud `Execute Command` lance [`scripts/collect-results.sh`](../scripts/collect-results.sh) dans le conteneur `n8n` ;
5. un nœud `Code` analyse le JSON renvoyé par le script ;
6. un second nœud `Code` génère le sujet, le texte brut et le HTML de la synthèse ;
7. un nœud `Email Send` envoie le récapitulatif.

## Prérequis

- la stack Docker du dépôt doit être démarrée ;
- un compte propriétaire doit avoir été créé dans `n8n` ;
- le nœud `Execute Command` doit rester autorisé dans l’instance locale ;
- un credential SMTP `Send Email` doit être créé manuellement dans `n8n` ;
- les adresses email placeholder du nœud `Préparer le contexte` doivent être remplacées avant activation du workflow.

Points volontairement manuels à ce stade :

- association du credential SMTP au nœud d’envoi ;
- choix des vraies adresses `from` et `to` ;
- éventuel ajustement de l’horaire du `Cron` ;
- branchement futur de la collecte réelle sur GitHub et Renovate.

## Importer le workflow dans n8n

### Depuis l’interface

1. démarrer la stack avec `docker compose up -d` ;
2. ouvrir `n8n` via l’URL locale configurée ;
3. créer un nouveau workflow puis utiliser l’option d’import JSON ;
4. sélectionner [`workflows/repo-ops-daily-maintenance.json`](./workflows/repo-ops-daily-maintenance.json) ;
5. ouvrir le nœud `Envoyer le récapitulatif` et lui associer un credential SMTP ;
6. ouvrir le nœud `Préparer le contexte` et remplacer les adresses placeholder ;
7. lancer un test via `Déclenchement manuel`.

### Depuis la CLI n8n dans le conteneur

```powershell
docker compose exec n8n n8n import:workflow --input=/files/workflows/repo-ops-daily-maintenance.json
```

Après import, la configuration SMTP reste à faire dans l’interface `n8n`.
