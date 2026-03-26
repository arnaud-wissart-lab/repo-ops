# Démo web repo-ops

Cette interface fournit une démonstration locale simple du flux `repo-ops` sans exécuter d’action dangereuse.

## Ce que montre la page

- déclenchement d’un run du worker en mode démonstration ;
- affichage du résumé global ;
- affichage des décisions du superviseur ;
- affichage des prompts générés ;
- rappel explicite des garde-fous de sécurité.

La page n’exécute pas :

- de commit ;
- de push ;
- de création de pull request ;
- de merge réel.

## Prérequis

- `Node.js` 20 ou plus récent ;
- le worker `.NET` démarré localement sur `http://127.0.0.1:8080` ;
- un environnement `repo-ops` déjà configuré si vous voulez une collecte GitHub utile.

## Lancement

1. Démarrer le worker :

```powershell
dotnet run --project .\src\RepoOps.Worker
```

2. Installer les dépendances de la démo :

```powershell
cd .\demo-ui
npm install
```

3. Démarrer l’interface :

```powershell
npm run dev
```

4. Ouvrir l’URL indiquée par Vite, généralement :

```text
http://127.0.0.1:5173
```

## Proxy HTTP

En développement, Vite relaie automatiquement :

- `/maintenance/*`
- `/supervisor/*`

vers `http://127.0.0.1:8080`.

Si vous devez viser une autre URL du worker :

```powershell
$env:VITE_DEMO_PROXY_TARGET="http://127.0.0.1:8081"
npm run dev
```

## Build de vérification

```powershell
npm run build
```

## Limites

- la page est pensée pour une démonstration locale, pas pour une exposition publique ;
- elle dépend des endpoints déjà présents dans le worker ;
- elle ne remplace ni `n8n`, ni le reporting historique, ni les flux CLI de validation avancée.
