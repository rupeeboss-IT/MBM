import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthSessionService } from '../../core/services/auth-session.service';
import {
  ConnectService,
  type ConnectAccessSummary,
  type ConnectProfileCard,
  type ConnectProfileDetail,
  type ConnectStats,
} from '../../core/services/connect.service';
import { ToastService } from '../../core/services/toast.service';
import { MembershipPlanCheckoutService } from '../../core/services/membership-plan-checkout.service';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-connect',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './connect.html',
  styleUrl: './connect.css',
})
export class Connect {
  private readonly session = inject(AuthSessionService);
  private readonly connectApi = inject(ConnectService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly planCheckout = inject(MembershipPlanCheckoutService);

  readonly search = signal('');
  readonly sector = signal('');
  readonly state = signal('');
  readonly turnover = signal('');

  readonly loading = signal(false);
  readonly profiles = signal<ConnectProfileCard[]>([]);
  readonly total = signal(0);
  readonly stats = signal<ConnectStats | null>(null);
  readonly access = signal<ConnectAccessSummary | null>(null);

  readonly upgradeOpen = signal(false);
  readonly selectedUpgradePlan = signal<'premium' | 'pro'>('premium');

  readonly profileOpen = signal(false);
  readonly profileLoading = signal(false);
  readonly currentProfile = signal<ConnectProfileDetail | null>(null);

  readonly hasPlan = computed(() => {
    const tier = this.access()?.tier ?? 'none';
    return tier !== 'none';
  });

  readonly isPremium = computed(() => this.access()?.tier === 'unlimited');

  readonly countLabel = computed(() => this.total());

  readonly display = ConnectService.display;
  readonly connectLabel = ConnectService.connectButtonLabel;

  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      this.search();
      this.sector();
      this.state();
      this.turnover();
      this.session.userId();
      this.session.token();
      if (this.searchTimer) clearTimeout(this.searchTimer);
      this.searchTimer = setTimeout(() => void this.loadProfiles(), 300);
    });
  }

  async loadProfiles() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.connectApi
          .search({
            page: 1,
            pageSize: 100,
            search: this.search().trim() || undefined,
            sector: this.sector() || undefined,
            state: this.state() || undefined,
            turnover: this.turnover() || undefined,
          })
          .pipe(timeout(20000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to load Connect directory.');
        this.profiles.set([]);
        return;
      }
      this.profiles.set(res.profiles ?? []);
      this.total.set(res.total ?? 0);
      this.stats.set(res.stats ?? null);
      this.access.set(res.access ?? null);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load Connect directory.'));
      this.profiles.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  openUpgrade() {
    this.upgradeOpen.set(true);
  }

  closeUpgrade() {
    this.upgradeOpen.set(false);
  }

  proceedToUpgrade() {
    this.closeUpgrade();
    void this.planCheckout.choosePlan(this.selectedUpgradePlan());
  }

  async openProfile(card: ConnectProfileCard) {
    if (card.isLocked) {
      this.openUpgrade();
      return;
    }
    try {
      this.profileLoading.set(true);
      this.profileOpen.set(true);
      const res = await firstValueFrom(
        this.connectApi.getProfile(card.userId).pipe(timeout(20000)),
      );
      if (res?.success === false || !res.profile) {
        this.toast.error(res?.message || 'Unable to load profile.');
        this.closeProfile();
        return;
      }
      this.currentProfile.set(res.profile);
      if (res.access) this.access.set(res.access);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to load profile.'));
      this.closeProfile();
    } finally {
      this.profileLoading.set(false);
    }
  }

  closeProfile() {
    this.profileOpen.set(false);
    this.currentProfile.set(null);
  }

  async connectTo(card: ConnectProfileCard) {
    if (card.isLocked || !this.hasPlan()) {
      if (!this.session.isLoggedIn()) {
        try {
          window.localStorage.setItem('mbm_pending_after_login', '/connect');
        } catch {}
        this.toast.info('Please login to use MSME Connect.');
        this.router.navigateByUrl('/login');
        return;
      }
      this.openUpgrade();
      return;
    }

    if (!this.session.isLoggedIn()) {
      try {
        window.localStorage.setItem('mbm_pending_after_login', '/connect');
      } catch {}
      this.toast.info('Please login to use MSME Connect.');
      this.router.navigateByUrl('/login');
      return;
    }

    if (!card.canConnect) return;

    try {
      const res = await firstValueFrom(
        this.connectApi.sendRequest(card.userId).pipe(timeout(15000)),
      );
      if (res?.success === false) {
        this.toast.error(res.message || 'Unable to send connection request.');
        return;
      }
      this.toast.success(res.message || 'Connection request sent.');
      await this.loadProfiles();
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Unable to send connection request.'));
    }
  }

  avatarColor(id: string): string {
    const colors = ['#C8102E', '#0B1C3D', '#16a34a', '#7c3aed', '#d97706', '#0891b2'];
    let hash = 0;
    for (let i = 0; i < id.length; i++) hash = (hash + id.charCodeAt(i)) % colors.length;
    return colors[hash];
  }

  statsListedLabel(): string {
    const n = this.stats()?.listedCount ?? 0;
    return n > 0 ? `${n}+` : '0';
  }

  unlockNote(): string {
    const a = this.access();
    if (!a || a.tier === 'none') return '';
    if (a.tier === 'unlimited') return `✅ Unlocked (${a.planName ?? 'Premium'})`;
    const limit = a.unlocksLimit ?? 5;
    return `✅ ${a.unlocksUsed} / ${limit} contacts used (${a.planName ?? 'Plan'})`;
  }
}
