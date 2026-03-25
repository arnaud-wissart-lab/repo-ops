# Plan de déploiement

## Phase 1 - Socle .NET et exécution locale

### Objectif

Poser un socle principal `.NET + Docker Compose` directement exploitable localement, sans perdre les briques déjà utiles.

### Livrables

- solution [`RepoOps.sln`](../RepoOps.sln) ;
- worker `.NET` exécutable ;
- AppHost Aspire de pilotage local ;
- stack `docker compose` intégrant `worker`, `postgres`, `n8n` et `renovate` ;
- documentation claire sur le partage des responsabilités entre Compose, Aspire, worker et `n8n`.

### Risques

- confusion entre le runtime réel et la couche de pilotage locale ;
- duplication de responsabilités entre scripts et services `.NET` ;
- dérive vers une stack trop complexe trop tôt.

### Critères de validation

- la solution s’ouvre correctement dans Visual Studio ;
- le worker `.NET` compile et s’exécute ;
- `docker compose` démarre la stack locale ;
- `Aspire` permet de visualiser localement le worker, `postgres` et `n8n`.

## Phase 2 - Reporting et orchestration métier

### Objectif

Déplacer progressivement la logique utile vers la couche `.NET`, tout en conservant `n8n` pour les déclenchements simples et les notifications.

### Livrables

- collecte GitHub structurée dans le worker ;
- qualification opérationnelle des PR Renovate dans le worker ;
- supervision d’une exécution explicite de Renovate avec artefact de reporting dédié ;
- consolidation de résultats multi-repo ;
- génération de synthèse plus complète ;
- workflows `n8n` mieux alignés avec les sorties du worker ;
- réduction progressive du rôle des scripts de transition.

### Risques

- contrat de données instable entre `n8n` et le worker ;
- données hétérogènes selon les dépôts ;
- faux sentiment de complétude alors que certaines sources restent placeholders.

### Critères de validation

- un cycle quotidien complet peut être observé localement ;
- la synthèse s’appuie majoritairement sur la couche `.NET` ;
- le worker sait interroger GitHub sur un premier périmètre réel sans casser le contrat de sortie ;
- le worker sait intégrer le dernier résultat connu d’une exécution explicite de Renovate sans déplacer cette logique dans `n8n` ;
- les scripts restants ont un rôle limité, explicite et documenté.

## Phase 3 - Superviseur IA

### Objectif

Ajouter une couche de supervision capable de piloter des tâches de delivery multi-repo sans remettre en cause le socle d’exécution existant.

### Livrables

- conception documentée du superviseur IA ;
- séparation explicite des rôles `planner`, `implementer`, `reviewer`, `QA` et `reporter` ;
- gabarits de tâches standardisés ;
- critères d’arrêt, de validation et de synthèse ;
- premières conventions d’intégration avec les dépôts réels pilotés.

### Risques

- surcouche trop complexe trop tôt ;
- automatisation insuffisamment bornée ;
- confusion entre dépôt d’orchestration et dépôts applicatifs pilotés.

### Critères de validation

- l’extension future reste compatible avec les `AGENTS.md` des dépôts cibles ;
- la séparation des rôles et des critères d’arrêt est explicite ;
- aucune automatisation non maîtrisée n’est introduite avant l’implémentation réelle.
