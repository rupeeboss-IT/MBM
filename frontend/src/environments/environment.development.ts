export const environment = {
  production: false,
  name: 'local' as const,
  apiBaseUrl: '/api',
  frontendBaseUrl: 'http://localhost:4200',
  /** reCAPTCHA v3 site key — replace with your actual key from https://www.google.com/recaptcha/admin */
  recaptchaSiteKey: '6LcAXkwtAAAAAHSe1LppPcRuSvri1tCuVDpVnaGb',
  /** GA4 Measurement ID — intentionally empty on local to avoid polluting production data */
  gaMeasurementId: '',
};
