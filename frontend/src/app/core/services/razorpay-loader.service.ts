import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

declare global {
  interface Window {
    Razorpay?: any;
  }
}

const SCRIPT_URL = 'https://checkout.razorpay.com/v1/checkout.js';

@Injectable({ providedIn: 'root' })
export class RazorpayLoaderService {
  private readonly doc = inject(DOCUMENT);
  private loadPromise: Promise<void> | null = null;

  load(): Promise<void> {
    if (typeof window === 'undefined') {
      return Promise.reject(new Error('Razorpay can only be loaded in the browser.'));
    }
    if (window.Razorpay) return Promise.resolve();
    if (this.loadPromise) return this.loadPromise;

    this.loadPromise = new Promise<void>((resolve, reject) => {
      const existing = this.doc.querySelector<HTMLScriptElement>(`script[src="${SCRIPT_URL}"]`);
      if (existing) {
        existing.addEventListener('load', () => resolve());
        existing.addEventListener('error', () => reject(new Error('Failed to load Razorpay.')));
        if (window.Razorpay) resolve();
        return;
      }
      const s = this.doc.createElement('script');
      s.src = SCRIPT_URL;
      s.async = true;
      s.onload = () => resolve();
      s.onerror = () => {
        this.loadPromise = null;
        reject(new Error('Failed to load Razorpay.'));
      };
      this.doc.body.appendChild(s);
    });

    return this.loadPromise;
  }
}
