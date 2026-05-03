import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function indianMobileValidator(): ValidatorFn {
  const re = /^[6-9]\d{9}$/;
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString().trim();
    if (!value) return null;
    if (!/^\d+$/.test(value)) return { phoneDigits: true };
    if (!re.test(value)) return { phoneIndianMobile: true };
    return null;
  };
}

