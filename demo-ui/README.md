# Démo web repo-ops

Cette interface fournit une démonstration visuelle du pipeline `repo-ops` dans un format lisible, professionnel et sûr. Elle s’appuie désormais sur une base visuelle inspirée de Metronic, avec un thème clair par défaut, un sélecteur de thème et une hiérarchie de cartes plus proche d’un produit de supervision technique que d’un simple dashboard custom.

## Ce que montre la page

- déclenchement d’un run HTTP du worker ;
- visualisation du pipeline `GitHub -> Analyse -> Décision -> Prompts -> Codex -> Validation -> Résultat` ;
- affichage du résumé exécutif ;
- affichage d’un bloc narratif “Ce que le système a fait” ;
- affichage d’un contexte de run plus visible : scénario, durée, dernière exécution ;
- affichage d’une timeline verticale du run ;
- affichage des décisions du superviseur ;
- affichage des prompts générés ;
- déclenchement d’un déploiement local explicite via le worker ;
- affichage d’un panneau développeur avec logs et JSON brut ;
- chargement d’un scénario mock réaliste si le backend n’est pas disponible ;
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
- [`src/mocks/demoData.ts`](./src/mocks/demoData.ts) : scénario mock réaliste ;
- [`src/components`](./src/components) : composants de présentation ;
- [`src/components/ui`](./src/components/ui) : primitives visuelles de base inspirées de Metronic ;
- [`src/lib/utils.ts`](./src/lib/utils.ts) : helper de fusion de classes Tailwind ;
- [`src/styles.css`](./src/styles.css) : fondations visuelles Tailwind/Metronic, tokens et thème global.

## Prérequis

- `Node.js` 20 ou plus récent ;
- le worker `.NET` démarré localement sur `http://127.0.0.1:8080` si vous voulez tester le mode API ;
- un environnement `repo-ops` configuré si vous souhaitez une collecte GitHub utile.

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

1. Cliquer sur `Charger un exemple` pour afficher immédiatement un run complet et comprendre le rôle de l’application.
2. Relire le bloc `Ce que le système a fait`, puis le pipeline et les décisions.
3. Utiliser ensuite `Lancer une analyse réelle` si le worker local est disponible.
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

## Mode mock

Deux mécanismes sont prévus :

- le bouton `Charger un exemple`, qui force un scénario mock ;
- la variable `VITE_DEMO_MODE`, utile pour démarrer directement la démo dans un mode donné.

Valeurs supportées :

- `api` : la page utilise l’API locale ;
- `mock` : la page charge directement le scénario mock ;
- `auto` : la page tente l’API, puis peut basculer vers le mock si l’appel échoue.

Le mode par défaut retenu pour la démo publique est `auto`, afin d’éviter une page bloquée si l’API locale n’est pas disponible ou mal routée.

Exemple :

```powershell
$env:VITE_DEMO_MODE="mock"
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
- lecture rapide des KPI principaux ;
- pipeline vertical immédiatement compréhensible ;
- panneau développeur valorisant pour la lecture technique ;
- détails consultables sans alourdir la page ;
- responsive sans mécanique complexe.

## Limites

- l’interface reste un client de démonstration local ;
- elle dépend des endpoints déjà présents dans le worker pour le mode API ;
- le mode mock est crédible mais reste statique ;
- elle ne remplace ni `n8n`, ni l’historique complet des runs, ni les flux CLI avancés de validation et d’exécution.
