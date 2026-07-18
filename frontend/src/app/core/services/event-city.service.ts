import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface EventCityItem {
  eventCityId: number;
  slug: string;
  name: string;
  badgeClass?: string | null;
  sortOrder: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface EventCityListResponse {
  success: boolean;
  message?: string;
  cities?: EventCityItem[];
}

export interface EventCityDetailResponse {
  success: boolean;
  message?: string;
  city?: EventCityItem;
}

export interface UpsertEventCityRequest {
  slug: string;
  name: string;
  badgeClass?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface EventCityMutationResponse {
  success: boolean;
  message?: string;
  eventCityId?: number;
}

@Injectable({ providedIn: 'root' })
export class EventCityService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/event-cities';
  private readonly adminBase = '/api/event-cities/admin';

  listPublic(): Observable<EventCityListResponse> {
    return this.http.get<EventCityListResponse>(apiUrl(this.base));
  }

  adminList(): Observable<EventCityListResponse> {
    return this.http.get<EventCityListResponse>(apiUrl(this.adminBase));
  }

  adminGet(id: number): Observable<EventCityDetailResponse> {
    return this.http.get<EventCityDetailResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  create(req: UpsertEventCityRequest): Observable<EventCityMutationResponse> {
    return this.http.post<EventCityMutationResponse>(apiUrl(this.adminBase), req);
  }

  update(id: number, req: UpsertEventCityRequest): Observable<EventCityMutationResponse> {
    return this.http.put<EventCityMutationResponse>(apiUrl(`${this.adminBase}/${id}`), req);
  }

  delete(id: number): Observable<EventCityMutationResponse> {
    return this.http.delete<EventCityMutationResponse>(apiUrl(`${this.adminBase}/${id}`));
  }
}
