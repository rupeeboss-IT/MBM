import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  PlanManagementService,
  type PlanFeatureItem,
  type UpsertPlanRequest,
} from '../../../core/services/plan-management.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { paiseToRupees, rupeesToPaise } from '../../../core/utils/plan-price.util';

interface FeatureRow {
  text: string;
  description: string;
  offeringSlug: string;
  isIncludesLine: boolean;
  sortOrder: number;
}

interface PlanFormData {
  name: string;
  code: string;
  tagline: string;
  iconEmoji: string;
  badgeClass: string;
  baseAmountRupees: number;
  gstPercent: number;
  durationDays: number;
  isActive: boolean;
  isFeatured: boolean;
  sortOrder: number;
  includesPlanCode: string;
  displayPriceSuffix: string;
  ctaLabel: string;
  popularLabel: string;
}

const BADGE_CLASSES = [
  { value: 'free', label: 'Free (green)' },
  { value: 'gold', label: 'Gold (orange)' },
  { value: 'platinum', label: 'Platinum (blue)' },
] as const;

const INCLUDES_OPTIONS = [
  { value: '', label: 'None' },
  { value: 'basic', label: 'Basic' },
  { value: 'premium', label: 'Premium' },
] as const;

const EMOJI_PRESETS = ['🌱', '🏆', '💎', '🌐'] as const;

const PLAN_EMOJI_BY_CODE: Record<string, string> = {
  basic: '🌱',
  premium: '🏆',
  pro: '💎',
  standard: '🌐',
};

function normalizeIconEmoji(code: string, emoji: string): string {
  const value = emoji?.trim() ?? '';
  if (!value || /[ðŸŒ]/.test(value) || (value.includes('?') && value.length <= 6)) {
    return PLAN_EMOJI_BY_CODE[code.toLowerCase()] ?? '🌱';
  }
  return value;
}

function emptyForm(): PlanFormData {
  return {
    name: '',
    code: '',
    tagline: '',
    iconEmoji: '🌱',
    badgeClass: 'free',
    baseAmountRupees: 0,
    gstPercent: 18,
    durationDays: 365,
    isActive: true,
    isFeatured: false,
    sortOrder: 1,
    includesPlanCode: '',
    displayPriceSuffix: '/ year + 18% GST',
    ctaLabel: '',
    popularLabel: '',
  };
}

function emptyFeatureRow(): FeatureRow {
  return { text: '', description: '', offeringSlug: '', isIncludesLine: false, sortOrder: 0 };
}

@Component({
  selector: 'app-plan-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './plan-form.html',
  styleUrls: ['../../admin-shared.css', './plan-form.css'],
})
export class PlanForm implements OnInit {
  private readonly api = inject(PlanManagementService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly badgeClasses = BADGE_CLASSES;
  readonly includesOptions = INCLUDES_OPTIONS;
  readonly emojiPresets = EMOJI_PRESETS;

  readonly planId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly form = signal<PlanFormData>(emptyForm());
  readonly features = signal<FeatureRow[]>([]);

  readonly previewTotal = computed(() => {
    const f = this.form();
    const base = rupeesToPaise(f.baseAmountRupees);
    const gst = Math.round(base * f.gstPercent / 100);
    return (base + gst) / 100;
  });

  async ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('planId');
    if (!idParam) {
      this.toast.error('Plan not found.');
      void this.router.navigate(['/admin/plan-management']);
      return;
    }
    const id = Number(idParam);
    this.planId.set(id);
    await this.loadExisting(id);
  }

