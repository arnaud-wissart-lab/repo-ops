# Instructions de travail pour ce dépôt

## Nature du dépôt

- `repo-ops` est un dépôt d’orchestration et de pilotage, pas un dépôt applicatif métier.
- Les changements attendus ici concernent surtout l’automatisation, la documentation, les workflows, la couche métier `.NET`, les scripts d’orchestration transitoires et les modèles de reporting.
- Éviter d’y introduire des conventions ou abstractions propres à un produit applicatif sans justification directe.
- `Docker Compose` est la base d’exécution réelle du socle.
- `Aspire` reste une couche locale de pilotage et de visualisation, pas un orchestrateur de production.
- Le worker `.NET` est la cible principale pour la logique métier future.
- `n8n` conserve un rôle ciblé de planification simple, d’enchaînement et de notification.
- Les scripts shell et PowerShell ne doivent exister qu’en compatibilité transitoire ou en secours clairement documenté.

## Communication

- Répondre en français.
- Rédiger en français toute documentation, commentaire utile, note technique et message explicatif créé dans ce dépôt.

## Principes de modification

- Privilégier une simplicité exploitable immédiatement.
- Respecter une logique incrémentale : petits changements, périmètre clair, validation explicite.
- Ne pas casser [`docker-compose.yml`](./docker-compose.yml) sans raison documentée et vérifiable.
- Ne pas faire dériver `Aspire` vers un rôle d’orchestrateur de production.
- Ne jamais stocker de secret en dur ; utiliser des variables d’environnement documentées dans [`.env.example`](./.env.example).
- Ne pas introduire de dépendance lourde sans justification technique claire.
- Préserver la vocation du dépôt : orchestrer des dépôts tiers sans se substituer à leur logique applicative propre.
- Repositionner explicitement comme transitoire tout script qui n’est plus la cible principale de l’architecture.
- Préférer la production d’un contrat de sortie côté `.NET` plutôt qu’une logique métier reconstruite dans un workflow `n8n`.
- Toute couche d’exécution contrôlée doit rester en `dry-run` par défaut et exiger des garde-fous explicites.
- Ne jamais concevoir une exécution qui pousse directement sur `main` ou `master`.

## Documentation

- Documenter chaque nouveau fichier important créé ou modifié.
- Expliquer les hypothèses retenues lorsqu’une valeur exacte n’est pas connue.
- Maintenir les README et la documentation d’architecture à jour quand le comportement change.
- Toute nouvelle variable d’environnement utilisée doit être déclarée dans [`.env.example`](./.env.example).
- Toute nouvelle variable conservée en réserve doit être explicitement signalée comme non branchée si elle n’est pas consommée par la stack.
- Toute nouvelle couche `.NET` doit être documentée avec son rôle exact dans l’architecture globale.
- Si un workflow `n8n` dépend encore d’un script transitoire, le fallback doit être assumé et documenté.

## Validation

- Après chaque modification significative, proposer des commandes de vérification locales adaptées.
- Vérifier en priorité la cohérence de `docker compose`, la compilation de la solution `.NET`, la lisibilité des scripts et la présence des variables d’environnement attendues.
