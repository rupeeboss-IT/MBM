import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { MembershipCheckoutModals } from './core/components/membership-checkout-modals/membership-checkout-modals';
import { SchemeDiscoveryModals } from './core/components/scheme-discovery-modals/scheme-discovery-modals';
import { MsmeSaathiChat } from './core/components/msme-saathi-chat/msme-saathi-chat';
import { ToastContainer } from './core/components/toast-container/toast-container';
import { captureContactLeadSourceFromUrl } from './core/utils/contact-lead-source.util';
import { captureRegistrationAdvisorFromUrl } from './core/utils/registration-advisor.util';
import { captureRegistrationLeadSourceFromUrl } from './core/utils/registration-lead-source.util';
import { captureRegistrationModeFromUrl } from './core/utils/registration-mode.util';
import { Header } from "./header/header";
import { Footer } from "./footer/footer";
import { CookieConsentBanner } from './core/components/cookie-consent-banner/cookie-consent-banner';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
  imports: [Header, Footer, RouterOutlet, ToastContainer, SchemeDiscoveryModals, MembershipCheckoutModals, MsmeSaathiChat, CookieConsentBanner]
})
export class App implements OnInit {
  constructor(private readonly router: Router) {}

  ngOnInit(): void {
    if (typeof window === 'undefined') return;
    this.captureLeadSourcesFromUrl(window.location.search);
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        const query = e.urlAfterRedirects.includes('?')
          ? e.urlAfterRedirects.slice(e.urlAfterRedirects.indexOf('?'))
          : '';
        this.captureLeadSourcesFromUrl(query);
      });
  }

  private captureLeadSourcesFromUrl(search: string): void {
    captureRegistrationLeadSourceFromUrl(search);
    captureRegistrationAdvisorFromUrl(search);
    captureContactLeadSourceFromUrl(search);
    captureRegistrationModeFromUrl(search);
  }
}
