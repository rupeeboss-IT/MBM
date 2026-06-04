import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JoinCtaService } from '../../services/join-cta.service';

/** Smart CTA: guest → register, logged-in → membership, active member → my-plan. */
@Component({
  selector: 'app-join-today-button',
  standalone: true,
  imports: [RouterLink],
  template: `
    <a
      [class]="cssClass()"
      [routerLink]="cta.route()"
      [attr.aria-busy]="cta.loading() || null"
    >
      {{ cta.buttonLabel() }}
    </a>
  `,
})
export class JoinTodayButton {
  readonly cta = inject(JoinCtaService);

  /** Matches existing site button classes (btn-white-lg | btn-primary-lg). */
  readonly variant = input<'white-lg' | 'primary-lg'>('white-lg');

  readonly cssClass = computed(() =>
    this.variant() === 'primary-lg' ? 'btn-primary-lg' : 'btn-white-lg'
  );
}
