import type { IdentifierChannel } from '../validators/identifier.validators';

export const FORGOT_PASSWORD_ACCOUNT_NOT_FOUND_REASON = 'account_not_found';

export function forgotPasswordAccountNotFoundMessage(channel: IdentifierChannel | null): string {
  if (channel === 'email') {
    return (
      'We could not find a member or partner account registered with this email address. ' +
      'Please check for spelling mistakes, try another email you may have used when signing up, ' +
      'or create a new account if you have not registered yet.'
    );
  }
  if (channel === 'sms') {
    return (
      'We could not find a member or partner account registered with this mobile number. ' +
      'Please enter the same 10-digit mobile number you used during registration, ' +
      'or create a new account if you have not signed up yet.'
    );
  }
  return (
    'We could not find a member or partner account with the details you entered. ' +
    'Please double-check your email or mobile number, or create a new account.'
  );
}

export function forgotPasswordAccountNotFoundHints(channel: IdentifierChannel | null): string[] {
  if (channel === 'email') {
    return [
      'Use the email address you registered with on MSME Bharat Manch.',
      'Check your inbox for past welcome or OTP emails from us.',
      'If you are new here, create an account to continue.',
    ];
  }
  if (channel === 'sms') {
    return [
      'Enter your 10-digit Indian mobile number without country code.',
      'Use the same number you verified during registration.',
      'If you have not signed up yet, create a new account.',
    ];
  }
  return [
    'Enter the email or mobile number from your registration.',
    'Members and partners can reset passwords from this page.',
    'New users can sign up using Create Account below.',
  ];
}

export function isForgotPasswordAccountNotFound(
  res: { success?: boolean; reason?: string; message?: string } | null | undefined
): boolean {
  if (!res || res.success !== false) return false;
  if (res.reason === FORGOT_PASSWORD_ACCOUNT_NOT_FOUND_REASON) return true;
  const msg = (res.message ?? '').toLowerCase();
  return msg.includes('could not find') || msg.includes("couldn't find") || msg.includes('not registered');
}

export function isAccountNotFoundErrorMessage(message: string | null | undefined): boolean {
  const msg = (message ?? '').toLowerCase();
  return msg.includes('could not find') || msg.includes("couldn't find") || msg.includes('not registered');
}
