import { CommonModule } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import type { ActivePlan } from '../../services/payment.service';
import { SchemeDiscoveryFlowService } from '../../services/scheme-discovery-flow.service';
import { isActivePremiumOrProMembership } from '../../utils/membership-eligibility.util';

export type SchemeDiscoveryAccessVariant = 'profile-card' | 'plan-benefit' | 'dashboard-action';

@Component({
  selector: 'app-scheme-discovery-report-access',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (eligible()) {
      @switch (variant()) {
        @case ('profile-card') {
          <section class="card profile-card profile-card--scheme-discovery">
            <div class="card__title">Scheme Discovery Report</div>
            <p class="muted">
              Generate and access government scheme recommendations for your business.
            </p>
            <button
              type="button"
              class="btn-primary sd-access-btn"
              (click)="startReport()"
              [disabled]="busy()"
              [attr.aria-busy]="busy() || null">
              {{ busy() ? 'Loading…' : 'Generate Report' }}
            </button>
          </section>
        }
        @case ('plan-benefit') {
          <div class="sd-access-benefit">
            <div class="sd-access-benefit__row">
              <span class="sd-access-benefit__check" aria-hidden="true">✓</span>
              <span class="sd-access-benefit__label">Scheme Discovery Report</span>
            </div>
            <button
              type="button"
              class="btn-outline sd-access-btn"
              (click)="startReport()"
              [disabled]="busy()"
              [attr.aria-busy]="busy() || null">
              {{ busy() ? 'Loading…' : 'Generate Report' }}
            </button>
          </div>
        }
        @case ('dashboard-action') {
          <div class="sd-access-dashboard">
            <div class="sd-access-dashboard__label">Generate Scheme Discovery Report</div>
            <button
              type="button"
              class="btn-outline btn-sm"
              (click)="startReport()"
              [disabled]="busy()"
              [attr.aria-busy]="busy() || null">
              {{ busy() ? 'Loading…' : 'Start Now' }}
            </button>
          </div>
        }
      }
    }
  `,
  styles: [
    `
      .sd-access-btn {
        margin-top: 0.85rem;
      }

      .profile-card--scheme-discovery .sd-access-btn {
        width: 100%;
        text-align: center;
        display: block;
      }

      .sd-access-benefit {
        margin-bottom: 1rem;
        padding: 0.9rem 1rem;
        border: 1px solid rgba(27, 94, 32, 0.18);
        border-radius: 14px;
        background: #f1f8f4;
      }

      .sd-access-benefit__row {
        display: flex;
        align-items: center;
        gap: 0.45rem;
        margin-bottom: 0.65rem;
        color: var(--navy);
        font-weight: 700;
      }

      .sd-access-benefit__check {
        color: var(--green);
        font-weight: 900;
      }

      .sd-access-dashboard {
        margin-top: 0.85rem;
        padding-top: 0.85rem;
        border-top: 1px solid rgba(0, 0, 0, 0.08);
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        flex-wrap: wrap;
      }

      .sd-access-dashboard__label {
        font-weight: 800;
        color: var(--navy);
      }
    `,
  ],
})
export class SchemeDiscoveryReportAccess {
  readonly variant = input<SchemeDiscoveryAccessVariant>('profile-card');
  readonly plan = input<ActivePlan | null>(null);

  private readonly flow = inject(SchemeDiscoveryFlowService);

  readonly eligible = computed(() => isActivePremiumOrProMembership(this.plan()));
  readonly busy = this.flow.reportCtaBusy;

  startReport(): void {
    void this.flow.startFlow();
  }
}
