import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface EventHighlightItem {
  eventHighlightId?: number | null;
  text: string;
  sortOrder: number;
}

export interface EventPartnerItem {
  eventPartnerId?: number | null;
  name: string;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  sortOrder: number;
}

export interface EventListItem {
  eventId: number;
  slug: string;
  name: string;
  crumb: string;
  tagline: string;
  shortDescription: string;
  categorySlug: string;
  categoryName?: string | null;
  citySlug?: string | null;
  cityName?: string | null;
  cityBadgeClass?: string | null;
  featuredImageUrl?: string | null;
  dateDisplayText: string;
  timeDisplayText: string;
  locationDisplayText: string;
  venueName: string;
  attendanceMode: string;
  startDate?: string | null;
  endDate?: string | null;
  dateISO?: string | null;
  isPublished: boolean;
  isFeatured: boolean;
  sortOrder: number;
  createdAt: string;
  lifecycle: string;
}

export interface EventDetailItem extends EventListItem {
  aboutHtml: string;
  highlightsHtml: string;
  associationHtml: string;
  bannerImageUrl?: string | null;
  thumbnailUrl?: string | null;
  venueAddress: string;
  landmark: string;
  cityNameField: string;
  state: string;
  country: string;
  mapsUrl?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  priceDisplay: string;
  regNote: string;
  seoTitle?: string | null;
  metaDescription?: string | null;
  updatedAt: string;
  highlights: EventHighlightItem[];
  partners: EventPartnerItem[];
}

export interface EventListResponse {
  success: boolean;
  message?: string;
  events?: EventListItem[];
  total?: number;
  page?: number;
  pageSize?: number;
}

export interface EventDetailResponse {
  success: boolean;
  message?: string;
  event?: EventDetailItem;
}

export interface ImageUploadResponse {
  success: boolean;
  message?: string;
  imageUrl?: string;
}

export interface UpsertEventRequest {
  slug: string;
  name: string;
  crumb: string;
  tagline: string;
  shortDescription: string;
  aboutHtml: string;
  highlightsHtml: string;
  associationHtml: string;
  categorySlug: string;
  citySlug?: string | null;
  featuredImageUrl?: string | null;
  bannerImageUrl?: string | null;
  thumbnailUrl?: string | null;
  dateDisplayText: string;
  timeDisplayText: string;
  startDate?: string | null;
  endDate?: string | null;
  dateISO?: string | null;
  attendanceMode: string;
  locationDisplayText: string;
  venueName: string;
  venueAddress: string;
  landmark: string;
  cityName: string;
  state: string;
  country: string;
  mapsUrl?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  priceDisplay: string;
  regNote: string;
  seoTitle?: string | null;
  metaDescription?: string | null;
  isPublished: boolean;
  isFeatured: boolean;
  sortOrder: number;
  highlights?: EventHighlightItem[];
  partners?: EventPartnerItem[];
}

export interface EventMutationResponse {
  success: boolean;
  message?: string;
  eventId?: number;
}

export interface PublicEventListParams {
  search?: string;
  category?: string;
  city?: string;
  lifecycle?: string;
  featured?: boolean;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class EventManagementService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/events';
  private readonly adminBase = '/api/events/admin';

  listPublished(params?: PublicEventListParams): Observable<EventListResponse> {
    let httpParams = new HttpParams();
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.category) httpParams = httpParams.set('category', params.category);
    if (params?.city) httpParams = httpParams.set('city', params.city);
    if (params?.lifecycle) httpParams = httpParams.set('lifecycle', params.lifecycle);
    if (params?.featured != null) httpParams = httpParams.set('featured', String(params.featured));
    if (params?.page) httpParams = httpParams.set('page', String(params.page));
    if (params?.pageSize) httpParams = httpParams.set('pageSize', String(params.pageSize));
    return this.http.get<EventListResponse>(apiUrl(this.base), { params: httpParams });
  }

  getPublishedBySlug(slug: string): Observable<EventDetailResponse> {
    return this.http.get<EventDetailResponse>(apiUrl(`${this.base}/${encodeURIComponent(slug)}`));
  }

  adminList(opts?: {
    search?: string;
    category?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<EventListResponse> {
    let params = new HttpParams();
    if (opts?.search) params = params.set('search', opts.search);
    if (opts?.category) params = params.set('category', opts.category);
    if (opts?.status) params = params.set('status', opts.status);
    if (opts?.page) params = params.set('page', String(opts.page));
    if (opts?.pageSize) params = params.set('pageSize', String(opts.pageSize));
    return this.http.get<EventListResponse>(apiUrl(this.adminBase), { params });
  }

  adminGet(id: number): Observable<EventDetailResponse> {
    return this.http.get<EventDetailResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  create(req: UpsertEventRequest): Observable<EventMutationResponse> {
    return this.http.post<EventMutationResponse>(apiUrl(this.adminBase), req);
  }

  update(id: number, req: UpsertEventRequest): Observable<EventMutationResponse> {
    return this.http.put<EventMutationResponse>(apiUrl(`${this.adminBase}/${id}`), req);
  }

  setPublished(id: number, isPublished: boolean): Observable<EventMutationResponse> {
    return this.http.patch<EventMutationResponse>(apiUrl(`${this.adminBase}/${id}/publish`), {
      isPublished,
    });
  }

  delete(id: number): Observable<EventMutationResponse> {
    return this.http.delete<EventMutationResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  uploadImage(file: File): Observable<ImageUploadResponse> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ImageUploadResponse>(apiUrl(`${this.adminBase}/upload-image`), form);
  }
}
