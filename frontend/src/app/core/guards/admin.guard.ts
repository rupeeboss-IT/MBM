import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { AdminSessionService } from '../services/admin-session.service';
import { SessionExpiryService } from '../services/session-expiry.service';

export const adminGuard: CanActivateFn = () => {
  const session = inject(AdminSessionService);
  const sessionExpiry = inject(SessionExpiryService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const status = sessionExpiry.ensureAdminSession();
  if (status === 'expired') return false;
  if (status === 'missing') {
    toast.warning('Please login as admin to continue.');
    return router.parseUrl('/admin-login');
  }

  if (session.hasAdminAccess()) return true;
  toast.warning('Please login as admin to continue.');
  return router.parseUrl('/admin-login');
};

