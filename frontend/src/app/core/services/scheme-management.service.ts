import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface SchemeBenefitItem {
  schemeBenefitId?: number | null;
  text: string;
  sortOrder: number;
}

export interface SchemeCardHighlightItem {
  schemeCardHighlightId?: number | null;
  text: string;
  sortOrder: number;
}

export interface SchemeListItem {
  schemeId: number;
  slug: string;
  name: string;
  crumb: string;
  tagline: string;
  shortDescription: string;
  categorySlug: string;
  categoryName?: string | null;
  primaryBadgeText: string;
  primaryBadgeClass: string;
  secondaryBadgeText: string;
  secondaryBadgeClass: string;
  homeTitle: string;
  homeBadgeText: string;
  homeBadgeClass: string;
  homeDescription: string;
  isPublished: boolean;
  isFeatured: boolean;
  sortOrder: number;
  createdAt: string;
  cardHighlights: SchemeCardHighlightItem[];
}

export interface SchemeDetailItem extends SchemeListItem {
  contentHtml: string;
  seoTitle?: string | null;
  metaDescription?: string | null;
  updatedAt: string;
  benefits: SchemeBenefitItem[];
}

export interface SchemeListResponse {
  success: boolean;
  message?: string;
  schemes?: SchemeListItem[];
  total?: number;
  page?: number;
  pageSize?: number;
}

export interface SchemeDetailResponse {
  success: boolean;
  message?: string;
  scheme?: SchemeDetailItem;
}

export interface UpsertSchemeRequest {
  slug: string;
  name: string;
  crumb: string;
  tagline: string;
  shortDescription: string;
  contentHtml: string;
  categorySlug: string;
  primaryBadgeText: string;
  primaryBadgeClass: string;
  secondaryBadgeText: string;
  secondaryBadgeClass: string;
  homeTitle: string;
  homeBadgeText: string;
  homeBadgeClass: string;
  homeDescription: string;
  seoTitle?: string | null;
  metaDescription?: string | null;
  isPublished: boolean;
  isFeatured: boolean;
  sortOrder: number;
  benefits?: SchemeBenefitItem[];
  cardHighlights?: SchemeCardHighlightItem[];
}

export interface SchemeMutationResponse {
  success: boolean;
  message?: string;
  schemeId?: number;
}

export interface PublicSchemeListParams {
  search?: string;
  category?: string;
  featured?: boolean;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class SchemeManagementService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/schemes';
  private readonly adminBase = '/api/schemes/admin';

  listPublished(params?: PublicSchemeListParams): Observable<SchemeListResponse> {
    let httpParams = new HttpParams();
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.category) httpParams = httpParams.set('category', params.category);
    if (params?.featured != null) httpParams = httpParams.set('featured', String(params.featured));
    if (params?.page) httpParams = httpParams.set('page', String(params.page));
    if (params?.pageSize) httpParams = httpParams.set('pageSize', String(params.pageSize));
    return this.http.get<SchemeListResponse>(apiUrl(this.base), { params: httpParams });
  }

  getPublishedBySlug(slug: string): Observable<SchemeDetailResponse> {
    return this.http.get<SchemeDetailResponse>(apiUrl(`${this.base}/${encodeURIComponent(slug)}`));
  }

  adminList(opts?: {
    search?: string;
    category?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<SchemeListResponse> {
    let params = new HttpParams();
    if (opts?.search) params = params.set('search', opts.search);
    if (opts?.category) params = params.set('category', opts.category);
    if (opts?.status) params = params.set('status', opts.status);
    if (opts?.page) params = params.set('page', String(opts.page));
    if (opts?.pageSize) params = params.set('pageSize', String(opts.pageSize));
    return this.http.get<SchemeListResponse>(apiUrl(this.adminBase), { params });
  }

  adminGet(id: number): Observable<SchemeDetailResponse> {
    return this.http.get<SchemeDetailResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  create(req: UpsertSchemeRequest): Observable<SchemeMutationResponse> {
    return this.http.post<SchemeMutationResponse>(apiUrl(this.adminBase), req);
  }

  update(id: number, req: UpsertSchemeRequest): Observable<SchemeMutationResponse> {
    return this.http.put<SchemeMutationResponse>(apiUrl(`${this.adminBase}/${id}`), req);
  }

  setPublished(id: number, isPublished: boolean): Observable<SchemeMutationResponse> {
    return this.http.patch<SchemeMutationResponse>(apiUrl(`${this.adminBase}/${id}/publish`), {
      isPublished,
    });
  }

  delete(id: number): Observable<SchemeMutationResponse> {
    return this.http.delete<SchemeMutationResponse>(apiUrl(`${this.adminBase}/${id}`));
  }
}
