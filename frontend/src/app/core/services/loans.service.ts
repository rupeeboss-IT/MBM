import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface SubmitLoanApplicationRequest {
  fullName: string;
  mobile: string;
  email?: string | null;
  pincode: string;
  loanTypeId: number;
  loanAmount: string;
  consentAccepted: boolean;
  referralCode?: string | null;
}

export interface SubmitLoanApplicationResponse {
  success: boolean;
  message?: string | null;
  leadId?: number | null;
}

@Injectable({ providedIn: 'root' })
export class LoansService {
  private readonly http = inject(HttpClient);

  submitApplication(req: SubmitLoanApplicationRequest): Observable<SubmitLoanApplicationResponse> {
    return this.http.post<SubmitLoanApplicationResponse>(apiUrl('/api/loans/apply'), req);
  }
}
