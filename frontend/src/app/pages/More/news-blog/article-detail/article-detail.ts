import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { DomSanitizer, Meta, Title } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ArticlesService } from '../../../../core/services/articles.service';
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
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

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
    this.title.setTitle(pageTitle);
    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ property: 'og:type', content: 'article' });
    this.meta.updateTag({ property: 'og:title', content: pageTitle });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ property: 'og:url', content: `/article/${slug}` });
    if (art.imageUrl) {
      this.meta.updateTag({ property: 'og:image', content: art.imageUrl });
    }
  }
}

