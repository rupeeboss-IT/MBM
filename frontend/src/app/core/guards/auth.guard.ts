import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { SessionExpiryService } from '../services/session-expiry.service';

export const authGuard: CanActivateFn = () => {
  const sessionExpiry = inject(SessionExpiryService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const status = sessionExpiry.ensureMemberSession();
  if (status === 'valid') return true;
  if (status === 'expired') return false;

  toast.warning('Please login to access your profile.');
  return router.parseUrl('/login');
};

