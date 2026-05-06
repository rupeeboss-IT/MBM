import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { AdminSessionService } from '../services/admin-session.service';

export const adminGuard: CanActivateFn = () => {
  const session = inject(AdminSessionService);
  const router = inject(Router);
  const toast = inject(ToastService);

  if (session.isLoggedIn() && session.hasAdminAccess()) return true;
  toast.warning('Please login as admin to continue.');
  return router.parseUrl('/admin-login');
};

