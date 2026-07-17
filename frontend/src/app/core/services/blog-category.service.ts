import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface BlogCategoryItem {
  blogCategoryId: number;
  slug: string;
  label: string;
  sortOrder: number;
  isActive: boolean;
  showInFilter: boolean;
}

export interface BlogCategoryListResponse {
  success: boolean;
  message?: string;
  categories?: BlogCategoryItem[];
}

export interface BlogCategoryDetailResponse {
  success: boolean;
  message?: string;
  category?: BlogCategoryItem;
}

export interface UpsertBlogCategoryRequest {
  slug: string;
  label: string;
  sortOrder: number;
  isActive: boolean;
  showInFilter: boolean;
}

export interface BlogCategoryMutationResponse {
  success: boolean;
  message?: string;
  blogCategoryId?: number;
}

@Injectable({ providedIn: 'root' })
export class BlogCategoryService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/blog-categories';
  private readonly adminBase = '/api/blog-categories/admin';

  listPublic(filtersOnly = false): Observable<BlogCategoryListResponse> {
    const params = filtersOnly ? { filtersOnly: 'true' } : undefined;
    return this.http.get<BlogCategoryListResponse>(apiUrl(this.base), { params });
  }

  adminList(): Observable<BlogCategoryListResponse> {
    return this.http.get<BlogCategoryListResponse>(apiUrl(this.adminBase));
  }

  adminGet(id: number): Observable<BlogCategoryDetailResponse> {
    return this.http.get<BlogCategoryDetailResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  create(req: UpsertBlogCategoryRequest): Observable<BlogCategoryMutationResponse> {
    return this.http.post<BlogCategoryMutationResponse>(apiUrl(this.adminBase), req);
  }

  update(id: number, req: UpsertBlogCategoryRequest): Observable<BlogCategoryMutationResponse> {
    return this.http.put<BlogCategoryMutationResponse>(apiUrl(`${this.adminBase}/${id}`), req);
  }

  delete(id: number): Observable<BlogCategoryMutationResponse> {
    return this.http.delete<BlogCategoryMutationResponse>(apiUrl(`${this.adminBase}/${id}`));
  }
}
