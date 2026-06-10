import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface ValidateReferralReq {
  referralCode: string;
}

export interface ValidateReferralRes {
  success: boolean;
  message?: string;
  employeeId?: number;
  employeeName?: string;
  referralCode?: string;
}

@Injectable({ providedIn: 'root' })
export class ReferralService {
  private readonly http = inject(HttpClient);

  validate(req: ValidateReferralReq): Observable<ValidateReferralRes> {
    return this.http.post<ValidateReferralRes>(apiUrl('/api/referral/validate'), req);
  }
}
