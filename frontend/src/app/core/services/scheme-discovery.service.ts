import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export type SchemeDiscoveryPhase =
  | 'no_membership'
  | 'plan_choice'
  | 'report_exists'
  | 'udyam_form'
  | 'awaiting_payment'
  | 'request_pending';

export type SchemeDiscoveryProfile = {
  fullName: string;
  email: string;
  phone: string;
  memberId: string;
};

export type SchemeDiscoveryMembership = {
  userPlanId: string;
  planCode: string;
  planName: string;
  activeFrom: string;
  activeTo?: string | null;
};

export type SchemeDiscoveryExistingReport = {
  reportId: string;
  reportName: string;
  uploadDate?: string | null;
};

export type SchemeDiscoveryStatus = {
  success: boolean;
  phase: string;
  message?: string | null;
  profile?: SchemeDiscoveryProfile | null;
  membership?: SchemeDiscoveryMembership | null;
  isPremiumOrPro: boolean;
  hasOneTimeEntitlement: boolean;
  existingReport?: SchemeDiscoveryExistingReport | null;
  pendingRequestStatus?: string | null;
  draftRequestId?: string | null;
  savedUdyam?: string | null;
  pendingRequestId?: string | null;
};

export type SchemeDiscoveryFinalizeOutcome = 'generated' | 'duplicate' | 'generation_failed';

export type SchemeDiscoverySubmitRes = {
  success: boolean;
  message?: string | null;
  requestId?: string | null;
  status?: string | null;
  outcome?: SchemeDiscoveryFinalizeOutcome | string | null;
  reportId?: string | null;
  expiryDate?: string | null;
};

@Injectable({ providedIn: 'root' })
export class SchemeDiscoveryApi {
  private readonly http = inject(HttpClient);

  getStatus(): Observable<SchemeDiscoveryStatus> {
    return this.http.get<SchemeDiscoveryStatus>(apiUrl('/api/scheme-discovery/status'));
  }

  saveDraft(udyamNumber: string): Observable<SchemeDiscoverySubmitRes> {
    return this.http.post<SchemeDiscoverySubmitRes>(apiUrl('/api/scheme-discovery/draft'), {
      udyamNumber,
    });
  }

  finalizeRequest(requestId: string): Observable<SchemeDiscoverySubmitRes> {
    return this.http.post<SchemeDiscoverySubmitRes>(apiUrl('/api/scheme-discovery/finalize'), {
      requestId,
    });
  }

  submitRequest(udyamNumber: string): Observable<SchemeDiscoverySubmitRes> {
    return this.http.post<SchemeDiscoverySubmitRes>(apiUrl('/api/scheme-discovery/request'), {
      udyamNumber,
    });
  }

  emailReport(reportId: string): Observable<{ success: boolean; message?: string | null }> {
    return this.http.post<{ success: boolean; message?: string | null }>(
      apiUrl('/api/scheme-discovery/email-report'),
      { reportId },
    );
  }
}
