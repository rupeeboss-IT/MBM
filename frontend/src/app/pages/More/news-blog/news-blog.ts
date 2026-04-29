import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ArticleCategory, ArticleModel, ArticleSlug } from '../../../data/articles.data';
import { ARTICLES_DATA } from '../../../data/articles.data';

@Component({
  selector: 'app-news-blog',
  imports: [CommonModule, RouterLink],
  templateUrl: './news-blog.html',
  styleUrl: './news-blog.css',
})
export class NewsBlog {
  readonly active = signal<'all' | ArticleCategory>('all');

  readonly articles = signal<Array<{ slug: ArticleSlug; data: ArticleModel }>>(
    (Object.keys(ARTICLES_DATA) as ArticleSlug[]).map((slug) => ({ slug, data: ARTICLES_DATA[slug] }))
  );

  readonly filtered = computed(() => {
    const cat = this.active();
    const all = this.articles();
    if (cat === 'all') return all;
    return all.filter((a) => a.data.category === cat);
  });

  setFilter(cat: 'all' | ArticleCategory) {
    this.active.set(cat);
  }
}
