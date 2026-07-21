import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MembershipPlanCards } from '../../../core/components/membership-plan-cards/membership-plan-cards';
import { AuthSessionService } from '../../../core/services/auth-session.service';
import { PlansService, type PublicPlanItem } from '../../../core/services/plans.service';
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
  imports: [CommonModule, RouterLink, MembershipPlanCards],
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
  private readonly plansApi = inject(PlansService);

  openFaqIndex: number | null = null;
  readonly membershipSource = signal<MembershipSource | null>(null);
  readonly cmsPlans = signal<PublicPlanItem[]>([]);
  readonly plansLoading = signal(true);
  readonly sourceGuidance = () => getMembershipSourceGuidance(this.membershipSource());
  readonly isRecommendedPlan = isRecommendedPlan;

  toggleFaq(index: number) {
    this.openFaqIndex = this.openFaqIndex === index ? null : index;
  }

  ngOnInit(): void {
    this.resolveMembershipSource();
    this.checkout.processPendingPlanAfterAuth();
    void this.loadCmsPlans();
  }

  private async loadCmsPlans() {
    try {
      const res = await firstValueFrom(this.plansApi.listPublic());
      this.cmsPlans.set(res.plans ?? []);
    } catch {
      this.cmsPlans.set([]);
    } finally {
      this.plansLoading.set(false);
    }
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

  onPlanChoose(code: string): void {
    this.choosePlan(code as MembershipPlanCode);
  }
}
