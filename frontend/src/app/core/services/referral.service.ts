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
  referralType?: 'Employee' | 'RBA';
  displayName?: string;
  brokerId?: number;
  inactive?: boolean;
}

/** UI display: referrer name only, without role/type suffixes such as (RBA) or (Employee). */
export function referrerDisplayName(
  res: Pick<ValidateReferralRes, 'displayName' | 'employeeName'> | null | undefined,
): string | null {
  const raw = (res?.displayName ?? res?.employeeName ?? '').trim();
  if (!raw) return null;
  const name = raw.replace(/\s*\([^)]*\)\s*$/, '').trim();
  return name || null;
}

@Injectable({ providedIn: 'root' })
export class ReferralService {
  private readonly http = inject(HttpClient);

  validate(req: ValidateReferralReq): Observable<ValidateReferralRes> {
    return this.http.post<ValidateReferralRes>(apiUrl('/api/referral/validate'), req);
  }
}
