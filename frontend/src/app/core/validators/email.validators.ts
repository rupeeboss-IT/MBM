import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const DISPOSABLE_DOMAINS = [
  'mailinator.com',
  'tempmail.com',
  '10minutemail.com',
  'guerrillamail.com',
  'yopmail.com',
  'dispostable.com'
];

const BLOCKED_EXAMPLE_DOMAINS = ['example.com', 'example.org', 'example.net'];

export function strictEmailValidator(): ValidatorFn {
  // Basic but strict-ish email format + block common fake/disposable domains.
  const basic = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim().toLowerCase();
    if (!value) return null;
    if (!basic.test(value)) return { emailFormat: true };

    const [, domain] = value.split('@');
    if (!domain) return { emailFormat: true };
    if (BLOCKED_EXAMPLE_DOMAINS.includes(domain)) return { emailFake: true };
    if (DISPOSABLE_DOMAINS.includes(domain)) return { emailDisposable: true };

    // Block obvious test addresses.
    if (value.startsWith('test@') || value.startsWith('fake@')) return { emailFake: true };
    return null;
  };
}

