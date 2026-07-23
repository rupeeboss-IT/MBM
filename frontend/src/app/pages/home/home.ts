import { CommonModule, isPlatformBrowser } from '@angular/common';
import {
  AfterViewInit,
  Component,
  computed,
  inject,
  OnDestroy,
  OnInit,
  PLATFORM_ID,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import { firstValueFrom } from 'rxjs';
import { JoinTodayButton } from '../../core/components/join-today-button/join-today-button';
import { ImageLightbox } from '../../core/components/image-lightbox/image-lightbox';
import { MembershipPlanCards } from '../../core/components/membership-plan-cards/membership-plan-cards';
import { ContactService } from '../../core/services/contact.service';
import { SchemeDiscoveryFlowService } from '../../core/services/scheme-discovery-flow.service';
import { MembershipPlanCheckoutService, type MembershipPlanCode } from '../../core/services/membership-plan-checkout.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { FREE_REGISTER_QUERY_PARAMS } from '../../core/utils/registration-mode.util';
import { RecaptchaService } from '../../core/services/recaptcha.service';
import { EventsService, type PublicEvent } from '../../core/services/events.service';
import { SchemesService, type PublicScheme } from '../../core/services/schemes.service';
import { PlansService, type PublicPlanItem } from '../../core/services/plans.service';

export interface HeroFeature {
  slug: string;
  title: string;
  tagline: string;
  description: string;
  theme: 'trust' | 'schemes' | 'loan' | 'bank' | 'whatsapp' | 'gem';
  iconSvg: string;
}

@Component({
  selector: 'app-home',
  imports: [CommonModule, FormsModule, RouterLink, JoinTodayButton, ImageLightbox, MembershipPlanCards],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit, AfterViewInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly contactApi = inject(ContactService);
  private readonly toast = inject(ToastService);
  private readonly recaptcha = inject(RecaptchaService);
  private readonly eventsApi = inject(EventsService);
  private readonly schemesApi = inject(SchemesService);
  readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);
  private readonly planCheckout = inject(MembershipPlanCheckoutService);
  private readonly plansApi = inject(PlansService);

  readonly lightboxOpen = signal(false);
  readonly lightboxSrc = signal<string | null>(null);
  readonly lightboxAlt = signal('');
  readonly homeEvents = signal<PublicEvent[]>([]);
  readonly homeSchemes = signal<PublicScheme[]>([]);
  readonly cmsPlans = signal<PublicPlanItem[]>([]);
  readonly plansLoading = signal(true);

  readonly callbackModalOpen = signal(false);
  readonly callbackSubmitting = signal(false);
  readonly callbackSubmitAttempted = signal(false);
  readonly callbackError = signal<string | null>(null);
  readonly callbackTouched = signal<Record<string, boolean>>({});

  callbackForm = {
    fullName: '',
    mobile: '',
    consent: false,
  };

  private readonly bannedNameWords = [
    'test', 'dummy', 'fake', 'unknown', 'n/a', 'na', 'null', 'none', 'abcd', 'asdf',
  ];

  readonly callbackErrors = signal<Record<string, string>>({});
  readonly isCallbackValid = computed(() => Object.keys(this.callbackErrors()).length === 0);

  readonly heroEnter = signal(false);
  readonly heroSettled = signal(false);
  /** Active feature for the spotlight panel (defaults to first). */
  readonly activeHeroFeature = signal(0);

  private heroSettleTimer: ReturnType<typeof setTimeout> | null = null;
  private heroAutoTimer: ReturnType<typeof setInterval> | null = null;
  private heroAutoPaused = false;
  private static readonly HERO_AUTO_MS = 3000;

  readonly heroFeatures: HeroFeature[] = [
    {
      slug: 'trust-score',
      title: 'Trust Score',
      tagline: 'Verified credibility',
      description: 'Build credibility with verified trust indicators that boost customer confidence and funding opportunities.',
      theme: 'trust',
      iconSvg: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="M9 12l2 2 4-4"/></svg>`,
    },
    {
      slug: 'scheme-discovery',
      title: 'Govt Schemes',
      tagline: 'Central & State benefits',
      description: 'Discover Central and State MSME schemes with eligibility guidance and subsidy support.',
      theme: 'schemes',
      iconSvg: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 21h18"/><path d="M5 21V7l7-4 7 4v14"/><path d="M9 21v-6h6v6"/><path d="M9 9h.01M15 9h.01M9 13h.01M15 13h.01"/></svg>`,
    },
    {
      slug: 'loan-audit',
      title: 'Loan Audit',
      tagline: 'Optimise borrowing',
      description: 'Review existing business loans and find opportunities to reduce borrowing costs.',
      theme: 'loan',
      iconSvg: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4.35-4.35"/><path d="M11 8v6M8 11h6"/></svg>`,
    },
    {
      slug: 'bank-statement-analyzer',
      title: 'Bank Analyzer',
      tagline: 'Compare financing',
      description: 'Compare banks and financing options to make informed funding decisions with clarity.',
      theme: 'bank',
      iconSvg: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10h18"/><path d="M5 10V20M9 10V20M15 10V20M19 10V20"/><path d="M2 20h20"/><path d="M12 2L2 7h20L12 2z"/></svg>`,
    },
    {
      slug: 'whatsapp-platform',
      title: 'WhatsApp',
      tagline: 'Business messaging',
      description: 'Connect with customers using automated messaging and business communication tools.',
      theme: 'whatsapp',
      iconSvg: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 11.5a8.38 8.38 0 01-.9 3.8 8.5 8.5 0 01-7.6 4.7 8.38 8.38 0 01-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 01-.9-3.8 8.5 8.5 0 014.7-7.6 8.38 8.38 0 013.8-.9h.5a8.48 8.48 0 018 8v.5z"/></svg>`,
    },
    {
      slug: 'gem-registration',
      title: 'GeM Registration',
      tagline: 'Government marketplace',
      description: 'Register on the Government eMarketplace and unlock new business opportunities.',
      theme: 'gem',
      iconSvg: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 7V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v2"/><path d="M12 12v4M10 14h4"/></svg>`,
    },
  ];

  ngOnInit(): void {
    void this.schemeDiscovery.tryResumeOnPageLoad();
    void this.loadHomeEvents();
    void this.loadHomeSchemes();
    void this.loadCmsPlans();
  }

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      this.heroEnter.set(true);
      this.heroSettled.set(true);
      return;
    }

    requestAnimationFrame(() => {
      this.heroEnter.set(true);
      this.heroSettleTimer = setTimeout(() => {
        this.heroSettled.set(true);
        this.startHeroAutoRotate();
      }, 1200);
    });
  }

  ngOnDestroy(): void {
    if (this.heroSettleTimer) clearTimeout(this.heroSettleTimer);
    this.stopHeroAutoRotate();
  }

  selectHeroFeature(index: number): void {
    // User interaction always pauses auto-rotate until pointer leaves the spotlight
    this.pauseHeroAutoRotate();
    this.activeHeroFeature.set(index);
  }

  pauseHeroAutoRotate(): void {
    this.heroAutoPaused = true;
    this.stopHeroAutoRotate();
  }

  resumeHeroAutoRotate(): void {
    this.heroAutoPaused = false;
    this.startHeroAutoRotate();
  }

  onHeroSpotlightFocusOut(event: FocusEvent): void {
    const root = event.currentTarget as HTMLElement | null;
    const next = event.relatedTarget as Node | null;
    if (root && next && root.contains(next)) return;
    this.resumeHeroAutoRotate();
  }

  private startHeroAutoRotate(): void {
    if (!isPlatformBrowser(this.platformId) || this.heroAutoPaused) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    this.stopHeroAutoRotate();
    this.heroAutoTimer = setInterval(() => {
      const next = (this.activeHeroFeature() + 1) % this.heroFeatures.length;
      this.activeHeroFeature.set(next);
    }, Home.HERO_AUTO_MS);
  }

  private stopHeroAutoRotate(): void {
    if (this.heroAutoTimer) {
      clearInterval(this.heroAutoTimer);
      this.heroAutoTimer = null;
    }
  }

  heroIcon(svg: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  private async loadCmsPlans() {
    try {
      const res = await firstValueFrom(this.plansApi.listPublic());
      this.cmsPlans.set(res.plans ?? []);
    } catch {
      this.cmsPlans.set([]);
    } finally {
      this.plansLoading.set(false);
    }
  }

  private async loadHomeEvents() {
    try {
      const featured = await firstValueFrom(
        this.eventsApi.getPublished({ featured: true, page: 1, pageSize: 6 }),
      );
      if (featured.events.length) {
        this.homeEvents.set(featured.events.slice(0, 3));
        return;
      }
      const all = await firstValueFrom(
        this.eventsApi.getPublished({ page: 1, pageSize: 6 }),
      );
      this.homeEvents.set(all.events.slice(0, 3));
    } catch {
      this.homeEvents.set([]);
    }
  }

  private async loadHomeSchemes() {
    try {
      const featured = await firstValueFrom(
        this.schemesApi.getPublished({ featured: true, page: 1, pageSize: 6 }),
      );
      if (featured.schemes.length) {
        this.homeSchemes.set(featured.schemes.slice(0, 6));
        return;
      }
      const all = await firstValueFrom(
        this.schemesApi.getPublished({ page: 1, pageSize: 6 }),
      );
      this.homeSchemes.set(all.schemes.slice(0, 6));
    } catch {
      this.homeSchemes.set([]);
    }
  }

  schemeBadgeClass(cls: string | null | undefined): string {
    const c = (cls || 'badge-green').trim();
    return c.startsWith('badge ') ? c : `badge ${c}`;
  }

  eventMetaLine(ev: PublicEvent): string {
    return [ev.date, ev.location].filter((p) => !!p?.trim()).join(' · ');
  }

  startSchemeDiscovery(): void {
    void this.schemeDiscovery.startFlow();
  }

  goToFreeRegister(): void {
    void this.router.navigate(['/register'], { queryParams: FREE_REGISTER_QUERY_PARAMS });
  }

  goToMembership(): void {
    void this.router.navigateByUrl('/membership');
  }

  choosePlan(code: MembershipPlanCode): void {
    void this.planCheckout.choosePlan(code);
  }

  onPlanChoose(code: string): void {
    this.choosePlan(code as MembershipPlanCode);
  }

  openLightbox(src: string, alt: string, event: MouseEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.lightboxSrc.set(src);
    this.lightboxAlt.set(alt);
    this.lightboxOpen.set(true);
  }

  closeLightbox() {
    this.lightboxOpen.set(false);
  }

  openCallbackModal() {
    this.callbackModalOpen.set(true);
    this.callbackError.set(null);
    try {
      document.body.style.overflow = 'hidden';
    } catch {
      // ignore
    }
  }

  closeCallbackModal() {
    this.callbackModalOpen.set(false);
    try {
      document.body.style.overflow = '';
    } catch {
      // ignore
    }
  }

  markCallbackTouched(field: string) {
    this.callbackTouched.update(v => ({ ...v, [field]: true }));
    this.revalidateCallback();
  }

  showCallbackError(field: string): boolean {
    return !!this.callbackErrors()[field] && (this.callbackSubmitAttempted() || !!this.callbackTouched()[field]);
  }

  onCallbackNameInput(v: string) {
    const cleaned = (v ?? '').replace(/[^a-zA-Z ]+/g, '').replace(/\s{2,}/g, ' ');
    this.callbackForm.fullName = cleaned;
    this.revalidateCallback();
  }

  onCallbackDigitsInput(ev: Event) {
    const el = ev.target as HTMLInputElement | null;
    const cleaned = (el?.value ?? '').replace(/\D+/g, '').slice(0, 10);
    this.callbackForm.mobile = cleaned;
    if (el && el.value !== cleaned) el.value = cleaned;
    this.revalidateCallback();
  }

  revalidateCallback() {
    const next: Record<string, string> = {};

    const nameErr = this.validateName(this.callbackForm.fullName);
    if (nameErr) next['fullName'] = nameErr;

    const mobileErr = this.validateMobile(this.callbackForm.mobile);
    if (mobileErr) next['mobile'] = mobileErr;

    if (!this.callbackForm.consent) next['consent'] = 'Consent is required to request a callback.';

    this.callbackErrors.set(next);
  }

  async submitCallback(): Promise<void> {
    if (this.callbackSubmitting()) return;

    this.callbackSubmitAttempted.set(true);
    this.callbackError.set(null);
    this.revalidateCallback();
    if (!this.isCallbackValid()) return;

    const mobile = this.callbackForm.mobile.trim();
    const messageParts = [
      'Callback requested from Home page — Government Scheme Discovery section.',
    ];

    this.callbackSubmitting.set(true);
    try {
      const recaptchaToken = await this.recaptcha.execute('home_callback');
      const res = await firstValueFrom(
        this.contactApi.submitCallback({
          fullName: this.callbackForm.fullName.trim(),
          mobile,
          subjectId: 3,
          message: messageParts.join('\n'),
          consentAccepted: this.callbackForm.consent === true,
          recaptchaToken,
        }),
      );

      if (!res?.success) {
        const msg = res?.message || API_USER_MESSAGES.contact;
        this.callbackError.set(msg);
        this.toast.error(msg);
        return;
      }

      this.toast.success(res.message || 'Thank you! Our scheme expert will call you within 24 hours.');
      this.resetCallbackForm();
      this.closeCallbackModal();
    } catch (e: unknown) {
      const msg = getHttpErrorMessage(e, API_USER_MESSAGES.contact);
      this.callbackError.set(msg);
      this.toast.error(msg);
    } finally {
      this.callbackSubmitting.set(false);
    }
  }

  private resetCallbackForm(): void {
    this.callbackForm.fullName = '';
    this.callbackForm.mobile = '';
    this.callbackForm.consent = false;
    this.callbackErrors.set({});
    this.callbackTouched.set({});
    this.callbackSubmitAttempted.set(false);
    this.callbackError.set(null);
  }

  private validateName(nameRaw: string): string | null {
    const name = (nameRaw ?? '').trim();
    if (!name) return 'Your name is required.';
    if (name.length < 2) return 'Please enter your full name.';
    if (/[^a-zA-Z ]/.test(name)) return 'Name can contain only letters and spaces.';
    if (/\d/.test(name)) return 'Name cannot contain numbers.';

    const words = name.toLowerCase().split(/\s+/).filter(Boolean);
    if (words.some(w => this.bannedNameWords.includes(w))) return 'Please enter a valid name.';
    return null;
  }

  private validateMobile(v: string): string | null {
    const m = (v ?? '').trim();
    if (!m) return 'Mobile number is required.';
    if (!/^\d+$/.test(m)) return 'Mobile number must be numeric.';
    if (!/^[6-9]\d{9}$/.test(m)) return 'Enter a valid 10-digit Indian mobile number.';
    return null;
  }

}
