import { CommonModule } from '@angular/common';
import { Component, computed, effect, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { MeRes } from '../../services/auth.service';
import type { ActivePlan } from '../../services/payment.service';
import { LocalDatePipe } from '../../pipes/local-date.pipe';
import { formatMemberPartnerId } from '../../utils/member-id-display.util';
import { MBM_LOGO_SRC } from '../../brand';

type PlanCode = 'basic' | 'standard' | 'premium' | 'pro';

interface PlanCardConfig {
  label: string;
  tier: string;
  theme: string;
  elite: boolean;
}

const PLANS: Record<PlanCode, PlanCardConfig> = {
  basic: { label: 'Basic', tier: 'Basic Member', theme: 't-blue', elite: false },
  standard: { label: 'Standard', tier: 'Silver Member', theme: 't-silver', elite: false },
  premium: { label: 'Premium', tier: 'Premium Member', theme: 't-gold', elite: true },
  pro: { label: 'Pro', tier: 'Pro · Elite', theme: 't-blackgold', elite: true },
};

const SERVICES = [
  'Business Funding',
  'Govt. Schemes',
  'Market Linkages',
  'Networking',
  'SME IPO Support',
  'Subsidy Advisory',
];

const ELITE_PERKS = [
  'Priority Funding',
  'VIP Networking',
  'National Directory',
  'Dedicated RM',
];

const ORG_PHONE = '+91 90059 00921';
const ORG_EMAIL = 'support@msmebharatmanch.com';
const ORG_SITE = 'www.msmebharatmanch.com';

@Component({
  selector: 'app-membership-card',
  standalone: true,
  imports: [CommonModule, RouterLink, LocalDatePipe],
  templateUrl: './membership-card.html',
  styleUrl: './membership-card.css',
})
export class MembershipCard {
  readonly profile = input<MeRes | null>(null);
  readonly plan = input<ActivePlan | null>(null);
  readonly loading = input(false);
  /** When false, hides footer CTA links (e.g. on my-plan where actions are redundant). */
  readonly showLinks = input(true);

  readonly flipped = signal(false);
  readonly qrDataUrl = signal('');

  readonly services = SERVICES;
  readonly elitePerks = ELITE_PERKS;
  readonly orgPhone = ORG_PHONE;
  readonly orgEmail = ORG_EMAIL;
  readonly orgSite = ORG_SITE;
  readonly logoSrc = MBM_LOGO_SRC;

  readonly memberNo = computed(() => {
    const p = this.profile();
    return formatMemberPartnerId(p?.memberId ?? null, p?.role ?? null) ?? '—';
  });

  readonly planCfg = computed((): PlanCardConfig => {
    if (!this.plan()?.planCode) {
      return {
        label: 'None',
        tier: 'No Active Plan',
        theme: 't-red',
        elite: false,
      };
    }
    const code = (this.plan()?.planCode ?? '').toLowerCase() as PlanCode;
    return PLANS[code] ?? {
      label: 'Member',
      tier: 'MSME Member',
      theme: 't-red',
      elite: false,
    };
  });

  readonly themeClass = computed(() => this.planCfg().theme);
  readonly isElite = computed(() => this.planCfg().elite);
  readonly hasActivePlan = computed(() => !!this.plan()?.planCode);

  readonly verifyUrl = computed(() => 'https://msmebharatmanch.com');

  constructor() {
    effect(() => {
      const url = this.verifyUrl();
      void this.refreshQr(url);
    });
  }

  toggleFlip(): void {
    this.flipped.update((v) => !v);
  }

  onSceneKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.toggleFlip();
    }
  }

  private async refreshQr(url: string): Promise<void> {
    try {
      const QRCode = await import('qrcode');
      const dataUrl = await QRCode.toDataURL(url, {
        width: 300,
        margin: 1,
        color: { dark: '#101010', light: '#ffffff' },
      });
      this.qrDataUrl.set(dataUrl);
    } catch {
      this.qrDataUrl.set('');
    }
  }
}
