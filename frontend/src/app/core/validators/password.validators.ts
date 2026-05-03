import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function passwordComplexityValidator(): ValidatorFn {
  // "more than 8" => enforce >= 9
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').toString();
    if (!value) return null;

    const errors: Record<string, boolean> = {};
    if (value.length < 9) errors['minLength9'] = true;
    if (!/[a-z]/.test(value)) errors['lowercase'] = true;
    if (!/[A-Z]/.test(value)) errors['uppercase'] = true;
    if (!/[0-9]/.test(value)) errors['number'] = true;
    return Object.keys(errors).length ? errors : null;
  };
}

