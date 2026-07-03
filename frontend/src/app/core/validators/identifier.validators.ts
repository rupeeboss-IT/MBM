import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/i;
const INDIAN_MOBILE_PATTERN = /^[6-9]\d{9}$/;

export type IdentifierChannel = 'email' | 'sms';

export function normalizePhoneDigits(value: string): string {
  let d = (value ?? '').replace(/\D/g, '');
  if (d.length === 12 && d.startsWith('91')) d = d.slice(2);
  if (d.length === 11 && d.startsWith('0')) d = d.slice(1);
  return d;
}

export function detectIdentifierChannel(value: string): IdentifierChannel | null {
  const trimmed = (value ?? '').trim();
  if (!trimmed) return null;
  return trimmed.includes('@') ? 'email' : 'sms';
}

export function loginIdentifierValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim();
    if (!value) return null;

    if (value.includes('@')) {
      return EMAIL_PATTERN.test(value) ? null : { emailFormat: true };
    }

    const digits = normalizePhoneDigits(value);
    if (!/^\d+$/.test(digits.replace(/\D/g, '')) && !/^\d+$/.test(value.replace(/\D/g, ''))) {
      return { phoneDigits: true };
    }
    return INDIAN_MOBILE_PATTERN.test(digits) ? null : { phoneIndianMobile: true };
  };
}

export function identifierValidationMessage(errors: ValidationErrors | null): string | null {
  if (!errors) return null;
  if (errors['required']) return 'Please enter your email address or mobile number.';
  if (errors['emailFormat']) return 'Please enter a valid email address.';
  if (errors['phoneDigits'] || errors['phoneIndianMobile']) return 'Please enter a valid mobile number.';
  return null;
}
