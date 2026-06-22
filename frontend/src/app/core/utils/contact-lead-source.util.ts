export const CONTACT_LEAD_SOURCES = {
  msmeContact: 'MSMECONTACT',
  msmeChatbot: 'MSMECHATBOT',
} as const;

export type ContactLeadSource =
  (typeof CONTACT_LEAD_SOURCES)[keyof typeof CONTACT_LEAD_SOURCES];

const STORAGE_KEY = 'mbm_contact_lead_source';
const QUERY_PARAM = 'source';

function normalizeSource(value: string | null | undefined): ContactLeadSource | null {
  const v = (value ?? '').trim().toUpperCase();
  if (v === CONTACT_LEAD_SOURCES.msmeChatbot) return CONTACT_LEAD_SOURCES.msmeChatbot;
  if (v === CONTACT_LEAD_SOURCES.msmeContact) return CONTACT_LEAD_SOURCES.msmeContact;
  return null;
}

/** Persist chatbot (or other) contact source from URL query param. */
export function captureContactLeadSourceFromUrl(search: string): void {
  if (typeof window === 'undefined') return;
  const params = new URLSearchParams(search.startsWith('?') ? search : `?${search}`);
  const source = normalizeSource(params.get(QUERY_PARAM));
  if (source) sessionStorage.setItem(STORAGE_KEY, source);
}

export function getContactLeadSource(): ContactLeadSource {
  if (typeof window === 'undefined') return CONTACT_LEAD_SOURCES.msmeContact;
  return normalizeSource(sessionStorage.getItem(STORAGE_KEY)) ?? CONTACT_LEAD_SOURCES.msmeContact;
}

export function clearContactLeadSource(): void {
  if (typeof window === 'undefined') return;
  sessionStorage.removeItem(STORAGE_KEY);
}
