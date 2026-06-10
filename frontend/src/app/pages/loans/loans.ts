import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LoansService } from '../../core/services/loans.service';
import { ToastService } from '../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-loans',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './loans.html',
  styleUrl: './loans.css',
})
export class Loans {
  private readonly loansApi = inject(LoansService);
  private readonly toast = inject(ToastService);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly loanTypeOpen = signal(false);

  readonly loanTypeOptions: ReadonlyArray<{ value: string; label: string }> = [
    { value: '1008', label: 'Balance Transfer' },
    { value: '8', label: 'Car Refinance Loan' },
    { value: '1008', label: 'CGTMSE Loan' },
    { value: '1003', label: 'Commercial Property Purchase Loan' },
    { value: '10', label: 'Credit Cards' },
    { value: '1014', label: 'Education Loan' },
    { value: '12', label: 'Home Loan' },
    { value: '7', label: 'Loan Against Property' },
    { value: '1024', label: 'Loan for Professional' },
    { value: '9', label: 'Personal Loan' },
    { value: '13', label: 'Unsecured Business Loan' },
    { value: '11', label: 'Working Capital Loan' },
  ];

  readonly submitting = signal(false);
  readonly submitAttempted = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly touched = signal<Record<string, boolean>>({});

  private readonly defaultSuccessMessage =
    'Application submitted successfully! Our team will contact you within 24 hours.';

  form = {
    fullName: '',
    mobile: '',
    email: '',
    pincode: '',
    loanType: '',
    loanAmount: '',
    consent: false,
  };

  private readonly bannedNameWords = [
    // Keep this list small and business-safe; extend as needed.
    'test',
    'dummy',
    'fake',
    'unknown',
    'n/a',
    'na',
    'null',
    'none',
    'abcd',
    'asdf',
  ];

  readonly errors = signal<Record<string, string>>({});
  readonly isFormValid = computed(() => Object.keys(this.errors()).length === 0);

  loanTypeDisplayLabel(): string {
    const selected = this.loanTypeOptions.find(o => o.value === this.form.loanType);
    return selected?.label ?? 'Select Loan Type';
  }

  toggleLoanTypeOpen(event: Event): void {
    event.stopPropagation();
    this.loanTypeOpen.update(open => !open);
  }

  selectLoanType(value: string): void {
    this.form.loanType = value;
    this.loanTypeOpen.set(false);
    this.markTouched('loanType');
    this.revalidate();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.loanTypeOpen()) return;
    const root = this.host.nativeElement.querySelector('.loan-type-ddl');
    if (root && !root.contains(event.target as Node)) {
      this.loanTypeOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.loanTypeOpen.set(false);
  }

  markTouched(field: string) {
    this.touched.update(v => ({ ...v, [field]: true }));
    this.revalidate();
  }

  showError(field: string): boolean {
    return !!this.errors()[field] && (this.submitAttempted() || !!this.touched()[field]);
  }

  onNameInput(v: string) {
    const cleaned = (v ?? '').replace(/[^a-zA-Z ]+/g, '').replace(/\s{2,}/g, ' ');
    this.form.fullName = cleaned;
    this.revalidate();
  }

  onDigitsInput(
    key: 'mobile' | 'pincode' | 'loanAmount',
    evOrValue: Event | string,
  ) {
    const raw =
      typeof evOrValue === 'string'
        ? evOrValue
        : ((evOrValue.target as HTMLInputElement | null)?.value ?? '');

    let cleaned = (raw ?? '').replace(/\D+/g, '');
    if (key === 'mobile') cleaned = cleaned.slice(0, 10);
    if (key === 'pincode') cleaned = cleaned.slice(0, 6);
    // keep loanAmount unlimited, but numeric-only

    (this.form as { mobile: string; pincode: string; loanAmount: string })[key] = cleaned;

    // Ensure the visible input value matches sanitized value.
    if (typeof evOrValue !== 'string') {
      const el = evOrValue.target as HTMLInputElement | null;
      if (el && el.value !== cleaned) el.value = cleaned;
    }

    this.revalidate();
  }

