import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { ARTICLES_DATA, type ArticleModel, type ArticleSlug } from '../../data/articles.data';
import { apiUrl } from '../utils/api-url';
import type { BlogDetailItem, BlogListItem } from './blog-management.service';

/** Shape that both public components (NewsBlog, ArticleDetail) consume. */
export interface PublicArticle {
  slug: string;
  title: string;
  crumb: string;
  meta: string;
  content: string;
  category: string;
  dateLabel: string;
  summary: string;
  badgeText: string;
  badgeClass: string;
  cardIcon: string;
  cardClass: string;
  imageUrl?: string | null;
  seoTitle?: string | null;
  metaDescription?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ArticlesService {
  private readonly http = inject(HttpClient);

  // ── HTTP-based API calls (used by public pages) ──────────────────────────────

  /**
   * Fetch all published articles from the API.
   * Falls back to static data if the API is unavailable or returns nothing.
   */
  getPublished(): Observable<Array<{ slug: string; data: PublicArticle }>> {
    return this.http
      .get<{ success: boolean; blogs?: BlogListItem[] }>(apiUrl('/api/blogs'))
      .pipe(
        map((res) => {
          const items = res?.blogs ?? [];
          if (items.length > 0) {
            return items.map((b) => ({ slug: b.slug, data: this.fromApiListItem(b) }));
          }
          return this.staticArticles();
        }),
        catchError(() => of(this.staticArticles())),
      );
  }

  /**
   * Fetch a single published article by slug from the API.
   * Falls back to static data if not found or API is unavailable.
   */
  getPublishedBySlug(slug: string): Observable<PublicArticle | null> {
    return this.http
      .get<{ success: boolean; blog?: BlogDetailItem }>(apiUrl(`/api/blogs/${slug}`))
      .pipe(
        map((res) => (res?.blog ? this.fromApiDetailItem(res.blog) : this.staticBySlug(slug))),
        catchError(() => of(this.staticBySlug(slug))),
      );
  }

  // ── Static helpers (kept for admin DashboardDetail + backward compat) ────────

  /** @deprecated Use getPublished() for public pages. */
  getArticleBySlug(slug: string): ArticleModel | null {
    return (ARTICLES_DATA as Record<string, ArticleModel>)[slug] ?? null;
  }

  /** @deprecated Use getPublished() for public pages. */
  getAllArticles(): Array<{ slug: ArticleSlug; data: ArticleModel }> {
    return (Object.keys(ARTICLES_DATA) as ArticleSlug[]).map((slug) => ({
      slug,
      data: ARTICLES_DATA[slug],
    }));
  }

  // ── Private helpers ──────────────────────────────────────────────────────────

  private staticArticles(): Array<{ slug: string; data: PublicArticle }> {
    return (Object.keys(ARTICLES_DATA) as ArticleSlug[]).map((slug) => ({
      slug,
      data: ARTICLES_DATA[slug] as PublicArticle,
    }));
  }

  private staticBySlug(slug: string): PublicArticle | null {
    const art = (ARTICLES_DATA as Record<string, ArticleModel>)[slug];
    return art ? (art as PublicArticle) : null;
  }

  private fromApiListItem(b: BlogListItem): PublicArticle {
    return {
      slug: b.slug,
      title: b.title,
      crumb: b.crumb,
      meta: b.meta,
      content: '',
      category: b.category,
      dateLabel: b.dateLabel,
      summary: b.summary,
      badgeText: b.badgeText,
      badgeClass: b.badgeClass,
      cardIcon: b.cardIcon,
      cardClass: b.cardClass,
      imageUrl: b.imageUrl,
      seoTitle: b.seoTitle,
      metaDescription: b.metaDescription,
    };
  }

  private fromApiDetailItem(b: BlogDetailItem): PublicArticle {
    return {
      slug: b.slug,
      title: b.title,
      crumb: b.crumb,
      meta: b.meta,
      content: b.content,
      category: b.category,
      dateLabel: b.dateLabel,
      summary: b.summary,
      badgeText: b.badgeText,
      badgeClass: b.badgeClass,
      cardIcon: b.cardIcon,
      cardClass: b.cardClass,
      imageUrl: b.imageUrl,
      seoTitle: b.seoTitle,
      metaDescription: b.metaDescription,
    };
  }
}
