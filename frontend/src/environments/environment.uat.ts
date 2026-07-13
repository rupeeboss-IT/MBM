export const environment = {
  production: true,
  name: 'uat' as const,
  apiBaseUrl: 'https://uat.rupeeboss.com/api',
  frontendBaseUrl: 'https://uat.rupeeboss.com',
  /** reCAPTCHA v3 site key — replace with your actual key from https://www.google.com/recaptcha/admin */
  recaptchaSiteKey: '6LcAXkwtAAAAAHSe1LppPcRuSvri1tCuVDpVnaGb',
  /** GA4 Measurement ID — empty on UAT to avoid polluting production data */
  gaMeasurementId: '',
};
