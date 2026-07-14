import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const KEY_USER_ID = 'mbm_admin_userId';
const KEY_TOKEN = 'mbm_admin_token';
const KEY_ROLE = 'mbm_admin_role';

@Injectable({ providedIn: 'root' })
export class AdminSessionService {
  private readonly platformId = inject(PLATFORM_ID);

  private readonly _userId = signal<string | null>(
    isPlatformBrowser(this.platformId) ? localStorage.getItem(KEY_USER_ID) : null
  );
  private readonly _token = signal<string | null>(
    isPlatformBrowser(this.platformId) ? localStorage.getItem(KEY_TOKEN) : null
  );
  private readonly _role = signal<string | null>(
    isPlatformBrowser(this.platformId) ? localStorage.getItem(KEY_ROLE) : null
  );

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

