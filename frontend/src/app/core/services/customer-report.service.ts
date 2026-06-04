import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export type CustomerReportListItem = {
  id: string;
  reportName: string;
  uploadDate: string;
  planName: string;
  fileSize: number;
  memberId: string;
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
};

export type ReportHistoryRes = {
  success: boolean;
  message?: string;
  items?: ReportHistoryItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

@Injectable({ providedIn: 'root' })
export class CustomerReportService {
  private readonly http = inject(HttpClient);
  private readonly memberBase = '/api/customer/reports';
  private readonly adminBase = '/api/admin/reports';

  listMyReports(): Observable<ListReportsRes> {
    return this.http.get<ListReportsRes>(this.memberBase);
  }

  downloadReport(reportId: string): Observable<Blob> {
    return this.http.get(`${this.memberBase}/${encodeURIComponent(reportId)}/download`, {
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
    return this.http.get<SearchCustomersRes>(`${this.adminBase}/customers/search`, { params });
  }

  adminUpload(customerId: string, file: File): Observable<UploadReportRes> {
    const form = new FormData();
    form.append('customerId', customerId);
    form.append('file', file, file.name);
    return this.http.post<UploadReportRes>(`${this.adminBase}/upload`, form);
  }

  adminHistory(search: string, page: number, pageSize: number): Observable<ReportHistoryRes> {
    const params: Record<string, string> = {
      page: String(page),
      pageSize: String(pageSize),
    };
    if (search.trim()) params['search'] = search.trim();
    return this.http.get<ReportHistoryRes>(`${this.adminBase}/history`, { params });
  }
}
