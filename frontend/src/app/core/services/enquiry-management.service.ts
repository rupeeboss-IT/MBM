import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import { appendAdminListParams, type AdminListQueryOpts } from '../utils/admin-list-params';

export type EnquiryManagementStats = {
  totalEnquiries: number;
  newEnquiries: number;
  readEnquiries: number;
  inProgressEnquiries: number;
  resolvedEnquiries: number;
  closedEnquiries: number;
};

export type EnquiryListItem = {
  id: number;
  fullName: string;
  companyName?: string | null;
  phone: string;
  email: string;
  source: string;
  subject: string;
  createdAt: string;
  status: string;
  assignedToName?: string | null;
  isUnread: boolean;
};

export type EnquiryStatusHistoryItem = {
  id: string;
  oldStatus?: string | null;
  newStatus: string;
  changedByUserId: string;
  changedByName?: string | null;
  changedOn: string;
  remarks?: string | null;
};

export type EnquiryDetail = {
  id: number;
  fullName: string;
  companyName?: string | null;
  phone: string;
  email: string;
  subject: string;
  message: string;
  source: string;
  createdAt: string;
  status: string;
  assignedToName?: string | null;
  statusHistory: EnquiryStatusHistoryItem[];
};

export type EnquiryListQueryOpts = AdminListQueryOpts & {
  source?: string;
};

@Injectable({ providedIn: 'root' })
export class EnquiryManagementService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/admin/enquiry-management';

  stats(): Observable<{ success: boolean; message?: string; stats?: EnquiryManagementStats }> {
    return this.http.get<{ success: boolean; message?: string; stats?: EnquiryManagementStats }>(
      apiUrl(`${this.base}/stats`),
    );
  }

  filters(): Observable<{
    success: boolean;
    message?: string;
    sources?: string[];
    statuses?: string[];
  }> {
    return this.http.get<{
      success: boolean;
      message?: string;
      sources?: string[];
      statuses?: string[];
    }>(apiUrl(`${this.base}/filters`));
  }

  list(
    search: string,
    opts?: EnquiryListQueryOpts,
  ): Observable<{
    success: boolean;
    message?: string;
    enquiries?: EnquiryListItem[];
    total?: number;
    page?: number;
    pageSize?: number;
  }> {
    const params = appendAdminListParams({}, opts);
    if (search.trim()) params['search'] = search.trim();
    if (opts?.source?.trim()) params['source'] = opts.source.trim();
    return this.http.get<{
      success: boolean;
      message?: string;
      enquiries?: EnquiryListItem[];
      total?: number;
      page?: number;
      pageSize?: number;
    }>(apiUrl(`${this.base}/enquiries`), { params });
  }

  get(enquiryId: number): Observable<{
    success: boolean;
    message?: string;
    enquiry?: EnquiryDetail;
  }> {
    return this.http.get<{ success: boolean; message?: string; enquiry?: EnquiryDetail }>(
      apiUrl(`${this.base}/enquiries/${enquiryId}`),
    );
  }

  updateStatus(
    enquiryId: number,
    status: string,
    remarks?: string | null,
  ): Observable<{ success: boolean; message?: string; updatedCount?: number }> {
    return this.http.patch<{ success: boolean; message?: string; updatedCount?: number }>(
      apiUrl(`${this.base}/enquiries/${enquiryId}/status`),
      { status, remarks: remarks ?? null },
    );
  }

  bulkUpdateStatus(
    enquiryIds: number[],
    status: string,
    remarks?: string | null,
  ): Observable<{ success: boolean; message?: string; updatedCount?: number }> {
    return this.http.post<{ success: boolean; message?: string; updatedCount?: number }>(
      apiUrl(`${this.base}/enquiries/bulk-status`),
      { enquiryIds, status, remarks: remarks ?? null },
    );
  }
}
