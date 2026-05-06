import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AdminSessionService } from '../services/admin-session.service';

export const adminTokenInterceptor: HttpInterceptorFn = (req, next) => {
  // Only attach token for /api/admin/* calls.
  if (!req.url.startsWith('/api/admin')) return next(req);

  const session = inject(AdminSessionService);
  const token = session.token();
  if (!token) return next(req);

  return next(
    req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    })
  );
};

