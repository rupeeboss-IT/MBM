import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import { appendAdminListParams, type AdminListQueryOpts } from '../utils/admin-list-params';

export type ConnectAdminListItem = {
  listingId: string;
  userId: string;
  role: string;
  fullName: string;
  companyName?: string | null;
  phone: string;
  email: string;
  isListed: boolean;
  isVerified: boolean;
  isActive: boolean;
  sector?: string | null;
  city?: string | null;
  state?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type ConnectMemberProfileReadOnly = {
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
  updatedAt?: string | null;
};

export type ConnectAdminDetail = {
  listingId: string;
  userId: string;
  role: string;
  fullName: string;
  companyName?: string | null;
  phone: string;
  email: string;
  isListed: boolean;
  isVerified: boolean;
  isActive: boolean;
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
  remarks?: string | null;
  memberProfile?: ConnectMemberProfileReadOnly | null;
  customerLockedFields: string[];
  createdAt: string;
  updatedAt: string;
};

export type ConnectUserSearchItem = {
  userId: string;
  role: string;
  fullName: string;
  companyName?: string | null;
  phone: string;
  email: string;
  hasListing: boolean;
};

export type ConnectListingFormBody = {
  userId?: string;
  isListed: boolean;
  isVerified: boolean;
  isActive: boolean;
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
  remarks?: string | null;
};

@Injectable({ providedIn: 'root' })
export class ConnectManagementService {
  private readonly http = inject(HttpClient);

  list(opts: AdminListQueryOpts & { role?: string; status?: string }): Observable<{
    success: boolean;
    listings?: ConnectAdminListItem[];
    total?: number;
    page?: number;
    pageSize?: number;
    message?: string;
  }> {
    const params = appendAdminListParams({}, opts);
    if (opts.role) params['role'] = opts.role;
    if (opts.status) params['status'] = opts.status;
    return this.http.get<{ success: boolean; listings?: ConnectAdminListItem[]; total?: number; page?: number; pageSize?: number; message?: string }>(
      apiUrl('/api/admin/connect-management/listings'),
      { params },
    );
  }

  get(listingId: string): Observable<{ success: boolean; listing?: ConnectAdminDetail; message?: string }> {
    return this.http.get<{ success: boolean; listing?: ConnectAdminDetail; message?: string }>(
      apiUrl(`/api/admin/connect-management/listings/${listingId}`),
    );
  }

  create(body: ConnectListingFormBody & { userId: string }): Observable<{ success: boolean; message?: string; listingId?: string }> {
    return this.http.post<{ success: boolean; message?: string; listingId?: string }>(
      apiUrl('/api/admin/connect-management/listings'),
      body,
    );
  }

  update(listingId: string, body: Omit<ConnectListingFormBody, 'userId'>): Observable<{ success: boolean; message?: string }> {
    return this.http.put<{ success: boolean; message?: string }>(
      apiUrl(`/api/admin/connect-management/listings/${listingId}`),
      body,
    );
  }

  searchUsers(search: string, role?: string, limit = 20): Observable<{ success: boolean; users?: ConnectUserSearchItem[]; message?: string }> {
    const params: Record<string, string> = { limit: String(limit) };
    if (search) params['search'] = search;
    if (role) params['role'] = role;
    return this.http.get<{ success: boolean; users?: ConnectUserSearchItem[]; message?: string }>(
      apiUrl('/api/admin/connect-management/users/search'),
      { params },
    );
  }
}
