import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ArticlesService, type PublicArticle } from '../../../../core/services/articles.service';
import { SeoService } from '../../../../core/services/seo.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-article-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './article-detail.html',
  styleUrl: './article-detail.css',
})
export class ArticleDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly articles = inject(ArticlesService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly seoService = inject(SeoService);

  readonly loading = signal(true);
  readonly slug = signal<string>('');
  readonly article = signal<PublicArticle | null>(null);

  readonly safeHtml = computed<SafeHtml | null>(() => {
    const art = this.article();
    if (!art?.content) return null;
    return this.sanitizer.bypassSecurityTrustHtml(art.content);
  });

  async ngOnInit() {
    this.route.paramMap.subscribe(async (p) => {
      const slug = p.get('slug') ?? '';
      this.slug.set(slug);
      this.loading.set(true);

      try {
        const art = await firstValueFrom(this.articles.getPublishedBySlug(slug));
        if (!art) {
          await this.router.navigate(['/news']);
          return;
        }
        this.article.set(art);
        this.setSeo(slug, art);
      } catch {
        await this.router.navigate(['/news']);
      } finally {
        this.loading.set(false);
      }
    });
  }

  private setSeo(slug: string, art: PublicArticle) {
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
