# Démo web repo-ops

Cette interface fournit une démonstration visuelle du pipeline `repo-ops` dans un format lisible, professionnel et sûr. Elle s’appuie désormais sur le dépôt GitHub public réel `arnaud-wissart/repoops-demo-weather-station`, afin de montrer une valeur concrète par rapport à une lecture GitHub brute. La base visuelle suit une hiérarchie inspirée de Metronic, avec un thème clair par défaut et un sélecteur de thème intégré.

## Ce que montre la page

- déclenchement d’un run HTTP du worker sur un dépôt GitHub réel ;
- visualisation du pipeline `GitHub -> Analyse -> Décision -> Prompts -> Codex -> Validation -> Résultat` ;
- affichage du résumé exécutif ;
- affichage d’un bloc narratif “Ce que le système a fait” ;
- affichage d’un contexte de run plus visible : scénario, durée, dernière exécution ;
- affichage d’une timeline verticale du run ;
- affichage des décisions du superviseur ;
- affichage des prompts générés ;
- déclenchement d’un déploiement local explicite via le worker ;
- affichage d’un panneau développeur avec logs et JSON brut ;
- sélecteur de thème `Clair / Sombre / Auto`.

La page n’exécute pas :

- de commit ;
- de push ;
- de création de pull request ;
- de merge réel ;
- de validation humaine.

Le bouton de déploiement local vise la machine sur laquelle le worker tourne. Dans le cas de `repo-ops`, il vise ce dépôt sur la machine personnelle et expose le contrôle public de `https://repoops.arnaudwissart.fr`. Dans le mode par défaut, il reste en `dry-run` tant que la configuration backend n’est pas ouverte au réel.
Ce bouton reste toutefois un outil de démonstration locale. Le flux principal de déploiement du dépôt passe désormais par le workflow GitHub Actions `Déploiement Manuel`.

En environnement déployé, la démo est maintenant servie par un conteneur Nginx sur `0.0.0.0:8084`, avec un proxy local vers le worker pour :

- `/maintenance/*`
- `/supervisor/*`
- `/deployment/*`

## Structure frontend

- [`src/App.tsx`](./src/App.tsx) : orchestration du scénario et états principaux ;
- [`src/api.ts`](./src/api.ts) : appels HTTP et gestion du timeout ;
- [`src/components`](./src/components) : composants de présentation ;
- [`src/components/ui`](./src/components/ui) : primitives visuelles de base inspirées de Metronic ;
- [`src/lib/utils.ts`](./src/lib/utils.ts) : helper de fusion de classes Tailwind ;
- [`src/styles.css`](./src/styles.css) : fondations visuelles Tailwind/Metronic, tokens et thème global.

## Prérequis

- `Node.js` 20 ou plus récent ;
- le worker `.NET` démarré localement sur `http://127.0.0.1:8080` ;
- un environnement `repo-ops` configuré avec `GITHUB_TOKEN` et `RENOVATE_REPOSITORIES` pointant vers `arnaud-wissart/repoops-demo-weather-station`.

## Lancement

1. Démarrer le worker :

```powershell
dotnet run --project .\src\RepoOps.Worker
```

2. Installer les dépendances :

```powershell
cd .\demo-ui
npm install
```

3. Lancer Vite :

```powershell
npm run dev
```

4. Ouvrir l’URL indiquée par Vite, généralement :

```text
http://127.0.0.1:5173
```

## Parcours conseillé

1. Ouvrir le dépôt GitHub public de démonstration depuis la zone d’actions.
2. Lancer `Analyser le dépôt de démonstration`.
3. Relire le bloc `Ce que le système a fait`, puis le pipeline, les décisions et les prompts.
4. Consulter enfin `Sortie technique (mode développeur)` pour les logs et le JSON.

## Configuration de l’URL API

En développement, Vite relaie automatiquement :

- `/maintenance/*`
- `/deployment/*`
- `/supervisor/*`

vers `http://127.0.0.1:8080`.

Pour viser une autre URL du worker :

```powershell
$env:VITE_DEMO_PROXY_TARGET="http://127.0.0.1:8081"
npm run dev
```

Timeout API configurable :

```powershell
$env:VITE_DEMO_API_TIMEOUT_MS="45000"
npm run dev
```

## Build de vérification

```powershell
npm run build
```

## Intentions UX/UI

- base visuelle inspirée de Metronic, plus sobre et plus standardisée ;
- hiérarchie visuelle forte avec un hero clair ;
- badge de démonstration visible en permanence ;
- sélecteur de thème `Clair / Sombre / Auto` pour éviter d’imposer un rendu sombre ;
- lecture narrative immédiate après le run ;
- contexte de scénario et de durée visible sans ouvrir les détails ;
- dépôt GitHub analysé visible dans les cartes clés ;
- lecture rapide des KPI principaux ;
- pipeline vertical immédiatement compréhensible ;
- panneau développeur valorisant pour la lecture technique ;
- détails consultables sans alourdir la page ;
- responsive sans mécanique complexe.

## Limites

- l’interface reste un client de démonstration local ;
- elle dépend des endpoints déjà présents dans le worker pour le mode API ;
- elle suppose que le worker puisse joindre GitHub pour produire un run réellement parlant ;
- elle ne remplace ni `n8n`, ni l’historique complet des runs, ni les flux CLI avancés de validation et d’exécution.
