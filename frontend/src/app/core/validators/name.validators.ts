import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// Very small starter set; extend as needed.
const BANNED_WORDS = [
  'fuck',
  'shit',
  'bitch',
  'asshole',
  'chutiya',
  'madarchod',
  'bhenchod',
  'gandu',
  'randi'
];

export function strictFullNameValidator(): ValidatorFn {
  // Only letters + spaces, and at least two "name parts" is encouraged but not required.
  // No digits, no special chars.
  const re = /^[A-Za-z ]+$/;
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = (control.value ?? '').toString();
    const value = raw.trim().replace(/\s+/g, ' ');
    if (!value) return null;
    if (!re.test(value)) return { nameFormat: true };
    return null;
  };
}

export function nameNoRepeatsValidator(): ValidatorFn {
  // Reject 3+ repeated characters consecutively, e.g., "Soooop".
  const repeatRe = /(.)\1\1/i;
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim();
    if (!value) return null;
    if (repeatRe.test(value.replace(/\s+/g, ''))) return { nameRepeats: true };
    return null;
  };
}

export function bannedWordsNameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().toLowerCase();
    if (!value) return null;
    const hit = BANNED_WORDS.find((w) => new RegExp(`\\b${escapeRegExp(w)}\\b`, 'i').test(value));
    return hit ? { vulgarWord: true } : null;
  };
}

function escapeRegExp(s: string) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

