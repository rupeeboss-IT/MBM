import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CreditRebuildService } from '../../core/services/credit-rebuild.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import { getRegistrationAdvisorCode } from '../../core/utils/registration-advisor.util';

type AudienceTab = 'individual' | 'business';

interface PricingPlan {
  tier: string;
  price: string;
  featured?: boolean;
  features: string[];
}

@Component({
  selector: 'app-credit-rebuild',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './credit-rebuild.html',
  styleUrl: './credit-rebuild.css',
})
export class CreditRebuild {
  private readonly creditRebuildApi = inject(CreditRebuildService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);

  readonly audience = signal<AudienceTab>('individual');
  readonly submitting = signal(false);
  readonly submitAttempted = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly touched = signal<Record<string, boolean>>({});

  private readonly defaultSuccessMessage =
    'Thank you! We have received your enquiry. Our credit rebuild team will contact you shortly.';

  form = {
    fullName: '',
    mobile: '',
    email: '',
    consent: false,
  };

  private readonly bannedNameWords = [
    'test', 'dummy', 'fake', 'unknown', 'n/a', 'na', 'null', 'none', 'abcd', 'asdf',
  ];

  readonly errors = signal<Record<string, string>>({});
  readonly isFormValid = computed(() => Object.keys(this.errors()).length === 0);

  readonly individualPlans: PricingPlan[] = [
    {
      tier: 'Essential',
      price: '₹9,999',
      features: [
        '1 Bureau (CIBIL or any one)',
        'Analyze and fix issues in one report',
        'When only one bureau has negative data',
      ],
    },
    {
      tier: 'Advanced',
      price: '₹12,999',
      featured: true,
      features: [
        'Everything in Essential',
        '2 Bureaus (CIBIL + Experian or CRIF)',
        'Correct data across both reports and match details',
        'When banks check multiple bureaus for loan approval',
      ],
    },
    {
      tier: 'Premium',
      price: '₹14,999',
      features: [
        'Everything in Advanced',
        'All 4 Bureaus (CIBIL, Experian, CRIF, Equifax)',
        'For complex credit issues',
        'When lenders check multiple bureaus',
      ],
    },
  ];

  readonly businessPlans: PricingPlan[] = [
    {
      tier: 'Essential',
      price: '₹34,999',
      features: [
        '1 Bureau (CIBIL or any one)',
        'Analyze and fix commercial report issues',
        'When only one bureau has negative data',
      ],
    },
    {
      tier: 'Advanced',
      price: '₹40,999',
      featured: true,
      features: [
        '2 Bureaus (CIBIL + Experian or CRIF)',
        'Correct and match data across both reports',
        'When banks check multiple bureaus for MSME loans',
      ],
    },
    {
      tier: 'Premium',
      price: '₹59,999',
      features: [
        'All 4 Bureaus (CIBIL, Experian, CRIF, Equifax)',
        'For large MSMEs or complex commercial credit issues',
        'Full bureau alignment for loan readiness',
      ],
    },
  ];

  readonly activePlans = computed(() =>
    this.audience() === 'individual' ? this.individualPlans : this.businessPlans,
  );

  constructor() {
    this.route.queryParamMap.subscribe((params) => {
      const audience = params.get('audience');
      if (audience === 'individual' || audience === 'business') {
        this.audience.set(audience);
      }

      const section = params.get('section');
      if (section) {
        window.setTimeout(() => this.scrollToSection(section), 200);
      }
    });
  }

  setAudience(tab: AudienceTab): void {
    this.audience.set(tab);
  }

  scrollToSection(section: string): void {
    const normalized = section.trim().toLowerCase();
    const id =
      normalized === 'plans'
        ? 'credit-rebuild-pricing'
        : normalized === 'pro'
          ? 'credit-rebuild-pro'
          : normalized === 'enquiry'
            ? 'credit-rebuild-enquiry'
            : null;

    if (!id) return;

    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  scrollToEnquiry(): void {
    this.scrollToSection('enquiry');
  }

  showError(field: string): boolean {
    return (this.submitAttempted() || this.touched()[field]) && !!this.errors()[field];
  }

  markTouched(field: string): void {
    this.touched.update(t => ({ ...t, [field]: true }));
    this.revalidate();
  }

  onNameInput(value: string): void {
    this.form.fullName = value.replace(/[^a-zA-Z\s]/g, '');
    this.revalidate();
  }

  onDigitsInput(field: 'mobile', event: Event): void {
    const el = event.target as HTMLInputElement;
    const digits = el.value.replace(/\D/g, '').slice(0, 10);
    this.form[field] = digits;
    el.value = digits;
    this.revalidate();
  }

  revalidate(): void {
    const next: Record<string, string> = {};

    const name = this.form.fullName.trim();
    if (!name) next['fullName'] = 'Your name is required.';
    else if (name.length < 2) next['fullName'] = 'Please enter your full name.';
    else if (/[0-9]/.test(name)) next['fullName'] = 'Name cannot contain numbers.';
    else {
      const words = name.toLowerCase().split(/\s+/).filter(Boolean);
      if (words.some(w => this.bannedNameWords.includes(w))) next['fullName'] = 'Please enter a valid name.';
    }

    const mobile = this.form.mobile.trim();
    if (!mobile) next['mobile'] = 'Mobile number is required.';
    else if (!/^[6-9]\d{9}$/.test(mobile)) next['mobile'] = 'Enter a valid 10-digit Indian mobile number.';

    const email = this.form.email.trim();
    if (!email) next['email'] = 'Email address is required.';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email)) next['email'] = 'Enter a valid email address.';

    if (!this.form.consent) next['consent'] = 'Consent is required to submit this enquiry.';

    this.errors.set(next);
  }

  async onSubmit(): Promise<void> {
    if (this.submitting()) return;
    this.submitAttempted.set(true);
    this.submitError.set(null);
    this.revalidate();
    if (!this.isFormValid()) return;

    this.submitting.set(true);
    try {
      const advisorCode = getRegistrationAdvisorCode();
      const res = await firstValueFrom(
        this.creditRebuildApi.submitEnquiry({
          fullName: this.form.fullName.trim(),
          mobile: this.form.mobile.trim(),
          email: this.form.email.trim(),
          consentAccepted: this.form.consent === true,
          advisorCode,
        }),
      );

      if (!res?.success) {
        const msg = res?.message || API_USER_MESSAGES.creditRebuildEnquiry;
        this.submitError.set(msg);
        this.toast.error(msg);
        return;
      }

      this.toast.success(res.message || this.defaultSuccessMessage);
      this.resetForm();
      this.revalidate();
    } catch (e: unknown) {
      const msg = getHttpErrorMessage(e, API_USER_MESSAGES.creditRebuildEnquiry);
      this.submitError.set(msg);
      this.toast.error(msg);
    } finally {
      this.submitting.set(false);
    }
  }

  private resetForm(): void {
    this.form.fullName = '';
    this.form.mobile = '';
    this.form.email = '';
    this.form.consent = false;
    this.submitAttempted.set(false);
    this.touched.set({});
  }
}
