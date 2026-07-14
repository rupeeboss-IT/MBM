import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ArticlesService, type PublicArticle } from '../../../core/services/articles.service';
import { firstValueFrom } from 'rxjs';

type CategoryFilter = 'all' | 'news' | 'blog' | 'success';

@Component({
  selector: 'app-news-blog',
  imports: [CommonModule, RouterLink],
  templateUrl: './news-blog.html',
  styleUrl: './news-blog.css',
})
export class NewsBlog implements OnInit {
  private readonly articlesService = inject(ArticlesService);

  readonly loading = signal(true);
  readonly active = signal<CategoryFilter>('all');
  readonly articles = signal<Array<{ slug: string; data: PublicArticle }>>([]);

  readonly filtered = computed(() => {
    const cat = this.active();
    const all = this.articles();
    if (cat === 'all') return all;
    return all.filter((a) => a.data.category === cat);
  });

  async ngOnInit() {
    try {
      const items = await firstValueFrom(this.articlesService.getPublished());
      this.articles.set(items);
    } finally {
      this.loading.set(false);
    }
  }

  setFilter(cat: CategoryFilter) {
    this.active.set(cat);
  }
}
