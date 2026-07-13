import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    grecaptcha: {
      ready(cb: () => void): void;
      execute(siteKey: string, opts: { action: string }): Promise<string>;
    };
  }
}

/**
 * Invisible reCAPTCHA v3 service.
 * Lazily loads the Google script on first use, SSR-safe (no-ops on server).
 */
@Injectable({ providedIn: 'root' })
export class RecaptchaService {
  private readonly platformId = inject(PLATFORM_ID);
  private loadPromise: Promise<void> | null = null;

  /**
   * Execute reCAPTCHA v3 for the given action and return a token.
   * Returns an empty string when running server-side (SSR).
   */
  async execute(action: string): Promise<string> {
    if (!isPlatformBrowser(this.platformId)) return '';
    const siteKey = environment.recaptchaSiteKey;
    if (!siteKey) return '';
    await this.ensureLoaded(siteKey);
    return new Promise<string>((resolve, reject) => {
      window.grecaptcha.ready(() => {
        window.grecaptcha.execute(siteKey, { action }).then(resolve).catch(reject);
      });
    });
  }

  private ensureLoaded(siteKey: string): Promise<void> {
    if (this.loadPromise) return this.loadPromise;
    this.loadPromise = new Promise<void>((resolve, reject) => {
      if (document.querySelector(`script[data-recaptcha]`)) {
        resolve();
        return;
      }
      const script = document.createElement('script');
      script.src = `https://www.google.com/recaptcha/api.js?render=${siteKey}`;
      script.async = true;
      script.defer = true;
      script.dataset['recaptcha'] = 'v3';
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Failed to load reCAPTCHA script'));
      document.head.appendChild(script);
    });
    return this.loadPromise;
  }
}
