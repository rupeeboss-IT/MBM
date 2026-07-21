import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  PlanManagementService,
  type PublicPlanItem,
} from '../../../core/services/plan-management.service';
import { ToastService } from '../../../core/services/toast.service';
import { formatPlanBasePrice } from '../../../core/utils/plan-price.util';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-plan-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './plan-list.html',
  styleUrls: ['../../admin-shared.css', './plan-list.css'],
})
export class PlanList implements OnInit {
  private readonly api = inject(PlanManagementService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly actionId = signal<number | null>(null);
  readonly plans = signal<PublicPlanItem[]>([]);

  readonly sortedPlans = computed(() => {
    const list = [...this.plans()];
    list.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
    return list;
  });

  readonly formatPrice = formatPlanBasePrice;

  async ngOnInit() {
    await this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      this.loadError.set(null);
      const res = await firstValueFrom(this.api.adminList());
      if (!res.success) {
        const msg = res.message ?? 'Failed to load plans.';
        this.loadError.set(msg);
        this.toast.error(msg);
        return;
      }
      this.plans.set(res.plans ?? []);
    } catch (e: unknown) {
      const msg = getHttpErrorMessage(e, 'Failed to load plans.');
      this.loadError.set(msg);
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }

  goToEdit(planId: number) {
    void this.router.navigate(['/admin/plan-management/edit', planId]);
  }

  async toggleActive(plan: PublicPlanItem) {
    this.actionId.set(plan.planId);
    try {
      const res = await firstValueFrom(this.api.setActive(plan.planId, !plan.isActive));
      if (res.success) {
        this.toast.success(res.message ?? 'Status updated.');
        await this.load();
      } else {
        this.toast.error(res.message ?? 'Failed to update status.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to update status.'));
    } finally {
      this.actionId.set(null);
    }
  }

  statusLabel(active: boolean): string {
    return active ? 'Active' : 'Inactive';
  }

  statusBadge(active: boolean): string {
    return active ? 'badge-success' : 'badge-warn';
  }
}
