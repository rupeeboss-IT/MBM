import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { AdminSessionService } from '../services/admin-session.service';

export const superAdminGuard: CanActivateFn = () => {
  const session = inject(AdminSessionService);
  const router = inject(Router);
  const toast = inject(ToastService);

  if (session.isLoggedIn() && session.isSuperAdmin()) return true;
  toast.warning('Only Super Admin can access this page.');
  return router.parseUrl('/admin-dashboard');
};
