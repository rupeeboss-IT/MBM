import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { EVENTS_DATA, type EventModel, type EventSlug } from '../../data/events.data';
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

    return this.http
      .get<EventListResponse>(apiUrl('/api/events'), { params: httpParams })
      .pipe(
        map((res) => {
          if (res?.success) {
            const items = res.events ?? [];
            // Empty CMS catalog → show legacy static events so the site is never blank.
            if (items.length === 0 && !params?.search && !params?.category && !params?.city && !params?.lifecycle) {
              return this.staticList(params);
            }
            return {
              events: items.map((e) => this.fromListItem(e)),
              total: res.total ?? items.length,
            };
          }
          return this.staticList(params);
        }),
        catchError(() => of(this.staticList(params))),
      );
  }

  getPublishedBySlug(slug: string): Observable<PublicEvent | null> {
    return this.http.get<EventDetailResponse>(apiUrl(`/api/events/${encodeURIComponent(slug)}`)).pipe(
      map((res) => (res?.event ? this.fromDetailItem(res.event) : this.staticBySlug(slug))),
      catchError(() => of(this.staticBySlug(slug))),
    );
  }

  /** @deprecated Prefer getPublished() for public pages. Kept for dashboard fallback. */
  getEventBySlug(slug: string): EventModel | null {
    return (EVENTS_DATA as Record<string, EventModel>)[slug] ?? null;
  }

  /** @deprecated Prefer getPublished() for public pages. */
  getAllEvents(): Array<{ slug: EventSlug; data: EventModel }> {
    return (Object.keys(EVENTS_DATA) as EventSlug[]).map((slug) => ({
      slug,
      data: EVENTS_DATA[slug],
    }));
  }

  private staticList(params?: {
    search?: string;
    category?: string;
    city?: string;
  }): { events: PublicEvent[]; total: number } {
    let events = (Object.keys(EVENTS_DATA) as EventSlug[]).map((slug) =>
      this.fromStatic(slug, EVENTS_DATA[slug]),
    );

    if (params?.search) {
      const s = params.search.toLowerCase();
      events = events.filter(
        (e) =>
          e.title.toLowerCase().includes(s) ||
          e.shortDescription.toLowerCase().includes(s) ||
          e.subtitle.toLowerCase().includes(s),
      );
    }
    if (params?.category) {
      const c = params.category.toLowerCase();
      events = events.filter((e) => e.categorySlug.toLowerCase() === c);
    }
    if (params?.city) {
      const c = params.city.toLowerCase();
      events = events.filter((e) => (e.citySlug ?? e.cityName ?? '').toLowerCase().includes(c));
    }

    return { events, total: events.length };
  }

  private staticBySlug(slug: string): PublicEvent | null {
    const data = (EVENTS_DATA as Record<string, EventModel>)[slug];
    return data ? this.fromStatic(slug, data) : null;
  }

  private fromStatic(slug: string, data: EventModel): PublicEvent {
    const sections = splitLegacyEventBody(data.body);
    return {
      slug,
      title: data.title,
      crumb: data.crumb,
      subtitle: data.subtitle,
      shortDescription: data.subtitle,
      categorySlug: (data.type || 'bll').toLowerCase(),
      categoryName: data.type || 'BLL',
      citySlug: 'mumbai',
      cityName: 'Mumbai',
      cityBadgeClass: 'badge-green',
      date: data.date,
      time: '',
      location: data.venue,
      venue: data.venue,
      dateISO: data.dateISO,
      imageUrl: data.imageUrl,
      aboutHtml: sections.about,
      highlightsHtml: sections.highlights,
      associationHtml: '',
      highlights: [],
      partners: sections.partners,
      price: data.price,
      regNote: data.regNote,
    };
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

/** Split legacy static HTML that embeds About / Highlights / Association under h2 tags. */
function splitLegacyEventBody(body: string): {
  about: string;
  highlights: string;
  partners: Array<{ name: string; logoUrl?: string | null; websiteUrl?: string | null }>;
} {
  const html = (body ?? '').trim();
  if (!html) return { about: '', highlights: '', partners: [] };

  const parts = html.split(/<h2\b[^>]*>/i);
  let about = '';
  let highlights = '';
  let associationHtml = '';

  for (const part of parts) {
    if (!part.trim()) continue;
    const closeIdx = part.search(/<\/h2>/i);
    if (closeIdx < 0) {
      if (!about) about = part.trim();
      continue;
    }
    const title = part.slice(0, closeIdx).replace(/<[^>]+>/g, '').trim().toLowerCase();
    const content = part.slice(closeIdx + 5).trim();
    if (title.includes('about')) about = content;
    else if (title.includes('highlight')) highlights = content;
    else if (title.includes('association')) associationHtml = content;
    else if (!about) about = `<h2>${part.slice(0, closeIdx)}</h2>${content}`;
  }

  const partners: Array<{ name: string; logoUrl?: string | null; websiteUrl?: string | null }> = [];
  if (associationHtml) {
    const text = associationHtml.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    if (text) partners.push({ name: text });
  }

  return { about, highlights, partners };
}
