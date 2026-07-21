import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import type { PlanFeatureItem, PlanListResponse, PlanDetailResponse, PublicPlanItem } from './plans.service';

export interface UpsertPlanRequest {
  name: string;
  tagline?: string | null;
  iconEmoji: string;
  badgeClass: string;
  baseAmountPaise: number;
  gstPercent: number;
  durationDays: number;
  isActive: boolean;
  isFeatured: boolean;
  sortOrder: number;
  includesPlanCode?: string | null;
  displayPriceSuffix: string;
  ctaLabel?: string | null;
  popularLabel?: string | null;
  features?: PlanFeatureItem[] | null;
}

export interface PlanMutationResponse {
  success: boolean;
  message?: string;
  planId?: number;
}

export type { PublicPlanItem, PlanFeatureItem };

@Injectable({ providedIn: 'root' })
export class PlanManagementService {
  private readonly http = inject(HttpClient);
  private readonly adminBase = '/api/plans/admin';

  adminList(): Observable<PlanListResponse> {
    return this.http.get<PlanListResponse>(apiUrl(this.adminBase));
  }

  adminGet(planId: number): Observable<PlanDetailResponse> {
    return this.http.get<PlanDetailResponse>(apiUrl(`${this.adminBase}/${planId}`));
  }

  update(planId: number, req: UpsertPlanRequest): Observable<PlanMutationResponse> {
    return this.http.put<PlanMutationResponse>(apiUrl(`${this.adminBase}/${planId}`), req);
  }

  setActive(planId: number, isActive: boolean): Observable<PlanMutationResponse> {
    return this.http.patch<PlanMutationResponse>(apiUrl(`${this.adminBase}/${planId}/active`), { isActive });
  }
}
