import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  BlogCategoryService,
  type UpsertBlogCategoryRequest,
} from '../../../core/services/blog-category.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

interface CategoryFormData {
  slug: string;
  label: string;
  sortOrder: number;
  isActive: boolean;
  showInFilter: boolean;
}

function emptyForm(): CategoryFormData {
  return {
    slug: '',
    label: '',
    sortOrder: 1,
    isActive: true,
    showInFilter: true,
  };
}

function toSlug(label: string): string {
  return label
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .slice(0, 50);
}

function nextSortOrder(sortOrders: number[]): number {
  if (!sortOrders.length) return 1;
  return Math.max(...sortOrders) + 1;
}

@Component({
  selector: 'app-blog-category-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './blog-category-form.html',
  styleUrls: ['../../admin-shared.css', './blog-category-form.css'],
})
export class BlogCategoryForm implements OnInit {
  private readonly api = inject(BlogCategoryService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly categoryId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly form = signal<CategoryFormData>(emptyForm());

  async ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('categoryId');
    if (idParam) {
      const id = Number(idParam);
      this.mode.set('edit');
      this.categoryId.set(id);
      await this.loadExisting(id);
    } else {
      await this.initCreateDefaults();
    }
  }

  private async initCreateDefaults() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminList());
      const orders = (res.categories ?? []).map((c) => Number(c.sortOrder) || 0);
      this.updateField('sortOrder', nextSortOrder(orders));
    } catch {
      this.updateField('sortOrder', 1);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadExisting(id: number) {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminGet(id));
      if (!res.success || !res.category) {
        this.toast.error(res.message ?? 'Category not found.');
        await this.router.navigate(['/admin/blog-categories']);
        return;
      }
      const c = res.category;
      this.form.set({
        slug: c.slug,
        label: c.label,
        sortOrder: c.sortOrder,
        isActive: c.isActive,
        showInFilter: c.showInFilter,
      });
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load category.'));
      await this.router.navigate(['/admin/blog-categories']);
    } finally {
      this.loading.set(false);
    }
  }

  updateField<K extends keyof CategoryFormData>(field: K, value: CategoryFormData[K]) {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  onLabelBlur() {
    const label = this.form().label.trim();
    if (!label) return;
    if (this.mode() === 'create' && !this.form().slug.trim()) {
      this.updateField('slug', toSlug(label));
    }
  }

  async save() {
    const f = this.form();
    if (!f.label.trim()) {
      this.toast.error('Filter name is required.');
      return;
    }
    if (!f.slug.trim()) {
      this.toast.error('Slug is required.');
      return;
    }

    const req: UpsertBlogCategoryRequest = {
      slug: f.slug.trim(),
      label: f.label.trim(),
      sortOrder: Number(f.sortOrder) || 0,
      isActive: f.isActive,
      showInFilter: f.showInFilter,
    };

    this.saving.set(true);
    try {
      if (this.mode() === 'create') {
        const res = await firstValueFrom(this.api.create(req));
        if (res.success) {
          this.toast.success('Category created.');
          await this.router.navigate(['/admin/blog-categories']);
        } else {
          this.toast.error(res.message ?? 'Failed to create category.');
        }
      } else {
        const res = await firstValueFrom(this.api.update(this.categoryId()!, req));
        if (res.success) {
          this.toast.success('Category updated.');
          await this.router.navigate(['/admin/blog-categories']);
        } else {
          this.toast.error(res.message ?? 'Failed to update category.');
        }
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to save category.'));
    } finally {
      this.saving.set(false);
    }
  }

  cancel() {
    void this.router.navigate(['/admin/blog-categories']);
  }
}
