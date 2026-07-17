import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface BlogListItem {
  blogId: number;
  slug: string;
  title: string;
  crumb: string;
  meta: string;
  category: string;
  badgeSlug: string;
  dateLabel: string;
  summary: string;
  badgeText: string;
  badgeClass: string;
  cardIcon: string;
  cardClass: string;
  imageUrl?: string | null;
  seoTitle?: string | null;
  metaDescription?: string | null;
  isPublished: boolean;
  createdAt: string;
}

export interface BlogDetailItem extends BlogListItem {
  content: string;
  updatedAt: string;
}

export interface BlogListResponse {
  success: boolean;
  message?: string;
  blogs?: BlogListItem[];
  total?: number;
  page?: number;
  pageSize?: number;
}

export interface BlogDetailResponse {
  success: boolean;
  message?: string;
  blog?: BlogDetailItem;
}

export interface MutationResponse {
  success: boolean;
  message?: string;
  blogId?: number;
}

export interface ImageUploadResponse {
  success: boolean;
  message?: string;
  imageUrl?: string;
}

export interface CreateBlogRequest {
  slug: string;
  title: string;
  crumb: string;
  meta: string;
  content: string;
  category: string;
  badgeSlug: string;
  dateLabel: string;
  summary: string;
  imageUrl?: string | null;
  seoTitle?: string | null;
  metaDescription?: string | null;
  isPublished: boolean;
}

export type UpdateBlogRequest = Omit<CreateBlogRequest, 'slug'>;

@Injectable({ providedIn: 'root' })
export class BlogManagementService {
  private readonly http = inject(HttpClient);
  private readonly adminBase = '/api/blogs/admin';
  private readonly publicBase = '/api/blogs';

  /** Admin: list all blogs (published + drafts) */
  adminList(opts?: {
    search?: string;
    category?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<BlogListResponse> {
    const params: Record<string, string | number> = {};
    if (opts?.search?.trim()) params['search'] = opts.search.trim();
    if (opts?.category?.trim()) params['category'] = opts.category.trim();
    if (opts?.status?.trim()) params['status'] = opts.status.trim();
    if (opts?.page) params['page'] = opts.page;
    if (opts?.pageSize) params['pageSize'] = opts.pageSize;
    return this.http.get<BlogListResponse>(apiUrl(this.adminBase), { params });
  }

  /** Admin: get single blog by id */
  adminGet(blogId: number): Observable<BlogDetailResponse> {
    return this.http.get<BlogDetailResponse>(apiUrl(`${this.adminBase}/${blogId}`));
  }

  /** Admin: create blog */
  create(req: CreateBlogRequest): Observable<MutationResponse> {
    return this.http.post<MutationResponse>(apiUrl(this.adminBase), req);
  }

  /** Admin: update blog */
  update(blogId: number, req: UpdateBlogRequest): Observable<MutationResponse> {
    return this.http.put<MutationResponse>(apiUrl(`${this.adminBase}/${blogId}`), req);
  }

  /** Admin: toggle publish */
  setPublished(blogId: number, isPublished: boolean): Observable<MutationResponse> {
    return this.http.patch<MutationResponse>(apiUrl(`${this.adminBase}/${blogId}/publish`), { isPublished });
  }

  /** Admin: delete */
  delete(blogId: number): Observable<MutationResponse> {
    return this.http.delete<MutationResponse>(apiUrl(`${this.adminBase}/${blogId}`));
  }

  /** Admin: upload cover image */
  uploadImage(file: File): Observable<ImageUploadResponse> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ImageUploadResponse>(apiUrl(`${this.adminBase}/upload-image`), form);
  }

  /** Public: list published blogs */
  listPublished(opts?: { category?: string; page?: number; pageSize?: number }): Observable<BlogListResponse> {
    const params: Record<string, string | number> = {};
    if (opts?.category?.trim()) params['category'] = opts.category.trim();
    if (opts?.page) params['page'] = opts.page;
    if (opts?.pageSize) params['pageSize'] = opts.pageSize;
    return this.http.get<BlogListResponse>(apiUrl(this.publicBase), { params });
  }

  /** Public: get by slug */
  getBySlug(slug: string): Observable<BlogDetailResponse> {
    return this.http.get<BlogDetailResponse>(apiUrl(`${this.publicBase}/${slug}`));
  }
}
