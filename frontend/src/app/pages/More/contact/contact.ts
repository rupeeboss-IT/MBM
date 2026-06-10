import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ContactService } from '../../../core/services/contact.service';
import { ToastService } from '../../../core/services/toast.service';
import { API_USER_MESSAGES } from '../../../core/utils/api-user-messages';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './contact.html',
  styleUrl: './contact.css',
})
export class Contact {
  private readonly contactApi = inject(ContactService);
  private readonly toast = inject(ToastService);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly subjectOpen = signal(false);

  readonly subjectOptions: ReadonlyArray<{ value: string; label: string }> = [
    { value: '1', label: 'Payment' },
    { value: '2', label: 'Loan Enquiry' },
    { value: '3', label: 'Government Scheme' },
    { value: '4', label: 'Company Formation' },
    { value: '5', label: 'Technology Services' },
    { value: '6', label: 'Digital Marketing' },
    { value: '7', label: 'Membership' },
    { value: '8', label: 'Partnership' },
    { value: '9', label: 'General Enquiry' },
    { value: '10', label: 'Other' },
  ];

  readonly submitting = signal(false);
  readonly submitAttempted = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly touched = signal<Record<string, boolean>>({});

  form = {
    fullName: '',
    mobile: '',
    email: '',
    subjectId: '',
    message: '',
    consent: false,
  };

  private readonly bannedNameWords = [
    'test', 'dummy', 'fake', 'unknown', 'n/a', 'na', 'null', 'none', 'abcd', 'asdf',
  ];

  readonly errors = signal<Record<string, string>>({});
  readonly isFormValid = computed(() => Object.keys(this.errors()).length === 0);

  subjectDisplayLabel(): string {
    const selected = this.subjectOptions.find(o => o.value === this.form.subjectId);
    return selected?.label ?? 'Select subject';
  }

  toggleSubjectOpen(event: Event): void {
    event.stopPropagation();
    this.subjectOpen.update(open => !open);
  }

  selectSubject(value: string): void {
    this.form.subjectId = value;
    this.subjectOpen.set(false);
    this.markTouched('subjectId');
    this.revalidate();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.subjectOpen()) return;
    const root = this.host.nativeElement.querySelector('.loan-type-ddl');
    if (root && !root.contains(event.target as Node)) {
      this.subjectOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.subjectOpen.set(false);
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

  onDigitsInput(ev: Event) {
    const el = ev.target as HTMLInputElement | null;
    const cleaned = (el?.value ?? '').replace(/\D+/g, '').slice(0, 10);
    this.form.mobile = cleaned;
    if (el && el.value !== cleaned) el.value = cleaned;
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

    if (!this.form.subjectId) next['subjectId'] = 'Please select a subject.';

    const msgErr = this.validateMessage(this.form.message);
    if (msgErr) next['message'] = msgErr;

    if (!this.form.consent) next['consent'] = 'Consent is required to send your message.';

    this.errors.set(next);
  }

  private validateName(nameRaw: string): string | null {
    const name = (nameRaw ?? '').trim();
    if (!name) return 'Your name is required.';
    if (name.length < 2) return 'Please enter your full name.';
    if (/[^a-zA-Z ]/.test(name)) return 'Name can contain only letters and spaces.';
    if (/\d/.test(name)) return 'Name cannot contain numbers.';
    if (/(.)\1{3,}/i.test(name.replace(/\s+/g, ''))) return 'Name looks invalid (repeated characters).';

    const words = name.toLowerCase().split(/\s+/).filter(Boolean);
    if (words.some(w => this.bannedNameWords.includes(w))) return 'Please enter a valid name.';
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
    if (!e) return 'Email address is required.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(e)) return 'Enter a valid email address.';
    return null;
  }

  private validateMessage(v: string): string | null {
    const m = (v ?? '').trim();
    if (!m) return 'Message is required.';
    if (m.length < 10) return 'Please enter at least 10 characters in your message.';
    if (m.length > 4000) return 'Message is too long (maximum 4000 characters).';
    return null;
  }

  async onSubmit(): Promise<void> {
    if (this.submitting()) return;

    this.submitAttempted.set(true);
    this.submitError.set(null);
    this.revalidate();
    if (!this.isFormValid()) return;

    const subjectId = Number(this.form.subjectId);
    if (!Number.isFinite(subjectId)) {
      this.errors.update(e => ({ ...e, subjectId: 'Please select a subject.' }));
      return;
    }

    this.submitting.set(true);
    try {
      const res = await firstValueFrom(
        this.contactApi.submit({
          fullName: this.form.fullName.trim(),
          mobile: this.form.mobile.trim(),
          email: this.form.email.trim(),
          subjectId,
          message: this.form.message.trim(),
          consentAccepted: this.form.consent === true,
        }),
      );

      if (!res?.success) {
        const msg = res?.message || API_USER_MESSAGES.contact;
        this.submitError.set(msg);
        this.toast.error(msg);
        return;
      }

      this.toast.success(res.message || 'Thank you! We have received your query. Our team will connect with you shortly.');
      this.resetForm();
      this.revalidate();
    } catch (e: unknown) {
      const msg = getHttpErrorMessage(e, API_USER_MESSAGES.contact);
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
    this.form.subjectId = '';
    this.subjectOpen.set(false);
    this.form.message = '';
    this.form.consent = false;
    this.errors.set({});
    this.touched.set({});
    this.submitAttempted.set(false);
    this.submitError.set(null);
  }
}
