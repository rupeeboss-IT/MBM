import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JoinCtaService } from '../../services/join-cta.service';

/**
 * Offering sidebar primary CTA — same routing as Join Today:
 * guest → register, logged-in without plan → membership, active member → my-plan.
 */
@Component({
  selector: 'app-offering-cta-actions',
  standalone: true,
  imports: [RouterLink],
  template: `
    <a
      class="btn-primary"
      style="width:100%;margin-bottom:.8rem;text-align:center;display:block"
      [routerLink]="cta.route()"
      [attr.aria-busy]="cta.loading() || null"
    >
      {{ primaryLabel() }}
    </a>
    <a
      class="btn-outline"
      style="width:100%;text-align:center;display:block"
      routerLink="/membership"
    >
      View All Plans
    </a>
  `,
})
export class OfferingCtaActions {
  readonly cta = inject(JoinCtaService);

  /** Per-offering headline, e.g. "Discover Your Schemes". */
  readonly ctaTitle = input.required<string>();

  readonly primaryLabel = computed(() => {
    switch (this.cta.audience()) {
      case 'guest':
        return `${this.ctaTitle()} →`;
      case 'member':
        return 'View Membership';
      default:
        return 'Explore Membership Plans';
    }
  });
}
