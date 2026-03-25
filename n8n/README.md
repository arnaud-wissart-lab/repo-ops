# n8n

Ce dossier accueille la partie orchestration du socle `repo-ops`.

## Contenu

- `workflows/` est réservé aux exports de workflows versionnés lorsque ceux-ci deviendront stables.

## Intention du premier jet

Le dépôt crée uniquement la structure de base nécessaire pour brancher rapidement :

- une planification quotidienne ;
- une collecte des résultats `Renovate` et GitHub ;
- un envoi de synthèse par email.

Les workflows restent volontairement hors périmètre à ce stade afin de garder le premier jet simple et relisible.
