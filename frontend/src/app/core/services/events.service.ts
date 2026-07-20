import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import type {
  EventDetailItem,
  EventListItem,
  EventListResponse,
  EventDetailResponse,
} from './event-management.service';

/** Shape consumed by public events list, detail, and home. */
export interface PublicEvent {
  slug: string;
  title: string;
  crumb: string;
  subtitle: string;
  shortDescription: string;
  categorySlug: string;
  categoryName: string;
  citySlug?: string | null;
  cityName?: string | null;
  cityBadgeClass?: string | null;
  date: string;
  time: string;
  location: string;
  venue: string;
  dateISO?: string | null;
  attendanceMode?: string;
  imageUrl?: string | null;
  aboutHtml: string;
  highlightsHtml: string;
  associationHtml: string;
  highlights: string[];
  partners: Array<{ name: string; logoUrl?: string | null; websiteUrl?: string | null }>;
  price: string;
  regNote?: string;
  seoTitle?: string | null;
  metaDescription?: string | null;
  lifecycle?: string;
  isFeatured?: boolean;
}

@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly http = inject(HttpClient);

  getPublished(params?: {
    search?: string;
    category?: string;
    city?: string;
    lifecycle?: string;
    featured?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<{ events: PublicEvent[]; total: number }> {
    let httpParams: Record<string, string> = {};
    if (params?.search) httpParams['search'] = params.search;
    if (params?.category) httpParams['category'] = params.category;
    if (params?.city) httpParams['city'] = params.city;
    if (params?.lifecycle) httpParams['lifecycle'] = params.lifecycle;
    if (params?.featured != null) httpParams['featured'] = String(params.featured);
    if (params?.page) httpParams['page'] = String(params.page);
    if (params?.pageSize) httpParams['pageSize'] = String(params.pageSize);

    return this.http.get<EventListResponse>(apiUrl('/api/events'), { params: httpParams }).pipe(
      map((res) => {
        if (!res?.success) {
          throw new Error(res?.message ?? 'Failed to load events.');
        }
        const items = res.events ?? [];
        return {
          events: items.map((e) => this.fromListItem(e)),
          total: res.total ?? items.length,
        };
      }),
    );
  }

  getPublishedBySlug(slug: string): Observable<PublicEvent | null> {
    return this.http
      .get<EventDetailResponse>(apiUrl(`/api/events/${encodeURIComponent(slug)}`))
      .pipe(map((res) => (res?.event ? this.fromDetailItem(res.event) : null)));
  }

  private fromListItem(e: EventListItem): PublicEvent {
    return {
      slug: e.slug,
      title: e.name,
      crumb: e.crumb || e.name,
      subtitle: e.tagline,
      shortDescription: e.shortDescription || e.tagline,
      categorySlug: e.categorySlug,
      categoryName: e.categoryName || e.categorySlug,
      citySlug: e.citySlug,
      cityName: e.cityName,
      cityBadgeClass: e.cityBadgeClass || 'badge-green',
      date: e.dateDisplayText,
      time: e.timeDisplayText,
      location: e.locationDisplayText || e.venueName,
      venue: e.venueName || e.locationDisplayText,
      dateISO: e.dateISO,
      attendanceMode: e.attendanceMode,
      imageUrl: e.featuredImageUrl,
      aboutHtml: '',
      highlightsHtml: '',
      associationHtml: '',
      highlights: [],
      partners: [],
      price: '',
      lifecycle: e.lifecycle,
      isFeatured: e.isFeatured,
    };
  }

  private fromDetailItem(e: EventDetailItem): PublicEvent {
    const base = this.fromListItem(e);
    return {
      ...base,
      aboutHtml: e.aboutHtml || '',
      highlightsHtml: e.highlightsHtml || '',
      associationHtml: e.associationHtml || '',
      highlights: (e.highlights ?? []).map((h) => h.text).filter(Boolean),
      partners: (e.partners ?? []).map((p) => ({
        name: p.name,
        logoUrl: p.logoUrl,
        websiteUrl: p.websiteUrl,
      })),
      price: e.priceDisplay || '',
      regNote: e.regNote || undefined,
      seoTitle: e.seoTitle,
      metaDescription: e.metaDescription,
      imageUrl: e.bannerImageUrl || e.featuredImageUrl,
    };
  }
}
