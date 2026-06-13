import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthSessionService } from '../../core/services/auth-session.service';
import {
  PaymentService,
  type ActivePlan,
  type PaymentHistoryItem,
} from '../../core/services/payment.service';
import { ToastService } from '../../core/services/toast.service';
import { LocalDatePipe } from '../../core/pipes/local-date.pipe';
import { SchemeDiscoveryReportAccess } from '../../core/components/scheme-discovery-report-access/scheme-discovery-report-access';

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
    { title: 'Bank statement analyzer', desc: 'Analyzer for financial insights.' },
    { title: 'Free credit report', desc: 'Credit visibility to support growth planning.' },
    { title: 'Website development', desc: 'Website support included.' },
    { title: 'WhatsApp Business platform', desc: 'Advanced WhatsApp platform benefits.' },
    { title: 'Relationship manager', desc: 'Dedicated support (as per plan offering).' },
    { title: 'Loan audit', desc: 'Audit support to optimize loan outcomes.' },
    { title: 'Insurance audit', desc: 'Audit support to optimize insurance outcomes.' },
    { title: 'Events access', desc: 'MSME events access included.' },
  ],
  pro: [
    { title: 'Business assessment report', desc: 'Infomerics business assessment report.' },
    { title: 'Scheme discovery report', desc: 'Personalized government scheme discovery.' },
    { title: 'GeM portal registration', desc: 'Registration & support for GeM portal.' },
    { title: 'Bank statement analyzer', desc: 'Analyzer for financial insights.' },
    { title: 'Free credit report', desc: 'Credit visibility to support growth planning.' },
    { title: 'Website development', desc: 'Website support included.' },
    { title: 'WhatsApp Business platform', desc: 'Advanced WhatsApp platform benefits.' },
    { title: 'Relationship manager', desc: 'Dedicated support (as per plan offering).' },
    { title: 'Loan audit', desc: 'Audit support to optimize loan outcomes.' },
    { title: 'Insurance audit', desc: 'Audit support to optimize insurance outcomes.' },
    { title: 'Events access', desc: 'More events access included.' },
  ],
};

