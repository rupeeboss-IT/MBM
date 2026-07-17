import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { AdminSessionService } from './admin-session.service';
import { AuthSessionService } from './auth-session.service';
import { ToastService } from './toast.service';
import { getJwtExpiryMs } from '../utils/token-expiry.util';

export const SESSION_EXPIRED_MESSAGE = 'Your session has expired. Please sign in again.';
export const SESSION_INACTIVITY_MESSAGE = 'Signed out due to inactivity. Please sign in again.';

export type SessionScope = 'member' | 'admin';
export type SessionValidity = 'valid' | 'expired' | 'missing';

@Injectable({ providedIn: 'root' })
export class SessionExpiryService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly memberSession = inject(AuthSessionService);
  private readonly adminSession = inject(AdminSessionService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  private handling = false;
  private memberTimer: ReturnType<typeof setTimeout> | undefined;
  private adminTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    if (!isPlatformBrowser(this.platformId)) return;
    this.scheduleMemberExpiry(this.memberSession.token());
    this.scheduleAdminExpiry(this.adminSession.token());
  }

  /** Centralized logout for expired or invalid sessions. */
  handleExpiredSession(scope: SessionScope, message: string = SESSION_EXPIRED_MESSAGE): void {
    if (this.handling) return;
    this.handling = true;

    if (scope === 'admin') {
      this.clearAdminSchedule();
      this.adminSession.logout();
      this.toast.warning(message);
      void this.router.navigateByUrl('/admin-login').finally(() => this.releaseHandlingLock());
      return;
    }

    this.clearMemberSchedule();
    this.memberSession.logout();
    this.toast.warning(message);
    void this.router.navigateByUrl('/login').finally(() => this.releaseHandlingLock());
  }

  ensureMemberSession(): SessionValidity {
    if (this.memberSession.isLoggedIn()) return 'valid';
    if (this.memberSession.token()) {
      this.handleExpiredSession('member', SESSION_INACTIVITY_MESSAGE);
      return 'expired';
    }
    return 'missing';
  }

  ensureAdminSession(): SessionValidity {
    if (this.adminSession.isLoggedIn()) return 'valid';
    if (this.adminSession.token()) {
      this.handleExpiredSession('admin', SESSION_INACTIVITY_MESSAGE);
      return 'expired';
    }
    return 'missing';
  }

  scheduleMemberExpiry(token: string | null | undefined): void {
    this.clearMemberSchedule();
    if (!isPlatformBrowser(this.platformId)) return;

    const expMs = getJwtExpiryMs(token);
    if (expMs == null) return;

    const delay = Math.max(0, expMs - Date.now() - 60_000);
    this.memberTimer = setTimeout(() => {
      if (this.memberSession.token()) {
        this.handleExpiredSession('member', SESSION_INACTIVITY_MESSAGE);
      }
    }, delay);
  }

  scheduleAdminExpiry(token: string | null | undefined): void {
    this.clearAdminSchedule();
    if (!isPlatformBrowser(this.platformId)) return;

    const expMs = getJwtExpiryMs(token);
    if (expMs == null) return;

    const delay = Math.max(0, expMs - Date.now() - 60_000);
    this.adminTimer = setTimeout(() => {
      if (this.adminSession.token()) {
        this.handleExpiredSession('admin', SESSION_INACTIVITY_MESSAGE);
      }
    }, delay);
  }

  clearMemberSchedule(): void {
    if (this.memberTimer) {
      clearTimeout(this.memberTimer);
      this.memberTimer = undefined;
    }
  }

  clearAdminSchedule(): void {
    if (this.adminTimer) {
      clearTimeout(this.adminTimer);
      this.adminTimer = undefined;
    }
  }

  private releaseHandlingLock(): void {
    setTimeout(() => {
      this.handling = false;
    }, 1000);
  }
}
