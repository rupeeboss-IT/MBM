import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  BlogCategoryService,
  type BlogCategoryItem,
} from '../../../core/services/blog-category.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-blog-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './blog-category-list.html',
  styleUrls: ['../../admin-shared.css', './blog-category-list.css'],
})
export class BlogCategoryList implements OnInit {
  private readonly api = inject(BlogCategoryService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly actionId = signal<number | null>(null);
  readonly categories = signal<BlogCategoryItem[]>([]);

  async ngOnInit() {
    await this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminList());
      if (!res.success) {
        this.toast.error(res.message ?? 'Failed to load categories.');
        return;
      }
      this.categories.set(res.categories ?? []);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load categories.'));
    } finally {
      this.loading.set(false);
    }
  }

  edit(id: number) {
    void this.router.navigate(['/admin/blog-categories/edit', id]);
  }

  async remove(cat: BlogCategoryItem) {
    if (!confirm(`Delete category "${cat.label}"? This cannot be undone.`)) return;

    this.actionId.set(cat.blogCategoryId);
    try {
      const res = await firstValueFrom(this.api.delete(cat.blogCategoryId));
      if (res.success) {
        this.categories.update((list) => list.filter((c) => c.blogCategoryId !== cat.blogCategoryId));
        this.toast.success('Category deleted.');
      } else {
        this.toast.error(res.message ?? 'Delete failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Delete failed.'));
    } finally {
      this.actionId.set(null);
    }
  }
}
