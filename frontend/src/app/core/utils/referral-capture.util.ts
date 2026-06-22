import { firstValueFrom } from 'rxjs';
import { AuthService, type MeRes } from '../services/auth.service';
import { ReferralService, referrerDisplayName } from '../services/referral.service';
import { getRegistrationAdvisorCode } from './registration-advisor.util';

export const REFERRAL_INVALID_MSG =
  'Referral code is incorrect. Please check and enter the correct code.';

export const REFERRAL_INACTIVE_MSG =
  'This advisor code is inactive. Payment will proceed with default assignment.';

export const REFERRAL_DEBOUNCE_MS = 500;

export type ReferralValidationState = 'idle' | 'validating' | 'valid' | 'invalid' | 'inactive';

export type ReferralCheckoutAction =
  | { kind: 'skip_modal'; code: string; displayName?: string | null }
  | { kind: 'confirm'; code: string; displayName: string }
  | { kind: 'prompt' };

export async function validateReferralCode(
  referrals: ReferralService,
  code: string,
): Promise<{ valid: boolean; inactive: boolean; displayName: string | null; normalizedCode: string }> {
  const trimmed = code.trim();
  if (!trimmed) {
    return { valid: false, inactive: false, displayName: null, normalizedCode: '' };
  }

  try {
    const res = await firstValueFrom(referrals.validate({ referralCode: trimmed }));
    const name = referrerDisplayName(res);
    if (res?.success && name) {
      return { valid: true, inactive: false, displayName: name, normalizedCode: trimmed };
    }
    if (res?.inactive) {
      return { valid: false, inactive: true, displayName: null, normalizedCode: trimmed };
    }
    return { valid: false, inactive: false, displayName: null, normalizedCode: trimmed };
  } catch {
    return { valid: false, inactive: false, displayName: null, normalizedCode: trimmed };
  }
}

export function canProceedWithReferralState(state: ReferralValidationState): boolean {
  return state === 'valid' || state === 'inactive';
}

/** Decide whether checkout should skip, confirm, or prompt for advisor code. */
export async function resolveReferralCheckoutAction(
  auth: AuthService,
  referrals: ReferralService,
): Promise<ReferralCheckoutAction> {
  let me: MeRes | null = null;
  try {
    me = await firstValueFrom(auth.me());
  } catch {
    // Fall back to session-stashed code only.
  }

  if (me?.registrationAdvisorLocked && me.registrationAdvisorCode?.trim()) {
    return {
      kind: 'skip_modal',
      code: me.registrationAdvisorCode.trim(),
      displayName: me.registrationAdvisorDisplayName ?? null,
    };
  }

  const stashed = getRegistrationAdvisorCode();
  if (stashed) {
    const validated = await validateReferralCode(referrals, stashed);
    if (validated.valid && validated.displayName) {
      return {
        kind: 'confirm',
        code: validated.normalizedCode,
        displayName: validated.displayName,
      };
    }
  }

  if (me?.registrationAdvisorCode?.trim()) {
    const validated = await validateReferralCode(referrals, me.registrationAdvisorCode);
    if (validated.valid && validated.displayName) {
      return {
        kind: 'confirm',
        code: validated.normalizedCode,
        displayName: validated.displayName,
      };
    }
  }

  return { kind: 'prompt' };
}

export function applyReferralConfirmPrefill(
  code: string,
  displayName: string,
  targets: {
    setCode: (value: string) => void;
    setName: (value: string | null) => void;
    setState: (value: ReferralValidationState) => void;
  },
): void {
  targets.setCode(code);
  targets.setName(displayName);
  targets.setState('valid');
}
