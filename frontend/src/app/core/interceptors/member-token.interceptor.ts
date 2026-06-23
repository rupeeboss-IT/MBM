import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { getApiPath } from '../utils/api-url';
import { AuthSessionService } from '../services/auth-session.service';

function isMemberApiUrl(url: string): boolean {
  const path = getApiPath(url);
  return (
    path.startsWith('/api/payment') ||
    path.startsWith('/api/user/me') ||
    path.startsWith('/api/customer/reports') ||
    path.startsWith('/api/scheme-discovery') ||
    path.startsWith('/api/connect')
  );
}

/** Attach member JWT for payment, profile, scheme-discovery, and connect APIs. */
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
