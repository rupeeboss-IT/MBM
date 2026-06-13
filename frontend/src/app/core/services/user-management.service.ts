import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import { appendAdminListParams, type AdminListQueryOpts } from '../utils/admin-list-params';

export type ManagedUser = {
  userId: string;
  role: string;
  fullName: string;
  email: string;
  phone: string;
  companyName?: string | null;
  memberId?: string | null;
  isActive: boolean;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
  createdByUserId?: string | null;
  createdByName?: string | null;
};

export type ManagedUserDetail = ManagedUser & {
  deletedAt?: string | null;
  planCode?: string | null;
  planName?: string | null;
};

export type UserManagementStats = {
  totalAdmins: number;
  activeAdmins: number;
  inactiveAdmins: number;
  totalPartners: number;
  activePartners: number;
  inactivePartners: number;
  totalMembers: number;
  activeMembers: number;
  inactiveMembers: number;
};

export type UserAuditItem = {
  id: string;
  userId: string;
  userType: string;
  action: string;
  performedByUserId: string;
  performedByName?: string | null;
  performedOn: string;
  previousValues?: string | null;
  newValues?: string | null;
  remarks?: string | null;
};

export type UserStatusHistoryItem = {
  id: string;
  userId: string;
  userType: string;
  actionType: string;
  oldStatus?: string | null;
  newStatus?: string | null;
  remarks?: string | null;
  performedByUserId: string;
  performedByName?: string | null;
  performedOn: string;
};

export type UserManagementRole = 'admins' | 'partners' | 'members';

@Injectable({ providedIn: 'root' })
export class UserManagementService {
  private readonly http = inject(HttpClient);

  stats(): Observable<{ success: boolean; message?: string; stats?: UserManagementStats }> {
    return this.http.get<{ success: boolean; message?: string; stats?: UserManagementStats }>(
      apiUrl('/api/admin/user-management/stats'),
    );
  }

  list(role: UserManagementRole, search: string, opts?: AdminListQueryOpts & { status?: string }): Observable<any> {
    const params = appendAdminListParams({}, opts);
    if (search.trim()) params['search'] = search.trim();
    if (opts?.status) params['status'] = opts.status;
    return this.http.get(apiUrl(`/api/admin/user-management/${role}`), { params });
  }

  getUser(userId: string): Observable<any> {
    return this.http.get(apiUrl(`/api/admin/user-management/users/${encodeURIComponent(userId)}`));
  }

  createAdmin(body: CreateUserBody): Observable<any> {
    return this.http.post(apiUrl('/api/admin/user-management/admins'), body);
  }

  createPartner(body: CreateUserBody): Observable<any> {
    return this.http.post(apiUrl('/api/admin/user-management/partners'), body);
  }

  createMember(body: CreateUserBody): Observable<any> {
    return this.http.post(apiUrl('/api/admin/user-management/members'), body);
  }

  updateUser(userId: string, body: UpdateUserBody): Observable<any> {
    return this.http.put(apiUrl(`/api/admin/user-management/users/${encodeURIComponent(userId)}`), body);
  }

  setActive(userId: string, isActive: boolean, remarks?: string): Observable<any> {
    return this.http.patch(apiUrl(`/api/admin/user-management/users/${encodeURIComponent(userId)}/active`), {
      isActive,
      remarks,
    });
  }

  deleteUser(userId: string, remarks?: string): Observable<any> {
    return this.http.delete(apiUrl(`/api/admin/user-management/users/${encodeURIComponent(userId)}`), {
      body: { remarks },
    });
  }

  audit(userId: string, page: number, pageSize: number): Observable<any> {
    return this.http.get(apiUrl(`/api/admin/user-management/users/${encodeURIComponent(userId)}/audit`), {
      params: { page: String(page), pageSize: String(pageSize) },
    });
  }

  statusHistory(userId: string, page: number, pageSize: number): Observable<any> {
    return this.http.get(
      apiUrl(`/api/admin/user-management/users/${encodeURIComponent(userId)}/status-history`),
      { params: { page: String(page), pageSize: String(pageSize) } },
    );
  }
}

export type CreateUserBody = {
  fullName: string;
  email: string;
  phone: string;
  password: string;
  companyName?: string | null;
};

export type UpdateUserBody = {
  fullName: string;
  email: string;
  phone: string;
  companyName?: string | null;
  password?: string | null;
};
