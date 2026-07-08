import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import { appendAdminListParams, type AdminListQueryOpts } from '../utils/admin-list-params';

export type CreditRepairManagementStats = {
  totalLeads: number;
  todayLeads: number;
  last7DaysLeads: number;
  linkedToLeadData: number;
  unlinkedLeads: number;
};

export type CreditRepairSourceBreakdown = {
  source: string;
  count: number;
};

export type CreditRepairCampaignBreakdown = {
  campaignName: string;
  count: number;
};

export type CreditRepairListItem = {
  id: number;
  fullName: string;
  phone: string;
  email?: string | null;
  source: string;
  campaignName: string;
  consentAccepted: boolean;
  createdAt: string;
  leadId?: number | null;
};

export type CreditRepairListQueryOpts = AdminListQueryOpts & {
  source?: string;
  campaign?: string;
  linkStatus?: string;
};

@Injectable({ providedIn: 'root' })
export class CreditRepairManagementService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/admin/credit-repair';

  stats(opts?: { dateFrom?: string; dateTo?: string }): Observable<{
    success: boolean;
    message?: string;
    stats?: CreditRepairManagementStats;
    sourceBreakdown?: CreditRepairSourceBreakdown[];
    campaignBreakdown?: CreditRepairCampaignBreakdown[];
  }> {
    const params = new URLSearchParams();
    if (opts?.dateFrom) params.set('dateFrom', opts.dateFrom);
    if (opts?.dateTo) params.set('dateTo', opts.dateTo);
    const q = params.toString();
    return this.http.get<{
      success: boolean;
      message?: string;
      stats?: CreditRepairManagementStats;
      sourceBreakdown?: CreditRepairSourceBreakdown[];
      campaignBreakdown?: CreditRepairCampaignBreakdown[];
    }>(apiUrl(`${this.base}/stats${q ? `?${q}` : ''}`));
  }

  filters(): Observable<{
    success: boolean;
    message?: string;
    sources?: string[];
    campaigns?: string[];
  }> {
    return this.http.get<{
      success: boolean;
      message?: string;
      sources?: string[];
      campaigns?: string[];
    }>(apiUrl(`${this.base}/filters`));
  }

  list(
    search: string,
    opts: CreditRepairListQueryOpts,
  ): Observable<{
    success: boolean;
    message?: string;
    leads?: CreditRepairListItem[];
    total?: number;
    page?: number;
    pageSize?: number;
  }> {
    const params = appendAdminListParams({}, opts);
    if (search.trim()) params['search'] = search.trim();
    if (opts.source?.trim()) params['source'] = opts.source.trim();
    if (opts.campaign?.trim()) params['campaign'] = opts.campaign.trim();
    if (opts.linkStatus?.trim()) params['linkStatus'] = opts.linkStatus.trim();
    return this.http.get<{
      success: boolean;
      message?: string;
      leads?: CreditRepairListItem[];
      total?: number;
      page?: number;
      pageSize?: number;
    }>(apiUrl(`${this.base}/leads`), { params });
  }
}
