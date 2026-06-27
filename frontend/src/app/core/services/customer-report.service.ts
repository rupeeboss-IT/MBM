import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export type CustomerReportListItem = {
  id: string;
  reportName: string;
  uploadDate: string;
  planName: string;
  fileSize: number;
  memberId: string;
  reportType?: string;
};

export type ListReportsRes = {
  success: boolean;
  message?: string;
  items?: CustomerReportListItem[];
};

export type CustomerSearchHit = {
  userId: string;
  memberId: string;
  fullName: string;
  email: string;
  phone: string;
  hasActiveSubscription: boolean;
  planName?: string | null;
  activeTo?: string | null;
};

export type SearchCustomersRes = {
  success: boolean;
  message?: string;
  customers?: CustomerSearchHit[];
};

export type UploadReportRes = {
  success: boolean;
  message?: string;
  reportId?: string;
};

export type ReportHistoryItem = {
  id: string;
  customerName: string;
  memberId: string;
  email: string;
  uploadDate: string;
  downloadCount: number;
  lastDownloadDate?: string | null;
  originalFileName: string;
  fileSize: number;
  planName?: string | null;
  hasPendingRequest?: boolean;
  pendingRequestId?: string | null;
  pendingRequestType?: string | null;
  latestRequestStatus?: string | null;
};

