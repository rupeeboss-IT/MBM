import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { PaymentService, type ActivePlan } from '../../core/services/payment.service';
import { ToastService } from '../../core/services/toast.service';

type Benefit = { title: string; desc: string };

const PLAN_BENEFITS: Record<string, Benefit[]> = {
  basic: [
    { title: 'Udyam Aadhaar onboarding', desc: 'Join as an MSME member and start your growth journey.' },
    { title: 'Credibility profile', desc: 'Build your MSME credibility and visibility.' },
    { title: 'Network access', desc: 'Connect with the national MSME ecosystem.' },
    { title: 'Knowledge & events', desc: 'Access MSME learning resources and events.' },
    { title: 'WhatsApp platform (basic)', desc: 'Start customer engagement and communication.' },
    { title: 'Basic website', desc: 'Get a simple web presence to represent your business online.' },
    { title: 'Free credit report', desc: 'Basic credit visibility to support financial planning.' },
  ],
  standard: [
    { title: 'Website development', desc: 'Business/portfolio/e-commerce website setup.' },
    { title: 'Hosting + server + DB', desc: 'Includes hosting, server, and database support.' },
    { title: 'Mobile-friendly fast site', desc: 'Responsive + WhatsApp & call buttons.' },
    { title: 'WhatsApp automation', desc: 'FAQ automation and basic routing/notifications.' },
    { title: 'Yearly maintenance', desc: 'Setup + yearly hosting/maintenance.' },
    { title: 'Knowledge & events', desc: 'Access MSME learning resources and events.' },
    { title: 'Free credit report', desc: 'Credit visibility to support growth planning.' },
  ],
  premium: [
    { title: 'Infomerics verified report', desc: 'Verified business verification / trust score report.' },
    { title: 'Scheme discovery report', desc: 'Personalized government scheme discovery.' },
    { title: 'WhatsApp Business platform', desc: 'Advanced WhatsApp platform benefits.' },
    { title: 'Bank statement analyzer', desc: 'Analyzer for financial insights.' },
    { title: 'Website development', desc: 'Website support included.' },
    { title: 'Relationship manager', desc: 'Dedicated support (as per plan offering).' },
    { title: 'Loan / insurance audit', desc: 'Audit support to optimize outcomes.' },
    { title: 'Events access', desc: 'MSME events access included.' },
  ],
  pro: [
    { title: 'Business assessment report', desc: 'Infomerics business assessment report.' },
    { title: 'GeM portal registration', desc: 'Registration & support for GeM portal.' },
    { title: 'Scheme discovery report', desc: 'Personalized government scheme discovery.' },
    { title: 'WhatsApp Business platform', desc: 'Advanced WhatsApp platform benefits.' },
    { title: 'Bank statement analyzer', desc: 'Analyzer for financial insights.' },
    { title: 'Relationship manager', desc: 'Dedicated support (as per plan offering).' },
    { title: 'Loan / insurance audit', desc: 'Audit support to optimize outcomes.' },
    { title: 'Events access', desc: 'More events access included.' },
  ],
};

