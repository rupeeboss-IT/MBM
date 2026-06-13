import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JoinCtaService } from '../../services/join-cta.service';
import { SchemeDiscoveryFlowService } from '../../services/scheme-discovery-flow.service';
import {
  membershipSourceFromOfferingSlug,
  membershipSourceQuery,
} from '../../utils/membership-source.util';

/**
 * Offering sidebar primary CTA — same routing as Join Today, or scheme-discovery flow when enabled.
 */
@Component({
  selector: 'app-offering-cta-actions',
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (schemeDiscoveryMode()) {
      <button
        type="button"
        class="btn-primary"
        style="width:100%;margin-bottom:.8rem;text-align:center;display:block"
        (click)="startSchemeDiscovery()"
        [disabled]="schemeDiscovery.reportCtaBusy()"
        [attr.aria-busy]="schemeDiscovery.reportCtaBusy() || null"
      >
        {{ primaryLabel() }}
      </button>
    } @else {
      <a
        class="btn-primary"
        style="width:100%;margin-bottom:.8rem;text-align:center;display:block"
        [routerLink]="cta.route()"
        [attr.aria-busy]="cta.loading() || null"
      >
        {{ primaryLabel() }}
      </a>
    }
    <a
      class="btn-outline"
      style="width:100%;text-align:center;display:block"
      [routerLink]="['/membership']"
      [queryParams]="viewPlansQuery()"
    >
      View All Plans
    </a>
  `,
})
export class OfferingCtaActions {
  readonly cta = inject(JoinCtaService);
  readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);

  /** Per-offering headline, e.g. "Discover Your Schemes". */
  readonly ctaTitle = input.required<string>();

  /** When true, primary CTA starts auth-first scheme discovery instead of join routing. */
  readonly schemeDiscoveryMode = input(false);

  /** Offering slug for membership source guidance on "View All Plans". */
  readonly offeringSlug = input('');

  readonly viewPlansQuery = computed(() => membershipSourceQuery(membershipSourceFromOfferingSlug(this.offeringSlug())));

  readonly primaryLabel = computed(() => {
    if (this.schemeDiscoveryMode()) {
      return this.ctaTitle();
    }
    switch (this.cta.audience()) {
      case 'guest':
        return `${this.ctaTitle()} →`;
      case 'member':
        return 'View Membership';
      default:
        return 'Explore Membership Plans';
    }
  });

  startSchemeDiscovery(): void {
    void this.schemeDiscovery.startFlow();
  }
}