export type ReportHistoryRes = {
  success: boolean;
  message?: string;
  items?: ReportHistoryItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type ReportChangeRequestItem = {
  id: string;
  reportId: string;
  requestType: string;
  status: string;
  reason: string;
  requestedOn: string;
  requestedByName: string;
  customerName?: string | null;
  memberId?: string | null;
  originalFileName?: string | null;
  reportUploadDate?: string | null;
};

export type ReportChangeRequestListRes = {
  success: boolean;
  message?: string;
  items?: ReportChangeRequestItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type ReportSummary = {
  id: string;
  customerName: string;
  memberId: string;
  email: string;
  originalFileName: string;
  fileSize: number;
  uploadDate: string;
  downloadCount: number;
  lastDownloadDate?: string | null;
  planName?: string | null;
  reportType: string;
};

export type ReportChangeRequestDetail = {
  id: string;
  reportId: string;
  requestType: string;
  status: string;
  reason: string;
  remarks?: string | null;
  requestedOn: string;
  requestedBy: string;
  requestedByName: string;
  approvedOn?: string | null;
  approvedByName?: string | null;
  rejectedOn?: string | null;
  rejectedByName?: string | null;
  previousReportPath?: string | null;
  newReportPath?: string | null;
  previousValues?: string | null;
  newValues?: string | null;
  pendingOriginalFileName?: string | null;
  pendingFileSize?: number | null;
  report?: ReportSummary | null;
};

export type ReportChangeRequestDetailRes = {
  success: boolean;
  message?: string;
  request?: ReportChangeRequestDetail | null;
};

export type ReportAuditLogItem = {
  auditId: string;
  action: string;
  createdAt: string;
  actorName?: string | null;
  requestId?: string | null;
  remarks?: string | null;
  previousReportPath?: string | null;
  newReportPath?: string | null;
  previousValues?: string | null;
  newValues?: string | null;
};

export type ReportAuditHistoryRes = {
  success: boolean;
  message?: string;
  items?: ReportAuditLogItem[];
};

export type ActionRes = { success: boolean; message?: string };

export type PendingCountRes = { success: boolean; count: number };

export type CreateChangeRequestRes = { success: boolean; message?: string; requestId?: string };

export type AdminSdrGenerateRes = {
  success: boolean;
  message?: string;
  reportId?: string;
  requestId?: string;
  expiryDate?: string;
  outcome?: string;
};

export type AdminSdrListRes = {
  success: boolean;
  message?: string;
  items?: CustomerReportListItem[];
};

@Injectable({ providedIn: 'root' })
export class CustomerReportService {
  private readonly http = inject(HttpClient);

  listMyReports(): Observable<ListReportsRes> {
    return this.http.get<ListReportsRes>(apiUrl('/api/customer/reports'));
  }

  downloadReport(reportId: string): Observable<Blob> {
    return this.http.get(apiUrl(`/api/customer/reports/${encodeURIComponent(reportId)}/download`), {
      responseType: 'blob',
    });
  }

  adminSearchCustomers(opts: {
    memberId?: string;
    mobile?: string;
    email?: string;
    customerName?: string;
  }): Observable<SearchCustomersRes> {
    const params: Record<string, string> = {};
    if (opts.memberId?.trim()) params['memberId'] = opts.memberId.trim();
    if (opts.mobile?.trim()) params['mobile'] = opts.mobile.trim();
    if (opts.email?.trim()) params['email'] = opts.email.trim();
    if (opts.customerName?.trim()) params['customerName'] = opts.customerName.trim();
    return this.http.get<SearchCustomersRes>(apiUrl('/api/admin/reports/customers/search'), { params });
  }

  adminUpload(customerId: string, file: File): Observable<UploadReportRes> {
    const form = new FormData();
    form.append('customerId', customerId);
    form.append('file', file, file.name);
    return this.http.post<UploadReportRes>(apiUrl('/api/admin/reports/upload'), form);
  }

  adminHistory(
    search: string,
    page: number,
    pageSize: number,
    opts?: {
      dateFrom?: string;
      dateTo?: string;
      sortBy?: string;
      sortDir?: 'asc' | 'desc';
      export?: boolean;
    },
  ): Observable<ReportHistoryRes> {
    const params: Record<string, string> = {
      page: String(page),
      pageSize: String(pageSize),
    };
    if (search.trim()) params['search'] = search.trim();
    if (opts?.dateFrom) params['dateFrom'] = opts.dateFrom;
    if (opts?.dateTo) params['dateTo'] = opts.dateTo;
    if (opts?.sortBy) params['sortBy'] = opts.sortBy;
    if (opts?.sortDir) params['sortDir'] = opts.sortDir;
    if (opts?.export) params['export'] = 'true';
    return this.http.get<ReportHistoryRes>(apiUrl('/api/admin/reports/history'), { params });
  }

  adminPendingRequestCount(): Observable<PendingCountRes> {
    return this.http.get<PendingCountRes>(apiUrl('/api/admin/reports/change-requests/pending-count'));
  }

  adminListChangeRequests(
    page: number,
    pageSize: number,
    opts?: { status?: string },
  ): Observable<ReportChangeRequestListRes> {
    const params: Record<string, string> = {
      page: String(page),
      pageSize: String(pageSize),
    };
    if (opts?.status) params['status'] = opts.status;
    return this.http.get<ReportChangeRequestListRes>(apiUrl('/api/admin/reports/change-requests'), { params });
  }

  adminGetChangeRequest(requestId: string): Observable<ReportChangeRequestDetailRes> {
    return this.http.get<ReportChangeRequestDetailRes>(
      apiUrl(`/api/admin/reports/change-requests/${encodeURIComponent(requestId)}`),
    );
  }

  adminCreateChangeRequest(form: FormData): Observable<CreateChangeRequestRes> {
    return this.http.post<CreateChangeRequestRes>(apiUrl('/api/admin/reports/change-requests'), form);
  }

  adminApproveChangeRequest(requestId: string, remarks?: string): Observable<ActionRes> {
    return this.http.post<ActionRes>(
      apiUrl(`/api/admin/reports/change-requests/${encodeURIComponent(requestId)}/approve`),
      { remarks: remarks?.trim() || null },
    );
  }

  adminRejectChangeRequest(requestId: string, remarks?: string): Observable<ActionRes> {
    return this.http.post<ActionRes>(
      apiUrl(`/api/admin/reports/change-requests/${encodeURIComponent(requestId)}/reject`),
      { remarks: remarks?.trim() || null },
    );
  }

  adminDirectDeleteReport(reportId: string, reason: string): Observable<ActionRes> {
    return this.http.delete<ActionRes>(apiUrl(`/api/admin/reports/${encodeURIComponent(reportId)}`), {
      body: { reason: reason.trim() },
    });
  }

  adminDirectEditReport(reportId: string, originalFileName: string, reason: string): Observable<ActionRes> {
    return this.http.put<ActionRes>(apiUrl(`/api/admin/reports/${encodeURIComponent(reportId)}`), {
      originalFileName: originalFileName.trim(),
      reason: reason.trim(),
    });
  }

  adminDirectReplaceReport(reportId: string, file: File, reason: string): Observable<ActionRes> {
    const form = new FormData();
    form.append('reason', reason.trim());
    form.append('file', file, file.name);
    return this.http.post<ActionRes>(
      apiUrl(`/api/admin/reports/${encodeURIComponent(reportId)}/replace`),
      form,
    );
  }

  adminReportAuditHistory(reportId: string): Observable<ReportAuditHistoryRes> {
    return this.http.get<ReportAuditHistoryRes>(
      apiUrl(`/api/admin/reports/${encodeURIComponent(reportId)}/audit`),
    );
  }

  adminGenerateSdr(customerId: string, udyamNumber: string): Observable<AdminSdrGenerateRes> {
    return this.http.post<AdminSdrGenerateRes>(apiUrl('/api/admin/reports/sdr/generate'), {
      customerId,
      udyamNumber: udyamNumber.trim(),
    });
  }

  adminListCustomerSdrReports(customerId: string): Observable<AdminSdrListRes> {
    return this.http.get<AdminSdrListRes>(
      apiUrl(`/api/admin/reports/sdr/customers/${encodeURIComponent(customerId)}`),
    );
  }

  adminDownloadReport(reportId: string): Observable<Blob> {
    return this.http.get(apiUrl(`/api/admin/reports/${encodeURIComponent(reportId)}/download`), {
      responseType: 'blob',
    });
  }
}
