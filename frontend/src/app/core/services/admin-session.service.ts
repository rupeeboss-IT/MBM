import { Injectable, computed, signal } from '@angular/core';

const KEY_USER_ID = 'mbm_admin_userId';
const KEY_TOKEN = 'mbm_admin_token';
const KEY_ROLE = 'mbm_admin_role';

@Injectable({ providedIn: 'root' })
export class AdminSessionService {
  private readonly _userId = signal<string | null>(localStorage.getItem(KEY_USER_ID));
  private readonly _token = signal<string | null>(localStorage.getItem(KEY_TOKEN));
  private readonly _role = signal<string | null>(localStorage.getItem(KEY_ROLE));

  readonly userId = this._userId.asReadonly();
  readonly token = this._token.asReadonly();
  readonly role = this._role.asReadonly();

  readonly isLoggedIn = computed(() => !!this._userId() && !!this._token());
  readonly isAdmin = computed(() => (this._role() ?? '').toLowerCase() === 'admin');
  readonly isSuperAdmin = computed(() => (this._role() ?? '').toLowerCase() === 'superadmin');
  readonly hasAdminAccess = computed(() => this.isAdmin() || this.isSuperAdmin());

  setSession(userId: string, token: string, role?: string) {
    const uid = (userId ?? '').trim();
    const tok = (token ?? '').trim();
    if (!uid || !tok) return;

    localStorage.setItem(KEY_USER_ID, uid);
    localStorage.setItem(KEY_TOKEN, tok);
    localStorage.setItem(KEY_ROLE, (role ?? 'admin').trim());

    this._userId.set(uid);
    this._token.set(tok);
    this._role.set((role ?? 'admin').trim());
  }

  logout() {
    localStorage.removeItem(KEY_USER_ID);
    localStorage.removeItem(KEY_TOKEN);
    localStorage.removeItem(KEY_ROLE);
    this._userId.set(null);
    this._token.set(null);
    this._role.set(null);
  }
}