@Component({
  selector: 'app-my-plan',
  standalone: true,
  imports: [CommonModule, RouterLink, LocalDatePipe, SchemeDiscoveryReportAccess],
  template: `
    <div class="page active plan-page">
      <div class="plan-hero">
        <div class="container">
          <div class="plan-hero__row">
            <div>
              <div class="plan-hero__badge">Membership</div>
              <h1 class="plan-hero__title">My Plan Details</h1>
              <p class="plan-hero__sub">See your active plan, renewal date, and payment history.</p>
            </div>
            <div class="plan-hero__actions">
              <a class="btn-outline" routerLink="/profile">Back to Profile</a>
              <a class="btn-primary" routerLink="/membership">Upgrade / Renew</a>
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
                <div class="d-flex" style="justify-content: space-between;">
                  <span class="k">Status</span>
                  <span class="v" style="font-weight: 700;">{{ p.status }}</span>
                </div><hr/>
                <div class="d-flex" style="justify-content: space-between;">
                  <span class="k">Active from</span>
                  <span class="v">{{ p.activeFrom | localDate:'dd MMM yyyy' }}</span>
                </div><hr/>
                <div class="d-flex" style="justify-content: space-between;" *ngIf="p.activeTo">
                  <span class="k">Active to</span>
                  <span class="v">{{ p.activeTo | localDate:'dd MMM yyyy' }}</span>
                </div><hr/>
                <div class="d-flex" style="justify-content: space-between;" *ngIf="p.daysRemaining != null">
                  <span class="k">Days remaining</span>
                  <span class="v" [class.warn]="p.daysRemaining <= 30">{{ p.daysRemaining }} day(s)</span>
                </div><hr/>
              </div>

              <div class="banner warn" *ngIf="p.daysRemaining != null && p.daysRemaining <= 30 && !p.cancelAtPeriodEnd">
                Your plan expires soon. <a routerLink="/membership">Renew now</a> to avoid interruption.
              </div>

              <div class="banner muted" *ngIf="p.cancelAtPeriodEnd">
                Renewal cancelled — you keep access until {{ p.activeTo | localDate:'dd MMM yyyy' }}. No refund for the remaining term.
              </div>

              <div class="actions" *ngIf="!p.cancelAtPeriodEnd">
                <button class="btn-outline" type="button" (click)="cancelRenewal()" [disabled]="cancelling()">
                  {{ cancelling() ? 'Cancelling…' : 'Cancel renewal' }}
                </button>
              </div>
            </ng-container>

            <ng-template #noPlan>
              <div class="muted">You don't have an active plan right now.</div>
              <div style="margin-top:.85rem">
                <a class="btn-primary" routerLink="/membership">Choose a plan</a>
              </div>
            </ng-template>
          </ng-container>
        </div>

        <div class="card plan-card" *ngIf="!loading() && plan()">
          <div class="card__title">Benefits included</div>
          <div class="muted" style="margin-bottom:.8rem">Based on your current plan.</div>
          <app-scheme-discovery-report-access variant="plan-benefit" [plan]="plan()" />
          <div class="benefits">
            <div class="benefit" *ngFor="let b of benefits">
              <div class="benefit__title">{{ b.title }}</div>
              <div class="benefit__desc">{{ b.desc }}</div>
            </div>
          </div>
        </div>

        <div class="card plan-card policy-card">
          <div class="card__title">How upgrades &amp; renewals work</div>
          <ul class="policy-list">
            <li><strong>Renewal (same plan):</strong> Your expiry date is extended by 12 months from the current end date.</li>
            <li><strong>Upgrade (higher plan):</strong> Your old plan ends immediately; the new plan starts today for a fresh 12-month term. Full new plan price is charged — unused time on the old plan is not refunded.</li>
            <li>After every payment you receive an email with your invoice and benefit list.</li>
          </ul>
        </div>

        <div class="card plan-card policy-card">
          <div class="card__title">Billing policy</div>
          <ul class="policy-list">
            <li>One payment = 12 months of membership (GST-inclusive amount at checkout).</li>
            <li>Renewal is manual — you are not auto-charged unless we enable that in future.</li>
            <li>Cancel renewal keeps access until your period end date; no mid-term refund.</li>
            <li>Upgrades take effect immediately; your previous plan ends when the new one starts.</li>
          </ul>
        </div>

        <div class="card plan-card policy-card" *ngIf="history().length > 0">
          <div class="card__title">Payment history</div>
          <div class="record-list">
            <div class="record-table" role="table" aria-label="Payment history">
              <div class="record-table__head record-table__row--history" role="row">
                <div role="columnheader">Plan</div>
                <div role="columnheader">Amount</div>
                <div role="columnheader">Status</div>
                <div role="columnheader">Date</div>
              </div>
              <div class="record-table__row record-table__row--history" role="row" *ngFor="let h of history()">
                <div role="cell">{{ h.planName }}</div>
                <div role="cell">{{ inr(h.amountPaise) }}</div>
                <div role="cell">{{ h.paymentStatus || h.orderStatus }}</div>
                <div role="cell">{{ (h.paidAt || h.createdAt) | localDate:'dd MMM yyyy' }}</div>
              </div>
            </div>
            <article class="record-card" *ngFor="let h of history()">
              <h3 class="record-card__title">{{ h.planName }}</h3>
              <dl class="record-card__fields">
                <div class="record-card__field">
                  <dt>Amount</dt>
                  <dd>{{ inr(h.amountPaise) }}</dd>
                </div>
                <div class="record-card__field">
                  <dt>Status</dt>
                  <dd>{{ h.paymentStatus || h.orderStatus }}</dd>
                </div>
                <div class="record-card__field">
                  <dt>Date</dt>
                  <dd>{{ (h.paidAt || h.createdAt) | localDate:'dd MMM yyyy' }}</dd>
                </div>
              </dl>
            </article>
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
        padding: 2.2rem 20px 1.6rem;
      }
      .plan-hero__row{ display:flex; justify-content:space-between; align-items:flex-end; gap:1rem; }
      .plan-hero__badge{
        display:inline-flex; background: rgba(255,255,255,.14);
        border: 1px solid rgba(255,255,255,.22); padding:.35rem .75rem;
        border-radius:999px; font-size:.72rem; font-weight:900;
        letter-spacing:.08em; text-transform:uppercase; margin-bottom:.8rem;
      }
      .plan-hero__title{ font-size: 2.05rem; font-weight: 950; margin-bottom: .55rem; }
      .plan-hero__sub{ color: rgba(255,255,255,.78); max-width: 54ch; }
      .plan-hero__actions{ display:flex; gap:.6rem; flex-wrap:wrap; }
      .plan-shell{
        padding: 1.2rem 0 3rem;
        display:grid;
        grid-template-columns: 420px 1fr;
        gap: 1rem;
        align-items:start;
      }
      .plan-card{
        background:#fff; border:1px solid var(--border); border-radius: 16px;
        box-shadow: var(--shadow); padding: 1.05rem 1.15rem;
      }
      .policy-card{ grid-column: 1 / -1; }
      .pill{
        display:inline-block; padding:.35rem .75rem; border-radius:999px;
        background: var(--green-light); color: var(--green); font-weight: 600;
        margin:.8rem 0 .8rem 0;
      }
      .meta{ display:grid; gap:.35rem; }
      .meta .k{ color: var(--muted);}
      .meta .v{ color: var(--navy);}
      .meta .v.warn{ color: #b45309; }
      .banner{
        margin-top: .85rem; padding: .75rem .9rem; border-radius: 12px;
        font-size: .9rem; font-weight: 600;
      }
      .banner.warn{ background: #fffbeb; border: 1px solid #fcd34d; color: #92400e; }
      .banner.muted{ background: #f3f4f6; border: 1px solid #e5e7eb; color: #4b5563; }
      .banner a{ color: var(--red); font-weight: 900; }
      .actions{ margin-top: .85rem; }
      .benefits{ display:grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: .8rem; }
      .benefit{
        border: 1px solid rgba(0,0,0,.10); border-radius: 14px;
        padding: .85rem; background:#fff;
      }
      .benefit__title{ font-weight: 550; color: var(--navy); margin-bottom: .25rem; }
      .benefit__desc{ color: var(--muted); font-weight: 600; font-size: .92rem; }
      .policy-list{ margin: 0; padding-left: 1.2rem; color: #555; font-weight: 600; font-size: .92rem; }
      .policy-list li{ margin-bottom: .4rem; }
      .record-list{
        display:flex;
        flex-direction:column;
        gap:.65rem;
      }

      /* Desktop: payment history as table */
      .record-table{
        width:100%;
        border:1px solid var(--border);
        border-radius:14px;
        overflow:hidden;
        background:#fff;
      }
      .record-table__head.record-table__row--history,
      .record-table__row.record-table__row--history{
        display:grid;
        align-items:center;
        gap:.75rem;
        grid-template-columns: 1.2fr .8fr .8fr .9fr;
      }
      .record-table__head.record-table__row--history{
        background:#f8fafc;
        padding:.65rem .75rem;
        border-bottom:1px solid rgba(0,0,0,.06);
        color:var(--muted);
        text-transform:uppercase;
        letter-spacing:.04em;
        font-size:.82rem;
        font-weight:950;
      }
      .record-table__row.record-table__row--history{
        padding:.8rem .75rem;
        border-bottom:1px solid rgba(0,0,0,.06);
      }
      .record-table__row.record-table__row--history:last-child{ border-bottom:none; }
      .record-table__head.record-table__row--history [role="columnheader"]{
        color:inherit;
        font-weight:inherit;
      }
      .record-table__row.record-table__row--history [role="cell"]{
        display:flex;
        align-items:center;
        color:var(--navy);
        font-weight:450;
        min-width:0;
      }

      /* Mobile: payment history as cards */
      .record-card{ display:none; }
      .record-card__title{
        margin:0 0 .6rem;
        font-size:1rem;
        font-weight:950;
        color:var(--navy);
        overflow-wrap:anywhere;
        word-break:break-word;
      }
      .record-card__fields{
        display:flex;
        flex-direction:column;
        gap:.6rem;
      }
      .record-card__field{
        display:grid;
        grid-template-columns:1fr 1fr;
        gap:.8rem;
        align-items:start;
      }
      .record-card__field dt{
        margin:0;
        font-size:.78rem;
        font-weight:900;
        color:var(--muted);
        text-transform:uppercase;
        letter-spacing:.04em;
      }
      .record-card__field dd{
        margin:0;
        font-size:.95rem;
        font-weight:900;
        color:var(--navy);
        overflow-wrap:anywhere;
        word-break:break-word;
      }

      @media (max-width: 1024px){
        .record-table{ display:none; }
        .record-card{
          display:block;
          border:1px solid var(--border);
          border-radius:14px;
          background:#fff;
          padding:1rem;
        }
      }
      @media (max-width: 980px){
        .plan-shell{ grid-template-columns: 1fr; padding-left: var(--page-gutter); padding-right: var(--page-gutter); }
        .plan-hero__row{ flex-direction: column; align-items:flex-start; }
      }
      @media (max-width: 720px){
        .benefits{ grid-template-columns: 1fr; }
        .plan-card{ padding: 1.25rem; }
        .plan-hero__actions .btn-outline,
        .plan-hero__actions .btn-primary{
          width:100%;
          text-align:center;
          display:block;
        }
        .actions .btn-outline{
          width:100%;
          text-align:center;
        }
      }
    `,
  ],
})
export class MyPlan {
  private readonly session = inject(AuthSessionService);
  private readonly payments = inject(PaymentService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly cancelling = signal(false);
  readonly plan = signal<ActivePlan | null>(null);
  readonly history = signal<PaymentHistoryItem[]>([]);

  constructor() {
    effect(() => {
      if (!this.session.isLoggedIn()) return;
      void this.load();
    });
  }

  get benefits(): Benefit[] {
    const code = (this.plan()?.planCode || '').toLowerCase();
    return PLAN_BENEFITS[code] ?? [];
  }

  inr(paise: number): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(
      (paise ?? 0) / 100
    );
  }

