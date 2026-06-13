import type { ActivePlan } from '../services/payment.service';
import { isApiLocalDateOnOrBeforeNow } from './parse-api-local-date';
import { isPremiumOrProPlanCode } from './scheme-discovery-journey.util';

/** Active Pro or Premium membership eligible for included Scheme Discovery Report. */
export function isActivePremiumOrProMembership(plan: ActivePlan | null | undefined): boolean {
  if (!plan) return false;
  if ((plan.status ?? '').trim().toLowerCase() !== 'active') return false;
  if (plan.activeTo && isApiLocalDateOnOrBeforeNow(plan.activeTo)) return false;
  return isPremiumOrProPlanCode(plan.planCode);
}
