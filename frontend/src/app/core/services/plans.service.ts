import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface PlanFeatureItem {
  planFeatureId?: number | null;
  text: string;
  description?: string | null;
  offeringSlug?: string | null;
  isIncludesLine: boolean;
  sortOrder: number;
}

export interface PublicPlanItem {
  planId: number;
  code: string;
  name: string;
  tagline?: string | null;
  iconEmoji: string;
  badgeClass: string;
  baseAmountPaise: number;
  gstPercent: number;
  gstPaise: number;
  totalAmountPaise: number;
  durationDays: number;
  isActive: boolean;
  isFeatured: boolean;
  sortOrder: number;
  includesPlanCode?: string | null;
  displayPriceSuffix: string;
  ctaLabel?: string | null;
  popularLabel?: string | null;
  updatedAt?: string;
  features: PlanFeatureItem[];
}

export interface PlanListResponse {
  success: boolean;
  message?: string;
  plans?: PublicPlanItem[];
  total?: number;
}

export interface PlanDetailResponse {
  success: boolean;
  message?: string;
  plan?: PublicPlanItem;
}

@Injectable({ providedIn: 'root' })
export class PlansService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/plans';

  listPublic(): Observable<PlanListResponse> {
    return this.http.get<PlanListResponse>(apiUrl(this.base));
  }

  getByCode(code: string): Observable<PlanDetailResponse> {
    return this.http.get<PlanDetailResponse>(apiUrl(`${this.base}/${encodeURIComponent(code)}`));
  }
}
