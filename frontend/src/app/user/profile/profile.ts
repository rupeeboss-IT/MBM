import { CommonModule } from '@angular/common';
import { Component, effect, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthService, type MeRes } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile {
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly api = inject(AuthService);
  private readonly toast = inject(ToastService);

  readonly userId = this.session.userId;
  profile: MeRes | null = null;
  loading = false;
  private lastLoadedUserId: string | null = null;

  constructor() {
    // React to userId changes (e.g. after login/register) and load once.
    effect(() => {
      const id = this.userId();
      if (!id) return;
      if (this.lastLoadedUserId === id) return;
      void this.loadProfile(id);
    });
  }

  private async loadProfile(id: string) {
    try {
      this.loading = true;
      this.profile = await firstValueFrom(this.api.me(id).pipe(timeout(15000)));
      this.lastLoadedUserId = id;
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Failed to load profile.';
      this.toast.error(msg);
    } finally {
      this.loading = false;
    }
  }

  logout() {
    this.session.logout();
    this.router.navigateByUrl('/home');
  }
}

