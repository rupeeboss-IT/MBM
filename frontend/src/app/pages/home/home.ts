import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { JoinTodayButton } from '../../core/components/join-today-button/join-today-button';
import { ImageLightbox } from '../../core/components/image-lightbox/image-lightbox';
import { ContactService } from '../../core/services/contact.service';
import { SchemeDiscoveryFlowService } from '../../core/services/scheme-discovery-flow.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { FREE_REGISTER_QUERY_PARAMS } from '../../core/utils/registration-mode.util';

@Component({
  selector: 'app-home',
  imports: [CommonModule, FormsModule, RouterLink, JoinTodayButton, ImageLightbox],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly router = inject(Router);
  private readonly contactApi = inject(ContactService);
  private readonly toast = inject(ToastService);
  readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);

  readonly lightboxOpen = signal(false);
  readonly lightboxSrc = signal<string | null>(null);
  readonly lightboxAlt = signal('');

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

  ngOnInit(): void {
    void this.schemeDiscovery.tryResumeOnPageLoad();
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
      const res = await firstValueFrom(
        this.contactApi.submitCallback({
          fullName: this.callbackForm.fullName.trim(),
          mobile,
          subjectId: 3,
          message: messageParts.join('\n'),
          consentAccepted: this.callbackForm.consent === true,
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
