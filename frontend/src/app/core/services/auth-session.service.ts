import { Injectable, computed, signal } from '@angular/core';

const KEY_USER = 'mbm_userId';
const KEY_TOKEN = 'mbm_token';
const KEY_ROLE = 'mbm_role';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly _userId = signal<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem(KEY_USER) : null
  );
  private readonly _token = signal<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem(KEY_TOKEN) : null
  );
  private readonly _role = signal<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem(KEY_ROLE) : null
  );

  readonly userId = this._userId.asReadonly();
  readonly token = this._token.asReadonly();
  readonly role = this._role.asReadonly();
  readonly isLoggedIn = computed(() => !!this._userId() && !!this._token());

  /** Re-read localStorage (e.g. before payment API calls after navigation). */
  refreshFromStorage() {
    if (typeof localStorage === 'undefined') return;
    this._userId.set(localStorage.getItem(KEY_USER));
    this._token.set(localStorage.getItem(KEY_TOKEN));
    this._role.set(localStorage.getItem(KEY_ROLE));
  }

  setSession(userId: string, token: string, role?: string) {
    const id = (userId ?? '').trim();
    const t = (token ?? '').trim();
    if (!id || !t) return;
    localStorage.setItem(KEY_USER, id);
    localStorage.setItem(KEY_TOKEN, t);
    if (role) localStorage.setItem(KEY_ROLE, role.trim());
    this._userId.set(id);
    this._token.set(t);
    this._role.set(role?.trim() ?? null);
  }

  /** @deprecated Use setSession */
  setUserId(userId: string) {
    const id = (userId ?? '').trim();
    if (!id) return;
    localStorage.setItem(KEY_USER, id);
    this._userId.set(id);
  }

  logout() {
    localStorage.removeItem(KEY_USER);
    localStorage.removeItem(KEY_TOKEN);
    localStorage.removeItem(KEY_ROLE);
    this._userId.set(null);
    this._token.set(null);
    this._role.set(null);
  }
}