  revalidate() {
    const next: Record<string, string> = {};

    const nameErr = this.validateName(this.form.fullName);
    if (nameErr) next['fullName'] = nameErr;

    const mobileErr = this.validateMobile(this.form.mobile);
    if (mobileErr) next['mobile'] = mobileErr;

    const emailErr = this.validateEmail(this.form.email);
    if (emailErr) next['email'] = emailErr;

    const pinErr = this.validatePincode(this.form.pincode);
    if (pinErr) next['pincode'] = pinErr;

    if (!this.form.loanType) next['loanType'] = 'Please select a loan type.';

    const amtErr = this.validateLoanAmount(this.form.loanAmount);
    if (amtErr) next['loanAmount'] = amtErr;

    if (!this.form.consent) next['consent'] = 'Consent is required to submit this enquiry.';

    this.errors.set(next);
  }

  private validateName(nameRaw: string): string | null {
    const name = (nameRaw ?? '').trim();
    if (!name) return 'Full name is required.';
    if (name.length < 2) return 'Please enter your full name.';
    if (/[^a-zA-Z ]/.test(name)) return 'Name can contain only letters and spaces.';
    if (/\d/.test(name)) return 'Name cannot contain numbers.';
    if (/(.)\1{3,}/i.test(name.replace(/\s+/g, ''))) return 'Name looks invalid (repeated characters).';

    const words = name
      .toLowerCase()
      .split(/\s+/)
      .filter(Boolean);
    if (words.some(w => this.bannedNameWords.includes(w))) {
      return 'Please enter a valid name.';
    }
    return null;
  }

  private validateMobile(v: string): string | null {
    const m = (v ?? '').trim();
    if (!m) return 'Mobile number is required.';
    if (!/^\d+$/.test(m)) return 'Mobile number must be numeric.';
    if (!/^[6-9]\d{9}$/.test(m)) return 'Enter a valid 10-digit Indian mobile number (starts with 6-9).';
    return null;
  }

  private validateEmail(v: string): string | null {
    const e = (v ?? '').trim();
    if (!e) return null; // optional
    // Practical email validation (not RFC-perfect).
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(e)) return 'Enter a valid email address.';
    return null;
  }

  private validatePincode(v: string): string | null {
    const p = (v ?? '').trim();
    if (!p) return 'Pincode is required.';
    if (!/^\d+$/.test(p)) return 'Pincode must be numeric.';
    if (!/^[1-9]\d{5}$/.test(p)) return 'Enter a valid 6-digit Indian pincode.';
    return null;
  }

  private validateLoanAmount(v: string): string | null {
    const a = (v ?? '').trim();
    if (!a) return 'Loan amount is required.';
    if (!/^\d+$/.test(a)) return 'Loan amount must be numeric.';
    const n = Number(a);
    if (!Number.isFinite(n) || n <= 0) return 'Enter a valid loan amount.';
    return null;
  }

  async onSubmit(): Promise<void> {
    this.submitAttempted.set(true);
    this.submitError.set(null);
    this.revalidate();
    if (!this.isFormValid()) return;

    const loanTypeId = Number(this.form.loanType);
    if (!Number.isFinite(loanTypeId)) {
      this.errors.update(e => ({ ...e, loanType: 'Please select a loan type.' }));
      return;
    }

    this.submitting.set(true);
    try {
      const res = await firstValueFrom(
        this.loansApi.submitApplication({
          fullName: this.form.fullName.trim(),
          mobile: this.form.mobile.trim(),
          email: this.form.email.trim() || null,
          pincode: this.form.pincode.trim(),
          loanTypeId,
          loanAmount: this.form.loanAmount.trim(),
          consentAccepted: this.form.consent === true,
        }),
      );

      if (!res?.success) {
        const msg = res?.message || API_USER_MESSAGES.loanApplication;
        this.submitError.set(msg);
        this.toast.error(msg);
        return;
      }

      this.toast.success(res.message || this.defaultSuccessMessage);
      this.resetForm();
      this.revalidate();
    } catch (e: unknown) {
      const msg = getHttpErrorMessage(e, API_USER_MESSAGES.loanApplication);
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
    this.form.pincode = '';
    this.form.loanType = '';
    this.loanTypeOpen.set(false);
    this.form.loanAmount = '';
    this.form.consent = false;
    this.errors.set({});
    this.touched.set({});
    this.submitAttempted.set(false);
    this.submitError.set(null);
  }
}