  private async loadExisting(planId: number) {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminGet(planId));
      if (!res.success || !res.plan) {
        this.toast.error(res.message ?? 'Plan not found.');
        void this.router.navigate(['/admin/plan-management']);
        return;
      }
      const p = res.plan;
      this.form.set({
        name: p.name,
        code: p.code,
        tagline: p.tagline ?? '',
        iconEmoji: normalizeIconEmoji(p.code, p.iconEmoji || ''),
        badgeClass: p.badgeClass || 'free',
        baseAmountRupees: paiseToRupees(p.baseAmountPaise),
        gstPercent: p.gstPercent,
        durationDays: p.durationDays,
        isActive: p.isActive,
        isFeatured: p.isFeatured,
        sortOrder: p.sortOrder,
        includesPlanCode: p.includesPlanCode ?? '',
        displayPriceSuffix: p.displayPriceSuffix || '/ year + 18% GST',
        ctaLabel: p.ctaLabel ?? '',
        popularLabel: p.popularLabel ?? '',
      });
      this.features.set(
        (p.features ?? []).map((f, i) => ({
          text: f.text,
          description: f.description ?? '',
          offeringSlug: f.offeringSlug ?? '',
          isIncludesLine: f.isIncludesLine,
          sortOrder: f.sortOrder ?? i + 1,
        })),
      );
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load plan.'));
      void this.router.navigate(['/admin/plan-management']);
    } finally {
      this.loading.set(false);
    }
  }

  updateField<K extends keyof PlanFormData>(key: K, value: PlanFormData[K]) {
    this.form.update((f) => ({ ...f, [key]: value }));
  }

  parseNumberField(raw: string | number): number {
    const n = typeof raw === 'number' ? raw : Number.parseFloat(String(raw));
    return Number.isFinite(n) ? n : 0;
  }

  parseIntField(raw: string | number): number {
    const n = typeof raw === 'number' ? raw : Number.parseInt(String(raw), 10);
    return Number.isFinite(n) ? n : 0;
  }

  cancel() {
    void this.router.navigate(['/admin/plan-management']);
  }

  addFeature() {
    this.features.update((list) => [...list, { ...emptyFeatureRow(), sortOrder: list.length + 1 }]);
  }

  removeFeature(index: number) {
    this.features.update((list) => list.filter((_, i) => i !== index));
  }

  moveFeature(index: number, direction: -1 | 1) {
    const target = index + direction;
    const list = [...this.features()];
    if (target < 0 || target >= list.length) return;
    [list[index], list[target]] = [list[target], list[index]];
    this.features.set(list.map((f, i) => ({ ...f, sortOrder: i + 1 })));
  }

  updateFeature(index: number, patch: Partial<FeatureRow>) {
    this.features.update((list) =>
      list.map((f, i) => (i === index ? { ...f, ...patch } : f)),
    );
  }

  setEmoji(emoji: string) {
    this.updateField('iconEmoji', emoji);
  }

  async save() {
    const id = this.planId();
    const f = this.form();
    if (!id) return;
    if (!f.name.trim()) {
      this.toast.error('Plan name is required.');
      return;
    }

    const featureItems: PlanFeatureItem[] = this.features()
      .filter((row) => row.text.trim())
      .map((row, i) => ({
        text: row.text.trim(),
        description: row.description.trim() || null,
        offeringSlug: row.offeringSlug.trim() || null,
        isIncludesLine: row.isIncludesLine,
        sortOrder: i + 1,
      }));

    const req: UpsertPlanRequest = {
      name: f.name.trim(),
      tagline: f.tagline.trim() || null,
      iconEmoji: f.iconEmoji.trim() || '🌱',
      badgeClass: f.badgeClass,
      baseAmountPaise: rupeesToPaise(f.baseAmountRupees),
      gstPercent: f.gstPercent,
      durationDays: f.durationDays,
      isActive: f.isActive,
      isFeatured: f.isFeatured,
      sortOrder: f.sortOrder,
      includesPlanCode: f.includesPlanCode.trim() || null,
      displayPriceSuffix: f.displayPriceSuffix.trim() || '/ year + 18% GST',
      ctaLabel: f.ctaLabel.trim() || null,
      popularLabel: f.popularLabel.trim() || null,
      features: featureItems,
    };

    this.saving.set(true);
    try {
      const res = await firstValueFrom(this.api.update(id, req));
      if (res.success) {
        this.toast.success(res.message ?? 'Plan saved.');
        void this.router.navigate(['/admin/plan-management']);
      } else {
        this.toast.error(res.message ?? 'Failed to save plan.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to save plan.'));
    } finally {
      this.saving.set(false);
    }
  }
}
