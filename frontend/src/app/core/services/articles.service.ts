import { Injectable } from '@angular/core';
import { ARTICLES_DATA, type ArticleModel, type ArticleSlug } from '../../data/articles.data';

@Injectable({ providedIn: 'root' })
export class ArticlesService {
  getArticleBySlug(slug: string): ArticleModel | null {
    return (ARTICLES_DATA as Record<string, ArticleModel>)[slug] ?? null;
  }

  getAllArticles(): Array<{ slug: ArticleSlug; data: ArticleModel }> {
    return (Object.keys(ARTICLES_DATA) as ArticleSlug[]).map((slug) => ({
      slug,
      data: ARTICLES_DATA[slug]
    }));
  }
}

