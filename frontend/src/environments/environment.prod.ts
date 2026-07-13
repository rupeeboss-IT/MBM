export const environment = {
  production: true,
  name: 'production' as const,
  /** Relative — API calls use the same host as the page (works for www and non-www). */
  apiBaseUrl: '/api',
  frontendBaseUrl: 'https://msmebharatmanch.com',
  /** reCAPTCHA v3 site key — replace with your actual key from https://www.google.com/recaptcha/admin */
  recaptchaSiteKey: '6LcAXkwtAAAAAHSe1LppPcRuSvri1tCuVDpVnaGb',
  /** GA4 Measurement ID — replace with your actual G-XXXXXXXXXX from analytics.google.com */
  gaMeasurementId: 'G-2N0ZSV8LX6',
};
