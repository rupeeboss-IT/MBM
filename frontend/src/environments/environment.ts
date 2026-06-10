/** Default local environment (overridden by angular.json fileReplacements per build config). */
export const environment = {
  production: false,
  name: 'local' as const,
  apiBaseUrl: '/api',
  frontendBaseUrl: 'http://localhost:4200',
};
