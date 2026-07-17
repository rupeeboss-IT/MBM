import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AdminSessionService } from '../services/admin-session.service';
import { AuthSessionService } from '../services/auth-session.service';
import { SessionExpiryService } from '../services/session-expiry.service';
import { getApiPath, isAdminApiRequest } from '../utils/api-url';

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

  return memberSession.token() || hadAuthHeader ? 'member' : null;
}

/** Logs out and redirects when an authenticated API returns 401. */
export const authSessionInterceptor: HttpInterceptorFn = (req, next) => {
  const memberSession = inject(AuthSessionService);
  const adminSession = inject(AdminSessionService);
  const sessionExpiry = inject(SessionExpiryService);

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
