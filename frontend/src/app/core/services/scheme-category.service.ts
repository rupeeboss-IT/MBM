import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface SchemeCategoryItem {
  schemeCategoryId: number;
  slug: string;
  name: string;
  shortDescription?: string | null;
  iconUrl?: string | null;
  sortOrder: number;
  isActive: boolean;
  showInFilter: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface SchemeCategoryListResponse {
  success: boolean;
  message?: string;
  categories?: SchemeCategoryItem[];
}

export interface SchemeCategoryDetailResponse {
  success: boolean;
  message?: string;
  category?: SchemeCategoryItem;
}

export interface UpsertSchemeCategoryRequest {
  slug: string;
  name: string;
  shortDescription?: string | null;
  iconUrl?: string | null;
  sortOrder: number;
  isActive: boolean;
  showInFilter: boolean;
}

export interface SchemeCategoryMutationResponse {
  success: boolean;
  message?: string;
  schemeCategoryId?: number;
}

@Injectable({ providedIn: 'root' })
export class SchemeCategoryService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/scheme-categories';
  private readonly adminBase = '/api/scheme-categories/admin';

  listPublic(filtersOnly = false): Observable<SchemeCategoryListResponse> {
    const params = filtersOnly ? { filtersOnly: 'true' } : undefined;
    return this.http.get<SchemeCategoryListResponse>(apiUrl(this.base), { params });
  }

  adminList(): Observable<SchemeCategoryListResponse> {
    return this.http.get<SchemeCategoryListResponse>(apiUrl(this.adminBase));
  }

  adminGet(id: number): Observable<SchemeCategoryDetailResponse> {
    return this.http.get<SchemeCategoryDetailResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  create(req: UpsertSchemeCategoryRequest): Observable<SchemeCategoryMutationResponse> {
    return this.http.post<SchemeCategoryMutationResponse>(apiUrl(this.adminBase), req);
  }

  update(id: number, req: UpsertSchemeCategoryRequest): Observable<SchemeCategoryMutationResponse> {
    return this.http.put<SchemeCategoryMutationResponse>(apiUrl(`${this.adminBase}/${id}`), req);
  }

  delete(id: number): Observable<SchemeCategoryMutationResponse> {
    return this.http.delete<SchemeCategoryMutationResponse>(apiUrl(`${this.adminBase}/${id}`));
  }
}
