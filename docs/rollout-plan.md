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

Ajouter une couche d’aide à la décision sans remettre en cause le socle d’exécution existant.

### Livrables

- règles de priorisation multi-repo ;
- interprétation assistée des échecs de CI ;
- recommandations d’actions ou de relance ;
- journalisation des décisions proposées.

### Risques

- surcouche trop complexe trop tôt ;
- décisions peu explicables ;
- dépendance fonctionnelle excessive à des signaux imparfaits.

### Critères de validation

- les recommandations restent traçables et compréhensibles ;
- le superviseur n’exécute rien directement sur les dépôts ;
- la valeur ajoutée est mesurable sur les dépôts déjà industrialisés.
