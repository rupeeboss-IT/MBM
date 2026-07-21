import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { PublicPlanItem } from '../../services/plans.service';
import { formatPlanBasePrice } from '../../utils/plan-price.util';
import {
  getMembershipSourceGuidance,
  isRecommendedPlan,
  type MembershipSource,
} from '../../utils/membership-source.util';
import type { MembershipPlanCode } from '../../services/membership-plan-checkout.service';

@Component({
  selector: 'app-membership-plan-cards',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './membership-plan-cards.html',
  styleUrl: './membership-plan-cards.css',
})
export class MembershipPlanCards {
  @Input({ required: true }) plans: PublicPlanItem[] = [];
  @Input() variant: 'home' | 'membership' | 'register' = 'membership';
  @Input() loading = false;
  @Input() busyPlan: string | null = null;
  @Input() checkingPlan = false;
  @Input() referralModalOpen = false;
  @Input() membershipSource: MembershipSource | null = null;
  @Input() highlightRecommended = false;

  @Output() choose = new EventEmitter<string>();

  readonly formatPrice = formatPlanBasePrice;
  readonly isRecommendedPlan = isRecommendedPlan;

  planBadge(code: string): string | null {
    const guidance = getMembershipSourceGuidance(this.membershipSource);
    if (!guidance || !isRecommendedPlan(code as MembershipPlanCode, guidance.source)) return null;
    if (code === 'premium') return guidance.premiumBadge;
    if (code === 'pro') return guidance.proBadge;
    return null;
  }

  priceLabel(plan: PublicPlanItem): string {
    return this.variant === 'home'
      ? formatPlanBasePrice(plan.baseAmountPaise, true)
      : formatPlanBasePrice(plan.baseAmountPaise);
  }

  ctaLabel(plan: PublicPlanItem): string {
    if (this.busyPlan === plan.code) return 'Processing…';
    return plan.ctaLabel?.trim() || `Get ${plan.name}`;
  }

  cardClass(plan: PublicPlanItem): Record<string, boolean> {
    return {
      'home-plan-card': true,
      'featured-plan': plan.isFeatured,
      'membership-plan--recommended':
        !!this.membershipSource &&
        isRecommendedPlan(plan.code as MembershipPlanCode, this.membershipSource),
      'membership-plan--highlight':
        this.highlightRecommended &&
        !!this.membershipSource &&
        isRecommendedPlan(plan.code as MembershipPlanCode, this.membershipSource),
    };
  }

  onChoose(code: string): void {
    this.choose.emit(code);
  }

  isCheckoutDisabled(code: string): boolean {
    return this.busyPlan === code || this.checkingPlan || this.referralModalOpen;
  }
}
