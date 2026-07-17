import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface BlogBadgeItem {
  blogBadgeId: number;
  slug: string;
  label: string;
  badgeText: string;
  badgeClass: string;
  cardIcon: string;
  cardClass: string;
  sortOrder: number;
  isActive: boolean;
}

export interface BlogBadgeListResponse {
  success: boolean;
  message?: string;
  badges?: BlogBadgeItem[];
}

export interface BlogBadgeDetailResponse {
  success: boolean;
  message?: string;
  badge?: BlogBadgeItem;
}

export interface UpsertBlogBadgeRequest {
  slug: string;
  label: string;
  badgeText: string;
  badgeClass: string;
  cardIcon: string;
  cardClass: string;
  sortOrder: number;
  isActive: boolean;
}

export interface BlogBadgeMutationResponse {
  success: boolean;
  message?: string;
  blogBadgeId?: number;
}

@Injectable({ providedIn: 'root' })
export class BlogBadgeService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/blog-badges';
  private readonly adminBase = '/api/blog-badges/admin';

  listPublic(): Observable<BlogBadgeListResponse> {
    return this.http.get<BlogBadgeListResponse>(apiUrl(this.base));
  }

  adminList(): Observable<BlogBadgeListResponse> {
    return this.http.get<BlogBadgeListResponse>(apiUrl(this.adminBase));
  }

  adminGet(id: number): Observable<BlogBadgeDetailResponse> {
    return this.http.get<BlogBadgeDetailResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  create(req: UpsertBlogBadgeRequest): Observable<BlogBadgeMutationResponse> {
    return this.http.post<BlogBadgeMutationResponse>(apiUrl(this.adminBase), req);
  }

  update(id: number, req: UpsertBlogBadgeRequest): Observable<BlogBadgeMutationResponse> {
    return this.http.put<BlogBadgeMutationResponse>(apiUrl(`${this.adminBase}/${id}`), req);
  }

  delete(id: number): Observable<BlogBadgeMutationResponse> {
    return this.http.delete<BlogBadgeMutationResponse>(apiUrl(`${this.adminBase}/${id}`));
  }
}
