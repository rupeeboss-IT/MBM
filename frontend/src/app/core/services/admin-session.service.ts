import { Injectable, Injector, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { isJwtExpired } from '../utils/token-expiry.util';
import { SessionExpiryService } from './session-expiry.service';

const KEY_USER_ID = 'mbm_admin_userId';
const KEY_TOKEN = 'mbm_admin_token';
const KEY_ROLE = 'mbm_admin_role';

@Injectable({ providedIn: 'root' })
export class AdminSessionService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly injector = inject(Injector);

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

  /**
   * Note: Angular caches computed() until signal deps change. Time passing alone
   * does not invalidate this — SessionExpiryService clears the token on expiry.
   * Prefer SessionExpiryService.ensureAdminSession() for auth decisions.
   */
  readonly isLoggedIn = computed(() => {
    const token = this._token();
    return !!this._userId() && !!token && !isJwtExpired(token);
  });
  readonly isAdmin = computed(() => (this._role() ?? '').toLowerCase() === 'admin');
  readonly isSuperAdmin = computed(() => (this._role() ?? '').toLowerCase() === 'superadmin');
  readonly hasAdminAccess = computed(() => this.isAdmin() || this.isSuperAdmin());

  /** Live check that always uses the current clock (not a stale computed cache). */
  hasValidSession(): boolean {
    const token = this._token();
    return !!this._userId() && !!token && !isJwtExpired(token);
  }

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
    this.injector.get(SessionExpiryService).scheduleAdminExpiry(tok);
  }

  logout() {
    this.injector.get(SessionExpiryService).clearAdminSchedule();
    localStorage.removeItem(KEY_USER_ID);
    localStorage.removeItem(KEY_TOKEN);
    localStorage.removeItem(KEY_ROLE);
    this._userId.set(null);
    this._token.set(null);
    this._role.set(null);
  }
}

