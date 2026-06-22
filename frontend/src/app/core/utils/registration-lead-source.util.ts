export const REGISTRATION_LEAD_SOURCES = {
  msmeRegistration: 'MSMEREGISTRATION',
  msmeChatbot: 'MSMECHATBOT',
} as const;

export type RegistrationLeadSource =
  (typeof REGISTRATION_LEAD_SOURCES)[keyof typeof REGISTRATION_LEAD_SOURCES];

const STORAGE_KEY = 'mbm_registration_lead_source';
const QUERY_PARAM = 'source';

function normalizeSource(value: string | null | undefined): RegistrationLeadSource | null {
  const v = (value ?? '').trim().toUpperCase();
  if (v === REGISTRATION_LEAD_SOURCES.msmeChatbot) return REGISTRATION_LEAD_SOURCES.msmeChatbot;
  if (v === REGISTRATION_LEAD_SOURCES.msmeRegistration) return REGISTRATION_LEAD_SOURCES.msmeRegistration;
  return null;
}

/** Persist chatbot (or other) registration source from URL query param. */
export function captureRegistrationLeadSourceFromUrl(search: string): void {
  if (typeof window === 'undefined') return;
  const params = new URLSearchParams(search.startsWith('?') ? search : `?${search}`);
  const source = normalizeSource(params.get(QUERY_PARAM));
  if (source) sessionStorage.setItem(STORAGE_KEY, source);
}

export function getRegistrationLeadSource(): RegistrationLeadSource | null {
  if (typeof window === 'undefined') return null;
  return normalizeSource(sessionStorage.getItem(STORAGE_KEY));
}

export function clearRegistrationLeadSource(): void {
  if (typeof window === 'undefined') return;
  sessionStorage.removeItem(STORAGE_KEY);
}
