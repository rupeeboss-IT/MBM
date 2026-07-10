import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CookieConsentService } from '../../services/cookie-consent.service';

@Component({
  selector: 'app-cookie-consent-banner',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './cookie-consent-banner.html',
  styleUrl: './cookie-consent-banner.css',
})
export class CookieConsentBanner implements OnInit {
  private readonly consentService = inject(CookieConsentService);

  readonly visible = signal(false);

  ngOnInit(): void {
    if (typeof window === 'undefined') return;
    if (!this.consentService.hasConsented()) {
      this.visible.set(true);
    }
  }

  accept(): void {
    this.visible.set(false);
    this.consentService.accept().subscribe();
  }

  close(): void {
    this.visible.set(false);
  }
}
