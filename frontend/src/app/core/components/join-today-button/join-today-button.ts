import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JoinCtaService } from '../../services/join-cta.service';
import { FREE_REGISTER_QUERY_PARAMS } from '../../utils/registration-mode.util';

/** Smart CTA: guest → register, logged-in → membership, active member → my-plan. */
@Component({
  selector: 'app-join-today-button',
  standalone: true,
  imports: [RouterLink],
  template: `
    <a
      [class]="cssClass()"
      [routerLink]="link()"
      [queryParams]="queryParams()"
      [attr.aria-busy]="cta.loading() || null"
    >
      {{ label() }}
    </a>
  `,
})
export class JoinTodayButton {
  readonly cta = inject(JoinCtaService);

  /** Matches existing site button classes (btn-white-lg | btn-primary-lg). */
  readonly variant = input<'white-lg' | 'primary-lg'>('white-lg');

  /** Home page only: guest CTA becomes free registration (skips membership steps). */
  readonly freeRegister = input(false);

  readonly cssClass = computed(() =>
    this.variant() === 'primary-lg' ? 'btn-primary-lg' : 'btn-white-lg'
  );

  readonly label = computed(() => {
    if (this.freeRegister() && this.cta.audience() === 'guest') {
      return 'Register Free';
    }
    return this.cta.buttonLabel();
  });

  readonly link = computed(() => this.cta.route());

  readonly queryParams = computed(() => {
    if (this.freeRegister() && this.cta.audience() === 'guest') {
      return FREE_REGISTER_QUERY_PARAMS;
    }
    return null;
  });
}
