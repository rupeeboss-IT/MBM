import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { BlogBadgeService, type UpsertBlogBadgeRequest } from '../../../core/services/blog-badge.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

interface BadgeFormData {
  slug: string;
  label: string;
  badgeText: string;
  badgeClass: string;
  cardIcon: string;
  cardClass: string;
  sortOrder: number;
  isActive: boolean;
}

function emptyForm(): BadgeFormData {
  return {
    slug: '',
    label: '',
    badgeText: 'MSME',
    badgeClass: 'badge badge-green',
    cardIcon: '📰',
    cardClass: 'news-img cat-blog',
    sortOrder: 10,
    isActive: true,
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

@Component({
  selector: 'app-blog-badge-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './blog-badge-form.html',
  styleUrls: ['../../admin-shared.css', './blog-badge-form.css'],
})
export class BlogBadgeForm implements OnInit {
  private readonly api = inject(BlogBadgeService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly badgeId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly form = signal<BadgeFormData>(emptyForm());

  readonly badgeClassOptions = [
    { value: 'badge badge-green', label: 'Green' },
    { value: 'badge badge-blue', label: 'Blue' },
    { value: 'badge badge-amber', label: 'Amber' },
    { value: 'badge badge-red', label: 'Red' },
    { value: 'badge badge-orange', label: 'Orange' },
  ];

  readonly cardClassOptions = [
    { value: 'news-img cat-blog', label: 'Green background' },
    { value: 'news-img cat-news', label: 'Blue background' },
    { value: 'news-img cat-success', label: 'Amber background' },
  ];

  async ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('badgeId');
    if (idParam) {
      const id = Number(idParam);
      this.mode.set('edit');
      this.badgeId.set(id);
      await this.loadExisting(id);
    }
  }

  private async loadExisting(id: number) {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminGet(id));
      if (!res.success || !res.badge) {
        this.toast.error(res.message ?? 'Card label not found.');
        await this.router.navigate(['/admin/blog-badges']);
        return;
      }
      const b = res.badge;
      this.form.set({
        slug: b.slug,
        label: b.label,
        badgeText: b.badgeText,
        badgeClass: b.badgeClass,
        cardIcon: b.cardIcon,
        cardClass: b.cardClass,
        sortOrder: b.sortOrder,
        isActive: b.isActive,
      });
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load card label.'));
      await this.router.navigate(['/admin/blog-badges']);
    } finally {
      this.loading.set(false);
    }
  }

  updateField<K extends keyof BadgeFormData>(field: K, value: BadgeFormData[K]) {
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
      this.toast.error('Dropdown name is required.');
      return;
    }
    if (!f.slug.trim()) {
      this.toast.error('Slug is required.');
      return;
    }
    if (!f.badgeText.trim()) {
      this.toast.error('Pill text is required.');
      return;
    }

    const req: UpsertBlogBadgeRequest = {
      slug: f.slug.trim(),
      label: f.label.trim(),
      badgeText: f.badgeText.trim(),
      badgeClass: f.badgeClass,
      cardIcon: f.cardIcon.trim() || '📰',
      cardClass: f.cardClass,
      sortOrder: Number(f.sortOrder) || 0,
      isActive: f.isActive,
    };

    this.saving.set(true);
    try {
      if (this.mode() === 'create') {
        const res = await firstValueFrom(this.api.create(req));
        if (res.success) {
          this.toast.success('Card label created.');
          await this.router.navigate(['/admin/blog-badges']);
        } else {
          this.toast.error(res.message ?? 'Failed to create card label.');
        }
      } else {
        const res = await firstValueFrom(this.api.update(this.badgeId()!, req));
        if (res.success) {
          this.toast.success('Card label updated.');
          await this.router.navigate(['/admin/blog-badges']);
        } else {
          this.toast.error(res.message ?? 'Failed to update card label.');
        }
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to save card label.'));
    } finally {
      this.saving.set(false);
    }
  }

  cancel() {
    void this.router.navigate(['/admin/blog-badges']);
  }
}