@Component({
  selector: 'app-my-plan',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="page active plan-page">
      <div class="plan-hero">
        <div class="container">
          <div class="plan-hero__row">
            <div>
              <div class="plan-hero__badge">Membership</div>
              <h1 class="plan-hero__title">My Plan Details</h1>
              <p class="plan-hero__sub">See your active plan and what you can avail.</p>
            </div>
            <div class="plan-hero__actions">
              <a class="btn-outline" routerLink="/profile">Back to Profile</a>
              <a class="btn-primary" routerLink="/membership">Upgrade / Change Plan</a>
            </div>
          </div>
        </div>
      </div>

      <div class="container plan-shell">
        <div class="card plan-card">
          <div class="card__title">Current plan</div>

          <div *ngIf="loading()" class="muted">Loading your plan…</div>

          <ng-container *ngIf="!loading()">
            <ng-container *ngIf="plan() as p; else noPlan">
              <div class="pill">{{ p.planName }}</div>
              <div class="meta">
                <div><span class="k">Status</span> <span class="v">{{ p.status }}</span></div>
                <div><span class="k">Active from</span> <span class="v">{{ p.activeFrom | date:'dd MMM yyyy' }}</span></div>
                <div *ngIf="p.activeTo"><span class="k">Active to</span> <span class="v">{{ p.activeTo | date:'dd MMM yyyy' }}</span></div>
              </div>
            </ng-container>

            <ng-template #noPlan>
              <div class="muted">You don’t have an active plan right now.</div>
              <div style="margin-top:.85rem">
                <a class="btn-primary" routerLink="/membership">Choose a plan</a>
              </div>
            </ng-template>
          </ng-container>
        </div>

        <div class="card plan-card" *ngIf="!loading() && plan()">
          <div class="card__title">Benefits included</div>
          <div class="muted" style="margin-bottom:.8rem">Based on your current plan.</div>

          <div class="benefits">
            <div class="benefit" *ngFor="let b of benefits">
              <div class="benefit__title">{{ b.title }}</div>
              <div class="benefit__desc">{{ b.desc }}</div>
            </div>
          </div>

          <div class="note">
            For full offering details, visit <a routerLink="/membership">Pricing / Plans</a> and click “Learn more” links.
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .plan-page{
        background:
          radial-gradient(circle at 10% 10%, rgba(230,57,70,.06) 0%, transparent 45%),
          radial-gradient(circle at 90% 20%, rgba(15,52,96,.06) 0%, transparent 45%),
          #fff;
      }

      .plan-hero{
        background: linear-gradient(135deg, var(--navy), var(--navy2));
        color:#fff;
        padding: 2.2rem 0 1.6rem;
      }

      .plan-hero__row{
        display:flex;
        justify-content:space-between;
        align-items:flex-end;
        gap:1rem;
      }

      .plan-hero__badge{
        display:inline-flex;
        align-items:center;
        background: rgba(255,255,255,.14);
        border: 1px solid rgba(255,255,255,.22);
        padding:.35rem .75rem;
        border-radius:999px;
        font-size:.72rem;
        font-weight:900;
        letter-spacing:.08em;
        text-transform:uppercase;
        margin-bottom:.8rem;
      }

      .plan-hero__title{
        font-size: 2.05rem;
        line-height:1.1;
        font-weight: 950;
        margin-bottom: .55rem;
      }

      .plan-hero__sub{
        color: rgba(255,255,255,.78);
        max-width: 54ch;
        font-size: 1rem;
      }

      .plan-hero__actions{
        display:flex;
        gap:.6rem;
        flex-wrap:wrap;
      }

      .plan-shell{
        padding: 1.2rem 0 3rem;
        display:grid;
        grid-template-columns: 420px 1fr;
        gap: 1rem;
        align-items:start;
      }

      .plan-card{
        background:#fff;
        border:1px solid var(--border);
        border-radius: 16px;
        box-shadow: var(--shadow);
        padding: 1.05rem 1.15rem;
      }

      .pill{
        display:inline-block;
        padding:.35rem .75rem;
        border-radius:999px;
        background: var(--green-light);
        color: var(--green);
        font-weight: 950;
        margin:.2rem 0 .8rem 0;
      }

      .meta{
        display:grid;
        gap:.35rem;
      }
      .meta .k{ color: var(--muted); font-weight: 900; }
      .meta .v{ color: var(--navy); font-weight: 900; }

      .benefits{
        display:grid;
        grid-template-columns: repeat(2, minmax(0,1fr));
        gap: .8rem;
      }

      .benefit{
        border: 1px solid rgba(0,0,0,.10);
        border-radius: 14px;
        padding: .85rem .85rem;
        background:#fff;
      }

      .benefit__title{
        font-weight: 950;
        color: var(--navy);
        margin-bottom: .25rem;
      }
      .benefit__desc{
        color: var(--muted);
        font-weight: 600;
        font-size: .92rem;
      }

      .note{
        margin-top: 1rem;
        padding: .85rem 1rem;
        border-radius: 14px;
        background: #f8f9fa;
        border: 1px solid rgba(0,0,0,.06);
        color: #555;
        font-weight: 600;
      }
      .note a{ color: var(--red); font-weight: 900; }

      @media (max-width: 980px){
        .plan-shell{ grid-template-columns: 1fr; }
        .plan-hero__row{ align-items:flex-start; flex-direction:column; }
      }

      @media (max-width: 720px){
        .benefits{ grid-template-columns: 1fr; }
        .plan-hero__title{ font-size: 1.7rem; }
        .plan-hero{ padding: 1.75rem 0 1.35rem; }
      }
    `,
  ],
})
export class MyPlan {
  private readonly session = inject(AuthSessionService);
  private readonly payments = inject(PaymentService);
  private readonly toast = inject(ToastService);

  readonly userId = this.session.userId;
  readonly loading = signal(false);
  readonly plan = signal<ActivePlan | null>(null);

  constructor() {
    effect(() => {
      const id = this.userId();
      if (!id) return;
      void this.load(id);
    });
  }

  get benefits(): Benefit[] {
    const code = (this.plan()?.planCode || '').toLowerCase();
    return PLAN_BENEFITS[code] ?? [];
  }

  private async load(userId: string) {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.payments.myPlan(userId).pipe(timeout(15000)));
      this.plan.set(res?.plan ?? null);
    } catch (e: any) {
      this.plan.set(null);
      const msg =
        (e?.error?.message as string | undefined) ||
        (typeof e?.error === 'string' ? e.error : undefined) ||
        e?.message ||
        'Failed to load plan details.';
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }
}

