import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  SchemeCategoryService,
  type SchemeCategoryItem,
} from '../../core/services/scheme-category.service';
import { SchemesService, type PublicScheme } from '../../core/services/schemes.service';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-schemes',
  imports: [CommonModule, RouterLink],
  templateUrl: './schemes.html',
  styleUrl: './schemes.css',
})
export class Schemes implements OnInit {
  private readonly schemesApi = inject(SchemesService);
  private readonly categoryApi = inject(SchemeCategoryService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly schemes = signal<PublicScheme[]>([]);
  readonly categories = signal<SchemeCategoryItem[]>([]);
  readonly activeCategory = signal<string>('all');

  async ngOnInit() {
    await Promise.all([this.loadFilters(), this.loadSchemes()]);
  }

  private async loadFilters() {
    try {
      const catRes = await firstValueFrom(this.categoryApi.listPublic(true));
      this.categories.set(catRes.categories ?? []);
    } catch {
      this.categories.set([]);
    }
  }

  async loadSchemes() {
    try {
      this.loading.set(true);
      this.error.set(null);
      const res = await firstValueFrom(
        this.schemesApi.getPublished({
          category: this.activeCategory() === 'all' ? undefined : this.activeCategory(),
          page: 1,
          pageSize: 100,
        }),
      );
      this.schemes.set(res.schemes);
    } catch (e: unknown) {
      this.error.set(getHttpErrorMessage(e, 'Failed to load schemes.'));
      this.schemes.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  setCategory(slug: string) {
    this.activeCategory.set(slug);
    void this.loadSchemes();
  }

  badgeClass(cls: string | null | undefined): string {
    const c = (cls || 'badge-green').trim();
    return c.startsWith('badge ') ? c : `badge ${c}`;
  }
}
