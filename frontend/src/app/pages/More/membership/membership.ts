import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { SchemeDiscoveryFlowService } from '../../../core/services/scheme-discovery-flow.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  MembershipPlanCheckoutService,
  type MembershipPlanCode,
} from '../../../core/services/membership-plan-checkout.service';
import { AnalyticsService } from '../../../core/services/analytics.service';
import {
  consumeStashedMembershipSource,
  getMembershipSourceGuidance,
  isRecommendedPlan,
  parseMembershipSource,
  stashMembershipSource,
  type MembershipSource,
} from '../../../core/utils/membership-source.util';
import { OFFERING_EXPLORER_CARDS } from '../../../data/offering-explorer-cards.data';

@Component({
  selector: 'app-membership',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './membership.html',
  styleUrl: './membership.css',
})
export class Membership implements OnInit {
  readonly offeringExplorerCards = OFFERING_EXPLORER_CARDS;
  readonly checkout = inject(MembershipPlanCheckoutService);
  private readonly session = inject(AuthSessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly schemeDiscovery = inject(SchemeDiscoveryFlowService);
  private readonly analytics = inject(AnalyticsService);

  openFaqIndex: number | null = null;
  readonly membershipSource = signal<MembershipSource | null>(null);
  readonly sourceGuidance = () => getMembershipSourceGuidance(this.membershipSource());
  readonly isRecommendedPlan = isRecommendedPlan;

  toggleFaq(index: number) {
    this.openFaqIndex = this.openFaqIndex === index ? null : index;
  }

  ngOnInit(): void {
    this.resolveMembershipSource();
    this.checkout.processPendingPlanAfterAuth();
  }

  private resolveMembershipSource(): void {
    const fromQuery = parseMembershipSource(this.route.snapshot.queryParamMap.get('source'));
    const fromStash = consumeStashedMembershipSource();
    const source = fromQuery ?? fromStash;
    this.membershipSource.set(source);
    this.checkout.setMembershipSource(source);
  }

  planBadge(code: MembershipPlanCode): string | null {
    const guidance = this.sourceGuidance();
    if (!guidance || !isRecommendedPlan(code, guidance.source)) return null;
    if (code === 'premium') return guidance.premiumBadge;
    if (code === 'pro') return guidance.proBadge;
    return null;
  }

  startSchemeDiscoveryOneTime(): void {
    if (!this.session.isLoggedIn()) {
      stashMembershipSource('scheme-discovery');
      this.toast.info('Please login or register to get your Scheme Discovery Report.');
      void this.router.navigateByUrl('/register');
      return;
    }
    this.schemeDiscovery.beginOneTimeReportCheckout();
  }

  choosePlan(code: MembershipPlanCode): void {
    this.analytics.trackMembershipCheckoutStarted(code);
    void this.checkout.choosePlan(code, { membershipSource: this.membershipSource() });
  }
}
