import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { apiUrl } from '../utils/api-url';
import type {
  SchemeDetailItem,
  SchemeListItem,
  SchemeListResponse,
  SchemeDetailResponse,
} from './scheme-management.service';

/** Shape consumed by public schemes list, detail, and home. */
export interface PublicScheme {
  slug: string;
  title: string;
  crumb: string;
  subtitle: string;
  shortDescription: string;
  categorySlug: string;
  categoryName: string;
  primaryBadgeText: string;
  primaryBadgeClass: string;
  secondaryBadgeText: string;
  secondaryBadgeClass: string;
  cardHighlights: string[];
  homeTitle: string;
  homeBadgeText: string;
  homeBadgeClass: string;
  homeDescription: string;
  contentHtml: string;
  benefits: string[];
  seoTitle?: string | null;
  metaDescription?: string | null;
  isFeatured?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SchemesService {
  private readonly http = inject(HttpClient);

  getPublished(params?: {
    search?: string;
    category?: string;
    featured?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<{ schemes: PublicScheme[]; total: number }> {
    let httpParams: Record<string, string> = {};
    if (params?.search) httpParams['search'] = params.search;
    if (params?.category) httpParams['category'] = params.category;
    if (params?.featured != null) httpParams['featured'] = String(params.featured);
    if (params?.page) httpParams['page'] = String(params.page);
    if (params?.pageSize) httpParams['pageSize'] = String(params.pageSize);

    return this.http.get<SchemeListResponse>(apiUrl('/api/schemes'), { params: httpParams }).pipe(
      map((res) => {
        if (!res?.success) {
          throw new Error(res?.message ?? 'Failed to load schemes.');
        }
        const items = res.schemes ?? [];
        return {
          schemes: items.map((s) => this.fromListItem(s)),
          total: res.total ?? items.length,
        };
      }),
    );
  }

  getPublishedBySlug(slug: string): Observable<PublicScheme | null> {
    return this.http
      .get<SchemeDetailResponse>(apiUrl(`/api/schemes/${encodeURIComponent(slug)}`))
      .pipe(map((res) => (res?.scheme ? this.fromDetailItem(res.scheme) : null)));
  }

  private fromListItem(s: SchemeListItem): PublicScheme {
    return {
      slug: s.slug,
      title: s.name,
      crumb: s.crumb || s.name,
      subtitle: s.tagline,
      shortDescription: s.shortDescription || s.tagline,
      categorySlug: s.categorySlug,
      categoryName: s.categoryName || s.categorySlug,
      primaryBadgeText: s.categoryName || s.primaryBadgeText,
      primaryBadgeClass: s.primaryBadgeClass || 'badge-green',
      secondaryBadgeText: s.secondaryBadgeText,
      secondaryBadgeClass: s.secondaryBadgeClass || 'badge-orange',
      cardHighlights: (s.cardHighlights ?? []).map((h) => h.text).filter(Boolean),
      homeTitle: s.homeTitle || s.crumb || s.name,
      homeBadgeText: s.homeBadgeText || s.categoryName || s.primaryBadgeText,
      homeBadgeClass: s.homeBadgeClass || s.primaryBadgeClass || 'badge-green',
      homeDescription: s.homeDescription || s.shortDescription || s.tagline,
      contentHtml: '',
      benefits: [],
      isFeatured: s.isFeatured,
    };
  }

  private fromDetailItem(s: SchemeDetailItem): PublicScheme {
    const base = this.fromListItem(s);
    return {
      ...base,
      contentHtml: s.contentHtml || '',
      benefits: (s.benefits ?? []).map((b) => b.text).filter(Boolean),
      seoTitle: s.seoTitle,
      metaDescription: s.metaDescription,
    };
  }
}