  async cancelRenewal() {
    if (!confirm('Cancel renewal? You will keep access until your plan end date. No refund is issued.')) return;
    try {
      this.cancelling.set(true);
      const res = await firstValueFrom(this.payments.cancelSubscription().pipe(timeout(15000)));
      if (res?.success) {
        this.toast.success(res.message || 'Renewal cancelled.');
        await this.load();
      } else {
        this.toast.error(res?.message || 'Could not cancel renewal.');
      }
    } catch (e: any) {
      const msg =
        (e?.error?.message as string | undefined) ||
        e?.message ||
        'Could not cancel renewal.';
      this.toast.error(msg);
    } finally {
      this.cancelling.set(false);
    }
  }

  private async load() {
    try {
      this.loading.set(true);
      const [planRes, histRes] = await Promise.all([
        firstValueFrom(this.payments.myPlan().pipe(timeout(15000))),
        firstValueFrom(this.payments.paymentHistory().pipe(timeout(15000))),
      ]);
      this.plan.set(planRes?.plan ?? null);
      this.history.set(histRes?.items ?? []);
    } catch (e: any) {
      this.plan.set(null);
      this.history.set([]);
      const msg =
        (e?.error?.message as string | undefined) ||
        e?.message ||
        'Failed to load plan details.';
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }
}
