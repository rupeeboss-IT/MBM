import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface EventCategoryItem {
  eventCategoryId: number;
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

export interface EventCategoryListResponse {
  success: boolean;
  message?: string;
  categories?: EventCategoryItem[];
}

export interface EventCategoryDetailResponse {
  success: boolean;
  message?: string;
  category?: EventCategoryItem;
}

export interface UpsertEventCategoryRequest {
  slug: string;
  name: string;
  shortDescription?: string | null;
  iconUrl?: string | null;
  sortOrder: number;
  isActive: boolean;
  showInFilter: boolean;
}

export interface EventCategoryMutationResponse {
  success: boolean;
  message?: string;
  eventCategoryId?: number;
}

@Injectable({ providedIn: 'root' })
export class EventCategoryService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/event-categories';
  private readonly adminBase = '/api/event-categories/admin';

  listPublic(filtersOnly = false): Observable<EventCategoryListResponse> {
    const params = filtersOnly ? { filtersOnly: 'true' } : undefined;
    return this.http.get<EventCategoryListResponse>(apiUrl(this.base), { params });
  }

  adminList(): Observable<EventCategoryListResponse> {
    return this.http.get<EventCategoryListResponse>(apiUrl(this.adminBase));
  }

  adminGet(id: number): Observable<EventCategoryDetailResponse> {
    return this.http.get<EventCategoryDetailResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  create(req: UpsertEventCategoryRequest): Observable<EventCategoryMutationResponse> {
    return this.http.post<EventCategoryMutationResponse>(apiUrl(this.adminBase), req);
  }

  update(id: number, req: UpsertEventCategoryRequest): Observable<EventCategoryMutationResponse> {
    return this.http.put<EventCategoryMutationResponse>(apiUrl(`${this.adminBase}/${id}`), req);
  }

  delete(id: number): Observable<EventCategoryMutationResponse> {
    return this.http.delete<EventCategoryMutationResponse>(apiUrl(`${this.adminBase}/${id}`));
  }
}
