import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AdminSessionService } from '../services/admin-session.service';
import { AuthSessionService } from '../services/auth-session.service';
import { SessionExpiryService } from '../services/session-expiry.service';
import { getApiPath, isAdminApiRequest, isApiRequest } from '../utils/api-url';
import { isJwtExpired } from '../utils/token-expiry.util';

function isPublicAuthEndpoint(url: string): boolean {
  const path = getApiPath(url);
  return (
    path === '/api/user/login' ||
    path === '/api/admin/login' ||
    path === '/api/user/register' ||
    path.startsWith('/api/auth/') ||
    path.startsWith('/api/user/password/')
  );
}

function resolveUnauthorizedScope(
  url: string,
  hadAuthHeader: boolean,
  memberSession: AuthSessionService,
  adminSession: AdminSessionService,
): 'member' | 'admin' | null {
  if (isAdminApiRequest(url)) {
    return adminSession.token() || hadAuthHeader ? 'admin' : null;
  }

  // Any authenticated member API failure (Bearer present or member token in storage)
  if (memberSession.token() || hadAuthHeader) return 'member';
  return null;
}

/** Logs out and redirects when an authenticated API returns 401, or when the JWT is already expired. */
export const authSessionInterceptor: HttpInterceptorFn = (req, next) => {
  const memberSession = inject(AuthSessionService);
  const adminSession = inject(AdminSessionService);
  const sessionExpiry = inject(SessionExpiryService);

  if (isApiRequest(req.url) && !isPublicAuthEndpoint(req.url)) {
    memberSession.refreshFromStorage();

    if (isAdminApiRequest(req.url)) {
      const adminToken = adminSession.token();
      if (adminToken && isJwtExpired(adminToken)) {
        sessionExpiry.handleExpiredSession('admin');
        return throwError(
          () =>
            new HttpErrorResponse({
              status: 401,
              statusText: 'Unauthorized',
              url: req.url,
              error: { success: false, message: 'Your session has expired. Please sign in again.' },
            }),
        );
      }
    } else {
      const memberToken = memberSession.token();
      if (memberToken && isJwtExpired(memberToken)) {
        sessionExpiry.handleExpiredSession('member');
        return throwError(
          () =>
            new HttpErrorResponse({
              status: 401,
              statusText: 'Unauthorized',
              url: req.url,
              error: { success: false, message: 'Your session has expired. Please sign in again.' },
            }),
        );
      }
    }
  }

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }

      if (isPublicAuthEndpoint(req.url)) {
        return throwError(() => err);
      }

      memberSession.refreshFromStorage();
      const scope = resolveUnauthorizedScope(
        req.url,
        req.headers.has('Authorization'),
        memberSession,
        adminSession,
      );

      if (scope) {
        sessionExpiry.handleExpiredSession(scope);
      }

      return throwError(() => err);
    }),
  );
};
