import { CommonModule } from '@angular/common';

import { Component, inject, signal } from '@angular/core';

import { RouterLink } from '@angular/router';

import { firstValueFrom, timeout } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';

import {

  VendorManagementService,

  type VendorPlanMappingGroup,

} from '../../../core/services/vendor-management.service';

import { getHttpErrorMessage } from '../../../core/utils/http-error-message';



@Component({

  selector: 'app-vendor-plan-mapping',

  standalone: true,

  imports: [CommonModule, RouterLink],

  templateUrl: './vendor-plan-mapping.html',

  styleUrls: ['../../admin-shared.css', './vendor-plan-mapping.css'],

})

export class VendorPlanMapping {

  private readonly api = inject(VendorManagementService);

  private readonly toast = inject(ToastService);



  readonly loading = signal(false);

  readonly groups = signal<VendorPlanMappingGroup[]>([]);

  readonly expandedPlanIds = signal<Set<number>>(new Set());



  constructor() {

    void this.load();

  }



  async load() {

    try {

      this.loading.set(true);

      const res = await firstValueFrom(this.api.listPlanMappings().pipe(timeout(20000)));

      if (res?.success === false) {

        this.toast.error(res.message || 'Unable to load plan mappings.');

        this.groups.set([]);

        this.expandedPlanIds.set(new Set());

        return;

      }

      const groups = res?.groups ?? [];

      this.groups.set(groups);

      if (groups.length > 0) {

        this.expandedPlanIds.set(new Set([groups[0].planId]));

      } else {

        this.expandedPlanIds.set(new Set());

      }

    } catch (e: unknown) {

      this.toast.error(getHttpErrorMessage(e, 'Unable to load plan mappings.'));

      this.groups.set([]);

      this.expandedPlanIds.set(new Set());

    } finally {

      this.loading.set(false);

    }

  }



  isExpanded(planId: number): boolean {

    return this.expandedPlanIds().has(planId);

  }



  togglePlan(planId: number): void {

    const next = new Set(this.expandedPlanIds());

    if (next.has(planId)) {

      next.delete(planId);

    } else {

      next.add(planId);

    }

    this.expandedPlanIds.set(next);

  }

}


