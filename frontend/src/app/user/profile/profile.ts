import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthService, type MeRes } from '../../core/services/auth.service';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { PaymentService, type ActivePlan } from '../../core/services/payment.service';
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
  private readonly payments = inject(PaymentService);
  private readonly toast = inject(ToastService);

  readonly userId = this.session.userId;
  /** Signals so updates run change detection in zoneless mode (this app has no zone.js). */
  readonly profile = signal<MeRes | null>(null);
  readonly loading = signal(false);
  readonly activePlan = signal<ActivePlan | null>(null);
  readonly planLoading = signal(false);
  private lastLoadedUserId: string | null = null;
  /** Ignores stale HTTP completions if userId/effect re-runs before the request finishes. */
  private loadGen = 0;

  constructor() {
    effect(() => {
      const id = this.userId();
      if (!id) return;
      if (this.lastLoadedUserId === id) return;
      void this.loadProfile(id);
      void this.loadActivePlan(id);
    });
  }

  private async loadProfile(id: string) {
    const gen = ++this.loadGen;
    try {
      this.loading.set(true);
      const data = await firstValueFrom(this.api.me(id).pipe(timeout(15000)));
      if (gen !== this.loadGen) return;
      this.profile.set(data);
      this.lastLoadedUserId = id;
    } catch (e: any) {
      if (gen !== this.loadGen) return;
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Failed to load profile.';
      this.toast.error(msg);
    } finally {
      if (gen === this.loadGen) this.loading.set(false);
    }
  }

  private async loadActivePlan(id: string) {
    try {
      this.planLoading.set(true);
      const res = await firstValueFrom(this.payments.myPlan(id).pipe(timeout(15000)));
      this.activePlan.set(res?.plan ?? null);
    } catch {
      // Silent: profile is still useful without plan info.
      this.activePlan.set(null);
    } finally {
      this.planLoading.set(false);
    }
  }

  logout() {
    this.session.logout();
    this.router.navigateByUrl('/home');
  }
}

