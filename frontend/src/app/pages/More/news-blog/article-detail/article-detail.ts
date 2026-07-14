import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ArticlesService } from '../../../../core/services/articles.service';
import { SeoService } from '../../../../core/services/seo.service';
import type { ArticleModel, ArticleSlug } from '../../../../data/articles.data';

@Component({
  selector: 'app-article-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './article-detail.html',
  styleUrl: './article-detail.css',
})
export class ArticleDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly articles = inject(ArticlesService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly seoService = inject(SeoService);

  readonly slug = signal<string>('');
  readonly article = signal<ArticleModel | null>(null);

  readonly safeHtml = computed(() => {
    const art = this.article();
    if (!art) return null;
    return this.sanitizer.bypassSecurityTrustHtml(art.content);
  });

  constructor() {
    this.route.paramMap.subscribe((p) => {
      const slug = p.get('slug') ?? '';
      this.slug.set(slug);

      const art = this.articles.getArticleBySlug(slug);
      if (!art) {
        void this.router.navigate(['/news']);
        return;
      }

      this.article.set(art);
      this.setSeo(slug as ArticleSlug, art);
    });
  }

  private setSeo(slug: ArticleSlug, art: ArticleModel) {
    const pageTitle = art.seoTitle ?? `${art.title} | MSME Bharat Manch`;
    const description = art.metaDescription ?? art.meta;
    const canonicalUrl = `https://msmebharatmanch.com/article/${slug}`;
    const ogImage = art.imageUrl
      ? (art.imageUrl.startsWith('http') ? art.imageUrl : `https://msmebharatmanch.com${art.imageUrl}`)
      : undefined;

    this.seoService.setPage({
      title: pageTitle,
      description,
      ogType: 'article',
      ogUrl: canonicalUrl,
      ogImage,
    });

    this.seoService.setJsonLd({
      '@context': 'https://schema.org',
      '@type': 'Article',
      'headline': pageTitle,
      'description': description,
      'url': canonicalUrl,
      'mainEntityOfPage': { '@type': 'WebPage', '@id': canonicalUrl },
      ...(ogImage ? { 'image': ogImage } : {}),
      'author': {
        '@type': 'Organization',
        'name': 'MSME Bharat Manch',
        'url': 'https://msmebharatmanch.com',
      },
      'publisher': {
        '@type': 'Organization',
        'name': 'MSME Bharat Manch',
        'logo': {
          '@type': 'ImageObject',
          'url': 'https://msmebharatmanch.com/assets/mbmlogo.png',
        },
      },
    });
  }
}

