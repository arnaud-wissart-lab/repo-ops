# Conception du futur superviseur IA

## Objectifs

Le futur superviseur IA a pour but de piloter des tâches de développement sur plusieurs dépôts sans transformer `repo-ops` en dépôt applicatif.

Objectifs cibles :

- centraliser la planification de tâches techniques multi-repo ;
- respecter les instructions locales de chaque dépôt, en particulier `AGENTS.md` ;
- garder un mode d’exécution incrémental, borné et vérifiable ;
- produire un reporting lisible pour revue humaine ;
- préparer des changements candidats à une pull request, sans industrialiser trop tôt l’autonomie complète.

## Responsabilités

Responsabilités attendues du superviseur :

- charger le contexte d’un dépôt cible ;
- identifier les consignes locales, conventions et validations attendues ;
- transformer un objectif en tâches élémentaires ;
- attribuer chaque tâche à un rôle d’exécution adapté ;
- vérifier que les validations minimales ont été exécutées ;
- construire une synthèse de sortie exploitable pour revue, arbitrage ou PR.

Responsabilités explicitement hors périmètre immédiat :

- déployer en production ;
- fusionner automatiquement les pull requests ;
- contourner les règles de validation d’un dépôt ;
- manipuler des secrets hors des variables ou mécanismes explicitement prévus.

## Limites de sécurité

Le superviseur devra respecter des limites de sécurité simples et strictes :

- ne jamais ignorer ou réécrire les instructions de dépôt sans justification explicite ;
- ne jamais stocker de secret dans les fichiers versionnés ;
- ne jamais pousser vers un dépôt tiers sans politique d’exécution clairement définie ;
- ne jamais considérer qu’un test non lancé est implictement réussi ;
- ne jamais supprimer ou réécrire des changements existants sans comprendre leur origine ;
- ne jamais présenter comme validé un lot qui ne l’a pas été réellement.

## Cycle cible

Cycle cible de bout en bout :

`plan -> implémentation -> validation -> synthèse -> PR`

Description de chaque étape :

### Plan

- analyse du dépôt cible ;
- lecture des consignes locales ;
- formulation d’hypothèses explicites ;
- découpage en tâches courtes et auditables.

### Implémentation

- exécution d’une tâche à la fois ;
- changements ciblés et relisibles ;
- conservation d’un périmètre strictement nécessaire.

### Validation

- exécution des commandes de build, tests, lint ou vérifications pertinentes ;
- remontée explicite des validations non exécutées ;
- arrêt du flux si l’état obtenu n’est pas exploitable.

### Synthèse

- résumé des fichiers touchés ;
- rappel des décisions techniques importantes ;
- mention des risques résiduels et des points manuels.

### PR

- préparation d’un lot prêt pour revue ;
- message de commit et description de PR conformes aux conventions ;
- aucune fusion automatique par défaut.

## Séparation des rôles

Le modèle cible sépare plusieurs rôles, même si tous ne seront pas implémentés immédiatement.

### Planner

- comprend la demande ;
- lit les consignes du dépôt ;
- découpe le travail ;
- définit les hypothèses, le périmètre et les critères d’arrêt.

### Implementer

- réalise les modifications de code ou de configuration ;
- garde un diff petit et cohérent ;
- documente les écarts rencontrés.

### Reviewer

- relit le diff avec une logique de risque ;
- signale régressions, oublis, incohérences et manque de tests ;
- vérifie l’alignement avec `AGENTS.md` et les conventions locales.

### QA

- exécute ou confirme les validations locales et automatiques ;
- vérifie la plausibilité fonctionnelle ;
- distingue clairement ce qui est testé de ce qui ne l’est pas.

### Reporter

- produit la synthèse finale ;
- prépare le contenu de PR ;
- consolide les logs, résultats et suites à donner.

## Critères d’arrêt

Le superviseur doit savoir s’arrêter proprement. Les critères d’arrêt cibles sont les suivants :

- la tâche demandée est terminée et validée au niveau attendu ;
- une information indispensable manque et ne peut pas être déduite proprement ;
- une règle de sécurité ou de dépôt empêcherait de continuer sans risque ;
- les validations échouent et exigent un arbitrage humain ;
- le périmètre glisse au-delà de la tâche initiale.

## Structure de dossiers future proposée

Cette structure n’est pas à créer maintenant. Elle sert de cible d’organisation future.

```text
repo-ops/
  docs/
    ai-supervisor-design.md
  templates/
    agent-task-template.md
  supervisor/
    planners/
    tasks/
    reports/
    policies/
```

Rôle pressenti de chaque dossier :

- `supervisor/planners/` : règles de découpage et modèles de planification ;
- `supervisor/tasks/` : tâches prêtes à exécuter ou archivées ;
- `supervisor/reports/` : synthèses consolidées par dépôt ou par exécution ;
- `supervisor/policies/` : règles de sécurité, critères d’arrêt et politiques d’exécution.
