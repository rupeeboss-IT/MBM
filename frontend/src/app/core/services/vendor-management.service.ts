import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import { appendAdminListParams, type AdminListQueryOpts } from '../utils/admin-list-params';

export type VendorPlan = {
  planId: number;
  code: string;
  name: string;
};

export type VendorListItem = {
  vendorId: string;
  serviceName: string;
  companyName: string;
  contactPersonName: string;
  mobile: string;
  alternateMobile?: string | null;
  email: string;
  alternateEmail?: string | null;
  assignedPlanNames: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type VendorDetail = {
  vendorId: string;
  serviceName: string;
  companyName: string;
  contactPersonName: string;
  mobile: string;
  alternateMobile?: string | null;
  email: string;
  alternateEmail?: string | null;
  website?: string | null;
  address?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  pincode?: string | null;
  remarks?: string | null;
  isActive: boolean;
  isDeleted: boolean;
  deletedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  assignedPlans: VendorPlan[];
};

export type VendorManagementStats = {
  totalVendors: number;
  activeVendors: number;
  inactiveVendors: number;
  vendorsAssignedToPlans: number;
};

export type VendorAuditItem = {
  id: string;
  vendorId: string;
  action: string;
  performedByUserId: string;
  performedByName?: string | null;
  performedOn: string;
  previousValues?: string | null;
  newValues?: string | null;
  remarks?: string | null;
};

export type VendorPlanMappingGroup = {
  planId: number;
  planCode: string;
  planName: string;
  vendors: {
    vendorId: string;
    serviceName: string;
    companyName: string;
    isActive: boolean;
  }[];
};

export type VendorFormBody = {
  serviceName: string;
  companyName: string;
  contactPersonName: string;
  mobile: string;
  alternateMobile?: string | null;
  email: string;
  alternateEmail?: string | null;
  website?: string | null;
  address?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  pincode?: string | null;
  remarks?: string | null;
  isActive: boolean;
  planIds?: number[];
};

@Injectable({ providedIn: 'root' })
export class VendorManagementService {
  private readonly http = inject(HttpClient);

  stats(): Observable<{ success: boolean; message?: string; stats?: VendorManagementStats }> {
    return this.http.get<{ success: boolean; message?: string; stats?: VendorManagementStats }>(
      apiUrl('/api/admin/vendor-management/stats'),
    );
  }

  listPlans(): Observable<{ success: boolean; message?: string; plans?: VendorPlan[] }> {
    return this.http.get<{ success: boolean; message?: string; plans?: VendorPlan[] }>(
      apiUrl('/api/admin/vendor-management/plans'),
    );
  }

  listPlanMappings(): Observable<{ success: boolean; message?: string; groups?: VendorPlanMappingGroup[] }> {
    return this.http.get<{ success: boolean; message?: string; groups?: VendorPlanMappingGroup[] }>(
      apiUrl('/api/admin/vendor-management/plan-mappings'),
    );
  }

  list(search: string, opts?: AdminListQueryOpts & { status?: string }): Observable<any> {
    const params = appendAdminListParams({}, opts);
    if (search.trim()) params['search'] = search.trim();
    return this.http.get(apiUrl('/api/admin/vendor-management/vendors'), { params });
  }

  getVendor(vendorId: string): Observable<{ success: boolean; message?: string; vendor?: VendorDetail }> {
    return this.http.get<{ success: boolean; message?: string; vendor?: VendorDetail }>(
      apiUrl(`/api/admin/vendor-management/vendors/${vendorId}`),
    );
  }

  create(body: VendorFormBody): Observable<{ success: boolean; message?: string; vendorId?: string }> {
    return this.http.post<{ success: boolean; message?: string; vendorId?: string }>(
      apiUrl('/api/admin/vendor-management/vendors'),
      body,
    );
  }

  update(
    vendorId: string,
    body: VendorFormBody,
  ): Observable<{ success: boolean; message?: string; vendorId?: string }> {
    return this.http.put<{ success: boolean; message?: string; vendorId?: string }>(
      apiUrl(`/api/admin/vendor-management/vendors/${vendorId}`),
      body,
    );
  }

  setActive(
    vendorId: string,
    isActive: boolean,
    remarks?: string,
  ): Observable<{ success: boolean; message?: string }> {
    return this.http.patch<{ success: boolean; message?: string }>(
      apiUrl(`/api/admin/vendor-management/vendors/${vendorId}/active`),
      { isActive, remarks: remarks ?? null },
    );
  }

  delete(vendorId: string, remarks?: string): Observable<{ success: boolean; message?: string }> {
    return this.http.delete<{ success: boolean; message?: string }>(
      apiUrl(`/api/admin/vendor-management/vendors/${vendorId}`),
      { body: { remarks: remarks ?? null } },
    );
  }

  assignPlans(vendorId: string, planIds: number[]): Observable<{ success: boolean; message?: string }> {
    return this.http.put<{ success: boolean; message?: string }>(
      apiUrl(`/api/admin/vendor-management/vendors/${vendorId}/plans`),
      { planIds },
    );
  }

  audit(
    vendorId: string,
    page = 1,
    pageSize = 10,
  ): Observable<{ success: boolean; message?: string; items?: VendorAuditItem[]; total?: number }> {
    return this.http.get<{ success: boolean; message?: string; items?: VendorAuditItem[]; total?: number }>(
      apiUrl(`/api/admin/vendor-management/vendors/${vendorId}/audit`),
      { params: { page: String(page), pageSize: String(pageSize) } },
    );
  }
}
