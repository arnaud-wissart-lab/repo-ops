const parseRepositories = (value) =>
  (value ?? "")
    .split(",")
    .map((repository) => repository.trim())
    .filter(Boolean);

const repositories = parseRepositories(process.env.RENOVATE_REPOSITORIES);

if (repositories.length === 0) {
  console.warn(
    "[repo-ops] Aucune valeur fournie dans RENOVATE_REPOSITORIES ; aucun dépôt ne sera traité."
  );
}

module.exports = {
  platform: "github",
  endpoint: "https://api.github.com/",
  onboarding: false,
  requireConfig: "ignored",
  autodiscover: false,
  repositories,
  timezone: "Europe/Paris",
  labels: ["dependencies", "renovate"],
  assignees: [],
  automerge: false,
  platformAutomerge: false,
  dependencyDashboard: true,
  enabledManagers: ["nuget", "npm", "github-actions", "dockerfile"],
  prConcurrentLimit: 5,
  branchConcurrentLimit: 5,
  prHourlyLimit: 2,
  schedule: [
    "after 1am and before 5am every weekday",
    "every weekend"
  ],
  logLevel: process.env.LOG_LEVEL || "info",
  logFormat: "json",
  packageRules: [
    {
      description: "Cadre commun pour les mises à jour applicatives détectées.",
      matchManagers: ["nuget", "npm", "github-actions", "dockerfile"],
      labels: ["dependencies", "renovate"]
    }
  ]
};
