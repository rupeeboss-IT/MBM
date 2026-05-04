import { Injectable, computed, signal } from '@angular/core';

const KEY = 'mbm_userId';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly _userId = signal<string | null>(localStorage.getItem(KEY));
  readonly userId = this._userId.asReadonly();
  readonly isLoggedIn = computed(() => !!this._userId());

  /** Call after register/login. */
  setUserId(userId: string) {
    const v = (userId ?? '').trim();
    if (!v) return;
    localStorage.setItem(KEY, v);
    this._userId.set(v);
  }

  logout() {
    localStorage.removeItem(KEY);
    this._userId.set(null);
  }
}

