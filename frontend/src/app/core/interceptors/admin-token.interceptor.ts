import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { isAdminApiRequest } from '../utils/api-url';
import { AdminSessionService } from '../services/admin-session.service';

export const adminTokenInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isAdminApiRequest(req.url)) return next(req);

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
