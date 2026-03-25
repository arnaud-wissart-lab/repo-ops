# Plan de déploiement

## Phase 1 - Maintenance multi-repo

### Objectif

Mettre en place une boucle de maintenance fiable sur une allowlist courte de dépôts publics personnels.

### Livrables

- stack `docker compose` opérationnelle ;
- configuration `Renovate` self-hosted d’administration ;
- premières exécutions sur une liste restreinte de dépôts ;
- documentation de démarrage et d’exploitation minimale.

### Risques

- jeton GitHub sous-dimensionné ;
- allowlist trop large dès le départ ;
- bruit excessif de pull requests si les limites de concurrence sont mal calibrées.

### Critères de validation

- la stack démarre localement ;
- `Renovate` cible uniquement les dépôts explicitement listés ;
- au moins un dépôt de test génère un comportement attendu en mode prudent.

## Phase 2 - Reporting et gouvernance

### Objectif

Structurer le reporting quotidien et rendre le suivi plus lisible pour la prise de décision.

### Livrables

- workflows `n8n` de planification et de collecte ;
- scripts de consolidation des résultats ;
- synthèse email HTML et texte brut ;
- premiers indicateurs de volumétrie, d’échec et de dette restante.

### Risques

- données hétérogènes selon les dépôts ;
- faux positifs ou informations incomplètes dans la synthèse ;
- dépendance trop forte à une seule source d’information.

### Critères de validation

- un résumé quotidien est généré sans intervention manuelle ;
- les sections clés du reporting sont alimentées de manière cohérente ;
- les actions manuelles recommandées sont identifiables rapidement.

## Phase 3 - Superviseur IA

### Objectif

Ajouter une couche de supervision IA capable de piloter des tâches de delivery multi-repo sans remettre en cause le socle d’exécution existant.

### Livrables

- conception documentée du superviseur IA ;
- séparation explicite des rôles `planner`, `implementer`, `reviewer`, `QA` et `reporter` ;
- gabarits de tâches standardisés ;
- critères d’arrêt, de validation et de synthèse ;
- première structure cible d’organisation interne pour les futures tâches et rapports.

### Risques

- surcouche trop complexe trop tôt ;
- décisions peu explicables ;
- dépendance fonctionnelle excessive à des signaux imparfaits ;
- confusion entre dépôt d’orchestration et dépôts applicatifs pilotés.

### Critères de validation

- l’architecture cible du superviseur est documentée et actionnable ;
- la séparation des rôles et des critères d’arrêt est explicite ;
- le superviseur reste borné par les instructions locales des dépôts cibles ;
- aucune automatisation non maîtrisée n’est introduite avant la phase d’implémentation réelle.
