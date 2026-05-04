import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { AuthSessionService } from '../services/auth-session.service';

export const authGuard: CanActivateFn = () => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  const toast = inject(ToastService);

  if (session.isLoggedIn()) return true;
  toast.warning('Please login to access your profile.');
  return router.parseUrl('/login');
};

