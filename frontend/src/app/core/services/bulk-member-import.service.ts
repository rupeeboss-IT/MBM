import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export type BulkImportRowPayload = {
  rowNumber: number;
  fullName?: string | null;
  email?: string | null;
  phone?: string | null;
  createdAt?: string | null;
};

export type BulkImportEmailRowPayload = {
  rowNumber: number;
  customerName: string;
  email: string;
  memberId: string;
};

export type BulkImportEmailLookupRowPayload = {
  rowNumber: number;
  customerName?: string | null;
  email?: string | null;
  phone?: string | null;
};

export type BulkImportRowResult = {
  rowNumber: number;
  customerName?: string | null;
  email?: string | null;
  mobile?: string | null;
  status: string;
  reason?: string | null;
  memberId?: string | null;
  emailSent: boolean;
};

export type BulkImportBatchSummary = {
  imported: number;
  skipped: number;
  emailsSent: number;
  emailFailed: number;
  withoutEmail: number;
  withoutMobile: number;
  duplicateMembers: number;
  invalidRows: number;
};

export type BulkImportBatchResponse = {
  success: boolean;
  message?: string;
  results?: BulkImportRowResult[];
  summary?: BulkImportBatchSummary;
};

@Injectable({ providedIn: 'root' })
export class BulkMemberImportService {
  private readonly http = inject(HttpClient);

  processBatch(body: {
    rows: BulkImportRowPayload[];
    importId?: string;
    batchIndex?: number;
    totalBatches?: number;
    totalRecords?: number;
    sendWelcomeEmails?: boolean;
  }): Observable<BulkImportBatchResponse> {
    return this.http.post<BulkImportBatchResponse>(
      apiUrl('/api/admin/bulk-member-import/process-batch'),
      body,
    );
  }

  sendEmailsBatch(body: {
    rows: BulkImportEmailRowPayload[];
    importId?: string;
    batchIndex?: number;
    totalBatches?: number;
  }): Observable<BulkImportBatchResponse> {
    return this.http.post<BulkImportBatchResponse>(
      apiUrl('/api/admin/bulk-member-import/send-emails-batch'),
      body,
    );
  }

  sendEmailsLookupBatch(body: {
    rows: BulkImportEmailLookupRowPayload[];
    importId?: string;
    batchIndex?: number;
    totalBatches?: number;
  }): Observable<BulkImportBatchResponse> {
    return this.http.post<BulkImportBatchResponse>(
      apiUrl('/api/admin/bulk-member-import/send-emails-lookup-batch'),
      body,
    );
  }
}

/** Normalize API row payloads (camelCase or PascalCase). */
export function normalizeImportRow(row: Record<string, unknown>): BulkImportRowResult {
  return {
    rowNumber: Number(row['rowNumber'] ?? row['RowNumber'] ?? 0),
    customerName: String(row['customerName'] ?? row['CustomerName'] ?? ''),
    email: String(row['email'] ?? row['Email'] ?? ''),
    mobile: String(row['mobile'] ?? row['Mobile'] ?? ''),
    status: String(row['status'] ?? row['Status'] ?? ''),
    reason: (row['reason'] ?? row['Reason'] ?? null) as string | null,
    memberId: (row['memberId'] ?? row['MemberId'] ?? null) as string | null,
    emailSent: Boolean(row['emailSent'] ?? row['EmailSent'] ?? false),
  };
}

export function normalizeImportSummary(
  summary?: Record<string, unknown> | BulkImportBatchSummary | null,
): BulkImportBatchSummary | null {
  if (!summary) return null;
  const s = summary as Record<string, unknown>;
  return {
    imported: Number(s['imported'] ?? s['Imported'] ?? 0),
    skipped: Number(s['skipped'] ?? s['Skipped'] ?? 0),
    emailsSent: Number(s['emailsSent'] ?? s['EmailsSent'] ?? 0),
    emailFailed: Number(s['emailFailed'] ?? s['EmailFailed'] ?? 0),
    withoutEmail: Number(s['withoutEmail'] ?? s['WithoutEmail'] ?? 0),
    withoutMobile: Number(s['withoutMobile'] ?? s['WithoutMobile'] ?? 0),
    duplicateMembers: Number(s['duplicateMembers'] ?? s['DuplicateMembers'] ?? 0),
    invalidRows: Number(s['invalidRows'] ?? s['InvalidRows'] ?? 0),
  };
}
