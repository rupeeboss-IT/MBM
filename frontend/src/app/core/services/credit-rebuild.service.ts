import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface SubmitCreditRebuildEnquiryRequest {
  fullName: string;
  mobile: string;
  email: string;
  consentAccepted: boolean;
  advisorCode?: string | null;
  recaptchaToken?: string;
}

export interface SubmitCreditRebuildEnquiryResponse {
  success: boolean;
  message?: string | null;
  leadId?: number | null;
}

@Injectable({ providedIn: 'root' })
export class CreditRebuildService {
  private readonly http = inject(HttpClient);

  submitEnquiry(req: SubmitCreditRebuildEnquiryRequest): Observable<SubmitCreditRebuildEnquiryResponse> {
    return this.http.post<SubmitCreditRebuildEnquiryResponse>(apiUrl('/api/credit-rebuild/enquire'), req);
  }
}
