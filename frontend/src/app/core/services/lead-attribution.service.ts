import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import { appendAdminListParams, type AdminListQueryOpts } from '../utils/admin-list-params';

export type LeadAttributionStats = {
  totalLeads: number;
  directLeads: number;
  employeeLeads: number;
  partnerLeads: number;
  organicLeads: number;
  campaignLeads: number;
  unknownLeads: number;
  totalMembershipSales: number;
  totalReportRequests: number;
};

export type LeadSourceBreakdown = {
  sourceType: string;
  count: number;
};

export type LeadPerformer = {
  name: string;
  code?: string | null;
  leadsGenerated: number;
  membershipsSold: number;
  reportsGenerated: number;
  conversionRate: number;
  revenuePaise: number;
  paymentCount: number;
};

export type LeadPerformerPayment = {
  userId: string;
  memberId: string;
  fullName: string;
  planCode: string;
  planName: string;
  orderType: string;
  paidAt: string;
  totalAmountPaise: number;
  referralCode?: string | null;
};

export type LeadPerformerDetail = {
  performerName?: string | null;
  performerCode?: string | null;
  totalRevenuePaise: number;
  paymentCount: number;
  payments?: LeadPerformerPayment[];
};

export type LeadCustomerListItem = {
  userId: string;
  memberId: string;
  fullName: string;
  phone: string;
  email: string;
  registrationDate: string;
  sourceType: string;
  sourceName?: string | null;
  sourceCode?: string | null;
  assignedEmployee?: string | null;
  membershipStatus?: string | null;
  planName?: string | null;
  membershipSalesCount: number;
  reportPurchaseCount: number;
  reportGeneratedCount: number;
  reportCount: number;
};

export type LeadPaymentHistoryItem = {
  paymentOrderId: string;
  planCode: string;
  planName: string;
  orderType: string;
  paidAt: string;
  referralCode?: string | null;
  totalAmountPaise: number;
};

export type LeadCustomerDetail = LeadCustomerListItem & {
  assignedAdvisor?: string | null;
  createdThrough?: string | null;
  planCode?: string | null;
  firstPaidAt?: string | null;
  firstPaidPlanCode?: string | null;
  firstPaidPlanName?: string | null;
  firstReferralCodeRaw?: string | null;
  paymentHistory?: LeadPaymentHistoryItem[];
};

export type LeadFilterOptions = {
  sourceTypes: string[];
  sourceNames: string[];
  employeeNames: string[];
  partnerNames: string[];
  planCodes: string[];
};

@Injectable({ providedIn: 'root' })
export class LeadAttributionService {
  private readonly http = inject(HttpClient);

  dashboard(opts?: { dateFrom?: string; dateTo?: string }): Observable<{
    success: boolean;
    message?: string;
    stats?: LeadAttributionStats;
    sourceBreakdown?: LeadSourceBreakdown[];
    topEmployees?: LeadPerformer[];
    topPartners?: LeadPerformer[];
  }> {
    const params: Record<string, string> = {};
    if (opts?.dateFrom?.trim()) params['dateFrom'] = opts.dateFrom.trim();
    if (opts?.dateTo?.trim()) params['dateTo'] = opts.dateTo.trim();
    return this.http.get<{
      success: boolean;
      message?: string;
      stats?: LeadAttributionStats;
      sourceBreakdown?: LeadSourceBreakdown[];
      topEmployees?: LeadPerformer[];
      topPartners?: LeadPerformer[];
    }>(apiUrl('/api/admin/lead-attribution/dashboard'), { params });
  }

  performerDetails(
    performerType: 'employee' | 'rba',
    code: string,
    opts?: { dateFrom?: string; dateTo?: string },
  ): Observable<{ success: boolean; message?: string } & Partial<LeadPerformerDetail>> {
    const params: Record<string, string> = { code };
    if (opts?.dateFrom?.trim()) params['dateFrom'] = opts.dateFrom.trim();
    if (opts?.dateTo?.trim()) params['dateTo'] = opts.dateTo.trim();
    return this.http.get<{ success: boolean; message?: string } & Partial<LeadPerformerDetail>>(
      apiUrl(`/api/admin/lead-attribution/performers/${performerType}/details`),
      { params },
    );
  }

  filters(): Observable<{ success: boolean; message?: string } & Partial<LeadFilterOptions>> {
    return this.http.get<{ success: boolean; message?: string } & Partial<LeadFilterOptions>>(
      apiUrl('/api/admin/lead-attribution/filters'),
    );
  }

  list(
    search: string,
    opts?: AdminListQueryOpts & {
      sourceType?: string;
      sourceName?: string;
      employeeName?: string;
      partnerName?: string;
      planCode?: string;
    },
  ): Observable<any> {
    const params = appendAdminListParams({}, opts);
    if (search.trim()) params['search'] = search.trim();
    if (opts?.sourceType?.trim()) params['sourceType'] = opts.sourceType.trim();
    if (opts?.sourceName?.trim()) params['sourceName'] = opts.sourceName.trim();
    if (opts?.employeeName?.trim()) params['employeeName'] = opts.employeeName.trim();
    if (opts?.partnerName?.trim()) params['partnerName'] = opts.partnerName.trim();
    if (opts?.planCode?.trim()) params['planCode'] = opts.planCode.trim();
    return this.http.get(apiUrl('/api/admin/lead-attribution/customers'), { params });
  }

  getCustomer(userId: string): Observable<{ success: boolean; message?: string; customer?: LeadCustomerDetail }> {
    return this.http.get<{ success: boolean; message?: string; customer?: LeadCustomerDetail }>(
      apiUrl(`/api/admin/lead-attribution/customers/${userId}`),
    );
  }
}
