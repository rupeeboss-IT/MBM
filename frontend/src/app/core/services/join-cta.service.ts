import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthSessionService } from './auth-session.service';
import { PaymentService, type ActivePlan } from './payment.service';

/** Guest → register; logged-in without plan → membership; active member → my-plan. */
export type JoinCtaAudience = 'guest' | 'prospect' | 'member';

/**
 * Resolves "Join Today" CTA label and destination from session + /api/payment/my-plan.
 * Root-scoped so home, about, and other CTAs stay in sync after login/logout.
 */
@Injectable({ providedIn: 'root' })
export class JoinCtaService {
  private readonly session = inject(AuthSessionService);
  private readonly payments = inject(PaymentService);

  private readonly activePlan = signal<ActivePlan | null>(null);
  private loadGen = 0;

  readonly loading = signal(false);

  readonly audience = computed<JoinCtaAudience>(() => {
    if (!this.session.isLoggedIn()) return 'guest';
    if (this.activePlan()) return 'member';
    return 'prospect';
  });

  readonly buttonLabel = computed(() => {
    switch (this.audience()) {
      case 'guest':
        return 'Join Today';
      case 'member':
        return 'View Membership';
      default:
        return 'Explore Membership Plans';
    }
  });

  readonly route = computed(() => {
    switch (this.audience()) {
      case 'guest':
        return '/register';
      case 'member':
        return '/my-plan';
      default:
        return '/membership';
    }
  });

  constructor() {
    effect(() => {
      const loggedIn = this.session.isLoggedIn();
      const userId = this.session.userId();
      if (!loggedIn || !userId) {
        this.activePlan.set(null);
        this.loading.set(false);
        return;
      }
      void this.loadActivePlan();
    });
  }

  /** Re-fetch plan (e.g. after successful payment on membership page). */
  async refresh(): Promise<void> {
    if (!this.session.isLoggedIn()) return;
    await this.loadActivePlan();
  }

  private async loadActivePlan(): Promise<void> {
    const gen = ++this.loadGen;
    this.loading.set(true);
    try {
      this.session.refreshFromStorage();
      const res = await firstValueFrom(this.payments.myPlan().pipe(timeout(15000)));
      if (gen !== this.loadGen) return;
      const plan = res?.plan ?? null;
      const isActive =
        !!plan && (plan.status ?? '').trim().toLowerCase() === 'active';
      this.activePlan.set(isActive ? plan : null);
    } catch {
      if (gen === this.loadGen) this.activePlan.set(null);
    } finally {
      if (gen === this.loadGen) this.loading.set(false);
    }
  }
}
