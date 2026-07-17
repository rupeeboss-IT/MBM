import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ArticlesService, type PublicArticle } from '../../../core/services/articles.service';
import { BlogCategoryService, type BlogCategoryItem } from '../../../core/services/blog-category.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-news-blog',
  imports: [CommonModule, RouterLink],
  templateUrl: './news-blog.html',
  styleUrl: './news-blog.css',
})
export class NewsBlog implements OnInit {
  private readonly articlesService = inject(ArticlesService);
  private readonly categoryApi = inject(BlogCategoryService);

  readonly loading = signal(true);
  readonly active = signal<string>('all');
  readonly articles = signal<Array<{ slug: string; data: PublicArticle }>>([]);
  readonly filterCategories = signal<BlogCategoryItem[]>([]);

  readonly filtered = computed(() => {
    const cat = this.active();
    const all = this.articles();
    if (cat === 'all') return all;
    return all.filter((a) => a.data.category === cat);
  });

  async ngOnInit() {
    try {
      const [items, catRes] = await Promise.all([
        firstValueFrom(this.articlesService.getPublished()),
        firstValueFrom(this.categoryApi.listPublic(true)),
      ]);
      this.articles.set(items);
      this.filterCategories.set(catRes.categories ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  setFilter(cat: string) {
    this.active.set(cat);
  }

  /** Card date as dd-MMM-yyyy (e.g. 17-Jul-2026). */
  formatCardDate(iso?: string | null): string {
    if (!iso) return '';
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';

    const day = String(d.getDate()).padStart(2, '0');
    const month = d.toLocaleString('en-GB', { month: 'short' });
    const year = d.getFullYear();
    return `${day}-${month}-${year}`;
  }
}
