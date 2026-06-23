import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export type ConnectAccessTier = 'none' | 'limited' | 'unlimited';

export type ConnectAccessSummary = {
  tier: ConnectAccessTier;
  unlocksUsed: number;
  unlocksLimit?: number | null;
  canUnlockMore: boolean;
  planCode?: string | null;
  planName?: string | null;
};

export type ConnectStats = {
  listedCount: number;
  sectorCount: number;
  stateCount: number;
};

export type ConnectConnectionStatus = 'none' | 'pending' | 'connected' | 'rejected';

export type ConnectProfileCard = {
  userId: string;
  isLocked: boolean;
  displayName?: string | null;
  companyName?: string | null;
  initials?: string | null;
  businessType?: string | null;
  sector?: string | null;
  state?: string | null;
  city?: string | null;
  turnover?: string | null;
  description?: string | null;
  verified: boolean;
  connectionStatus: ConnectConnectionStatus;
  canConnect: boolean;
  contactVisible: boolean;
};

export type ConnectProfileDetail = ConnectProfileCard & {
  designation?: string | null;
  udyam?: string | null;
  employees?: string | null;
  website?: string | null;
  established?: string | null;
  socialLinksJson?: string | null;
  phone?: string | null;
  email?: string | null;
  connectionMessage?: string | null;
};

export type ConnectSearchRes = {
  success: boolean;
  message?: string;
  profiles?: ConnectProfileCard[];
  total?: number;
  page?: number;
  pageSize?: number;
  stats?: ConnectStats;
  access?: ConnectAccessSummary;
};

export type ConnectProfileDetailRes = {
  success: boolean;
  message?: string;
  profile?: ConnectProfileDetail;
  access?: ConnectAccessSummary;
};

export type ConnectActionRes = {
  success: boolean;
  message?: string;
  status?: string;
};

export type ConnectIncomingItem = {
  requestId: string;
  fromUserId: string;
  fromDisplayName?: string | null;
  fromCompanyName?: string | null;
  message?: string | null;
  createdAt: string;
};

export type ConnectIncomingRes = {
  success: boolean;
  message?: string;
  items?: ConnectIncomingItem[];
};

export type UpdateConnectMemberProfileBody = {
  designation?: string | null;
  businessType?: string | null;
  sector?: string | null;
  state?: string | null;
  city?: string | null;
  turnover?: string | null;
  udyam?: string | null;
  employees?: string | null;
  description?: string | null;
  website?: string | null;
  established?: string | null;
  socialLinksJson?: string | null;
};

@Injectable({ providedIn: 'root' })
export class ConnectService {
  private readonly http = inject(HttpClient);

  search(opts: {
    page?: number;
    pageSize?: number;
    search?: string;
    sector?: string;
    state?: string;
    turnover?: string;
  }): Observable<ConnectSearchRes> {
    const params: Record<string, string> = {};
    if (opts.page) params['page'] = String(opts.page);
    if (opts.pageSize) params['pageSize'] = String(opts.pageSize);
    if (opts.search) params['search'] = opts.search;
    if (opts.sector) params['sector'] = opts.sector;
    if (opts.state) params['state'] = opts.state;
    if (opts.turnover) params['turnover'] = opts.turnover;
    return this.http.get<ConnectSearchRes>(apiUrl('/api/connect/profiles'), { params });
  }

  getProfile(userId: string): Observable<ConnectProfileDetailRes> {
    return this.http.get<ConnectProfileDetailRes>(apiUrl(`/api/connect/profiles/${userId}`));
  }

  getAccess(): Observable<{ success: boolean; access?: ConnectAccessSummary }> {
    return this.http.get<{ success: boolean; access?: ConnectAccessSummary }>(apiUrl('/api/connect/access'));
  }

  sendRequest(userId: string, message?: string): Observable<ConnectActionRes> {
    return this.http.post<ConnectActionRes>(apiUrl(`/api/connect/profiles/${userId}/request`), { message });
  }

  acceptRequest(requestId: string): Observable<ConnectActionRes> {
    return this.http.post<ConnectActionRes>(apiUrl(`/api/connect/requests/${requestId}/accept`), {});
  }

  rejectRequest(requestId: string): Observable<ConnectActionRes> {
    return this.http.post<ConnectActionRes>(apiUrl(`/api/connect/requests/${requestId}/reject`), {});
  }

  incoming(): Observable<ConnectIncomingRes> {
    return this.http.get<ConnectIncomingRes>(apiUrl('/api/connect/requests/incoming'));
  }

  getMyProfile(): Observable<ConnectProfileDetailRes> {
    return this.http.get<ConnectProfileDetailRes>(apiUrl('/api/connect/my-profile'));
  }

  updateMyProfile(body: UpdateConnectMemberProfileBody): Observable<ConnectProfileDetailRes> {
    return this.http.put<ConnectProfileDetailRes>(apiUrl('/api/connect/my-profile'), body);
  }

  static display(value: string | null | undefined): string {
    const s = (value ?? '').trim();
    return s.length ? s : '-';
  }

  static connectButtonLabel(status: ConnectConnectionStatus, hasPlan: boolean): string {
    if (!hasPlan) return '🔒 Connect';
    switch (status) {
      case 'pending':
        return 'Request Pending';
      case 'connected':
        return 'Connected';
      case 'rejected':
        return 'Request Rejected';
      default:
        return 'Connect';
    }
  }
}
