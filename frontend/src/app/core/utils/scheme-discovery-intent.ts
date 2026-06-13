export const SCHEME_DISCOVERY_INTENT_KEY = 'mbm_return_intent';
export const SCHEME_DISCOVERY_INTENT_VALUE = 'scheme-discovery';

export function setSchemeDiscoveryIntent(): void {
  try {
    window.localStorage.setItem(SCHEME_DISCOVERY_INTENT_KEY, SCHEME_DISCOVERY_INTENT_VALUE);
  } catch {
    // ignore
  }
}

export function consumeSchemeDiscoveryIntent(): boolean {
  try {
    const v = window.localStorage.getItem(SCHEME_DISCOVERY_INTENT_KEY);
    if (v !== SCHEME_DISCOVERY_INTENT_VALUE) return false;
    window.localStorage.removeItem(SCHEME_DISCOVERY_INTENT_KEY);
    return true;
  } catch {
    return false;
  }
}

export function hasSchemeDiscoveryIntent(): boolean {
  try {
    return window.localStorage.getItem(SCHEME_DISCOVERY_INTENT_KEY) === SCHEME_DISCOVERY_INTENT_VALUE;
  } catch {
    return false;
  }
}
