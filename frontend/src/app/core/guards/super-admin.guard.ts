import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { AdminSessionService } from '../services/admin-session.service';
import { SessionExpiryService } from '../services/session-expiry.service';

export const superAdminGuard: CanActivateFn = () => {
  const session = inject(AdminSessionService);
  const sessionExpiry = inject(SessionExpiryService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const status = sessionExpiry.ensureAdminSession();
  if (status !== 'valid') {
    return router.parseUrl('/admin-login');
  }

  if (session.isSuperAdmin()) return true;
  toast.warning('Only Super Admin can access this page.');
  return router.parseUrl('/admin-dashboard');
};
