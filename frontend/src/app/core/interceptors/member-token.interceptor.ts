import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthSessionService } from '../services/auth-session.service';

function apiPath(url: string): string {
  if (url.includes('://')) {
    try {
      return new URL(url).pathname;
    } catch {
      return url;
    }
  }
  return url.split('?')[0] ?? url;
}

function isMemberApiUrl(url: string): boolean {
  const path = apiPath(url);
  return (
    path.startsWith('/api/payment') ||
    path.startsWith('/api/user/me') ||
    path.startsWith('/api/customer/reports')
  );
}

/** Attach member JWT for payment and profile APIs. */
export const memberTokenInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isMemberApiUrl(req.url)) return next(req);

  const session = inject(AuthSessionService);
  session.refreshFromStorage();
  const token = session.token();
  if (!token) return next(req);

  if (req.headers.has('Authorization')) return next(req);

  return next(
    req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    }),
  );
};
