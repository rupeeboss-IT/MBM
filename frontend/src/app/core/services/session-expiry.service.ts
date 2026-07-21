import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { NavigationStart, Router } from '@angular/router';
import { filter } from 'rxjs';
import { AdminSessionService } from './admin-session.service';
import { AuthSessionService } from './auth-session.service';
import { ToastService } from './toast.service';
import { getJwtExpiryMs, isJwtExpired } from '../utils/token-expiry.util';

export const SESSION_EXPIRED_MESSAGE = 'Your session has expired. Please sign in again.';
export const SESSION_INACTIVITY_MESSAGE = 'Signed out due to inactivity. Please sign in again.';

export type SessionScope = 'member' | 'admin';
export type SessionValidity = 'valid' | 'expired' | 'missing';

@Injectable({ providedIn: 'root' })
export class SessionExpiryService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);
  private readonly memberSession = inject(AuthSessionService);
  private readonly adminSession = inject(AdminSessionService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  private handling = false;
  private started = false;
  private memberTimer: ReturnType<typeof setTimeout> | undefined;
  private adminTimer: ReturnType<typeof setTimeout> | undefined;

  /**
   * Must be called once from the root app (browser only).
   * Starts expiry timers, navigation checks, and visibility checks.
   */
  startWatching(): void {
    if (this.started || !isPlatformBrowser(this.platformId)) return;
    this.started = true;

    this.memberSession.refreshFromStorage();
    this.adminSession.refreshFromStorage();
    this.scheduleMemberExpiry(this.memberSession.token());
    this.scheduleAdminExpiry(this.adminSession.token());
    this.checkSessions(SESSION_INACTIVITY_MESSAGE);

    this.router.events
      .pipe(filter((e): e is NavigationStart => e instanceof NavigationStart))
      .subscribe(() => this.checkSessions(SESSION_INACTIVITY_MESSAGE));

    this.document.addEventListener('visibilitychange', () => {
      if (this.document.visibilityState === 'visible') {
        this.checkSessions(SESSION_INACTIVITY_MESSAGE);
      }
    });

    this.document.defaultView?.addEventListener('focus', () => {
      this.checkSessions(SESSION_INACTIVITY_MESSAGE);
    });
  }

  /** Centralized logout for expired or invalid sessions. */
  handleExpiredSession(scope: SessionScope, message: string = SESSION_EXPIRED_MESSAGE): void {
    if (!isPlatformBrowser(this.platformId)) return;
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

  /**
   * Live expiry check — does not trust the memoized isLoggedIn computed,
   * which only re-runs when token/userId signals change (not when time passes).
   */
  ensureMemberSession(): SessionValidity {
    this.memberSession.refreshFromStorage();
    const token = this.memberSession.token();
    const userId = this.memberSession.userId();

    if (!userId || !token) return 'missing';
    if (isJwtExpired(token)) {
      this.handleExpiredSession('member', SESSION_INACTIVITY_MESSAGE);
      return 'expired';
    }
    return 'valid';
  }

  ensureAdminSession(): SessionValidity {
    this.adminSession.refreshFromStorage();
    const token = this.adminSession.token();
    const userId = this.adminSession.userId();

    if (!userId || !token) return 'missing';
    if (isJwtExpired(token)) {
      this.handleExpiredSession('admin', SESSION_INACTIVITY_MESSAGE);
      return 'expired';
    }
    return 'valid';
  }

  /** Re-check stored tokens now (navigation, tab focus, app start). */
  checkSessions(message: string = SESSION_EXPIRED_MESSAGE): void {
    if (!isPlatformBrowser(this.platformId) || this.handling) return;

    this.memberSession.refreshFromStorage();
    this.adminSession.refreshFromStorage();

    const memberToken = this.memberSession.token();
    if (memberToken && this.memberSession.userId() && isJwtExpired(memberToken)) {
      this.handleExpiredSession('member', message);
      return;
    }

    const adminToken = this.adminSession.token();
    if (adminToken && this.adminSession.userId() && isJwtExpired(adminToken)) {
      this.handleExpiredSession('admin', message);
    }
  }

  scheduleMemberExpiry(token: string | null | undefined): void {
    this.clearMemberSchedule();
    if (!isPlatformBrowser(this.platformId)) return;

    const expMs = getJwtExpiryMs(token);
    if (expMs == null) return;

    // Logout at real expiry (no early skew on the timer — skew is for validation only).
    const delay = Math.max(0, expMs - Date.now());
    this.memberTimer = setTimeout(() => {
      if (this.memberSession.token() && isJwtExpired(this.memberSession.token())) {
        this.handleExpiredSession('member', SESSION_INACTIVITY_MESSAGE);
      }
    }, delay);
  }

  scheduleAdminExpiry(token: string | null | undefined): void {
    this.clearAdminSchedule();
    if (!isPlatformBrowser(this.platformId)) return;

    const expMs = getJwtExpiryMs(token);
    if (expMs == null) return;

    const delay = Math.max(0, expMs - Date.now());
    this.adminTimer = setTimeout(() => {
      if (this.adminSession.token() && isJwtExpired(this.adminSession.token())) {
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
