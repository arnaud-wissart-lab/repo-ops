# Instructions de travail pour ce dépôt

## Nature du dépôt

- `repo-ops` est un dépôt d’orchestration et de pilotage, pas un dépôt applicatif métier.
- Les changements attendus ici concernent surtout l’automatisation, la documentation, les workflows, les scripts d’orchestration et les modèles de reporting.
- Éviter d’y introduire des conventions ou abstractions propres à un produit applicatif sans justification directe.

## Communication

- Répondre en français.
- Rédiger en français toute documentation, commentaire utile, note technique et message explicatif créé dans ce dépôt.

## Principes de modification

- Privilégier une simplicité exploitable immédiatement.
- Respecter une logique incrémentale : petits changements, périmètre clair, validation explicite.
- Ne pas casser [`docker-compose.yml`](./docker-compose.yml) sans raison documentée et vérifiable.
- Ne jamais stocker de secret en dur ; utiliser des variables d’environnement documentées dans [`.env.example`](./.env.example).
- Ne pas introduire de dépendance lourde sans justification technique claire.
- Préserver la vocation du dépôt : orchestrer des dépôts tiers sans se substituer à leur logique applicative propre.

## Documentation

- Documenter chaque nouveau fichier important créé ou modifié.
- Expliquer les hypothèses retenues lorsqu’une valeur exacte n’est pas connue.
- Maintenir les README et la documentation d’architecture à jour quand le comportement change.
- Toute nouvelle variable d’environnement utilisée doit être déclarée dans [`.env.example`](./.env.example).
- Toute nouvelle variable conservée en réserve doit être explicitement signalée comme non branchée si elle n’est pas consommée par la stack.

## Validation

- Après chaque modification significative, proposer des commandes de vérification locales adaptées.
- Vérifier en priorité la cohérence de `docker compose`, la lisibilité des scripts et la présence des variables d’environnement attendues.
