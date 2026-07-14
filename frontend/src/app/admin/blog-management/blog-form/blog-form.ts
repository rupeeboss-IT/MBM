import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  BlogManagementService,
  type CreateBlogRequest,
  type UpdateBlogRequest,
} from '../../../core/services/blog-management.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { RichEditor } from './rich-editor/rich-editor';

interface BlogFormData {
  slug: string;
  title: string;
  crumb: string;
  meta: string;
  content: string;
  category: string;
  dateLabel: string;
  summary: string;
  badgeText: string;
  badgeClass: string;
  cardIcon: string;
  cardClass: string;
  imageUrl: string;
  seoTitle: string;
  metaDescription: string;
  isPublished: boolean;
}

const CATEGORY_DEFAULTS: Record<string, { badgeClass: string; cardClass: string; cardIcon: string }> = {
  blog: { badgeClass: 'badge badge-green', cardClass: 'news-img cat-blog', cardIcon: '📰' },
  news: { badgeClass: 'badge badge-blue', cardClass: 'news-img cat-news', cardIcon: '📢' },
  success: { badgeClass: 'badge badge-amber', cardClass: 'news-img cat-success', cardIcon: '🌟' },
};

function emptyForm(): BlogFormData {
  return {
    slug: '',
    title: '',
    crumb: '',
    meta: '',
    content: '',
    category: 'blog',
    dateLabel: '',
    summary: '',
    badgeText: 'MSME',
    badgeClass: 'badge badge-green',
    cardIcon: '📰',
    cardClass: 'news-img cat-blog',
    imageUrl: '',
    seoTitle: '',
    metaDescription: '',
    isPublished: false,
  };
}

function toSlug(title: string): string {
  return title
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .slice(0, 200);
}

function currentMonthLabel(): string {
  return new Date().toLocaleDateString('en-IN', { month: 'long', year: 'numeric' });
}

@Component({
  selector: 'app-blog-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RichEditor],
  templateUrl: './blog-form.html',
  styleUrls: ['../../admin-shared.css', './blog-form.css'],
})
export class BlogForm implements OnInit {
  private readonly api = inject(BlogManagementService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly blogId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly uploadingImage = signal(false);
  readonly showSeo = signal(false);
  readonly form = signal<BlogFormData>(emptyForm());

  readonly categories = ['blog', 'news', 'success'];
  readonly acceptImages = 'image/jpeg,image/png,image/webp,image/gif,.jpg,.jpeg,.png,.webp,.gif';

  async ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('blogId');
    if (idParam) {
      const id = Number(idParam);
      this.mode.set('edit');
      this.blogId.set(id);
      await this.loadExisting(id);
    }
  }

  private async loadExisting(id: number) {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminGet(id));
      if (!res.success || !res.blog) {
        this.toast.error(res.message ?? 'Blog not found.');
        await this.router.navigate(['/admin/blog-management']);
        return;
      }
      const b = res.blog;
      this.form.set({
        slug: b.slug,
        title: b.title,
        crumb: b.crumb,
        meta: b.meta,
        content: b.content,
        category: b.category,
        dateLabel: b.dateLabel,
        summary: b.summary,
        badgeText: b.badgeText,
        badgeClass: b.badgeClass,
        cardIcon: b.cardIcon,
        cardClass: b.cardClass,
        imageUrl: b.imageUrl ?? '',
        seoTitle: b.seoTitle ?? '',
        metaDescription: b.metaDescription ?? '',
        isPublished: b.isPublished,
      });
      if (b.seoTitle || b.metaDescription) this.showSeo.set(true);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load blog.'));
      await this.router.navigate(['/admin/blog-management']);
    } finally {
      this.loading.set(false);
    }
  }

  updateField<K extends keyof BlogFormData>(field: K, value: BlogFormData[K]) {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  onTitleBlur() {
    const title = this.form().title.trim();
    if (!title) return;

    this.form.update((f) => {
      const next = { ...f };
      if (this.mode() === 'create' && !f.slug.trim()) {
        next.slug = toSlug(title);
      }
      if (!f.crumb.trim()) next.crumb = title.slice(0, 80);
      if (!f.dateLabel.trim()) next.dateLabel = currentMonthLabel();
      if (!f.meta.trim()) {
        const cat = (f.category || 'blog').replace(/^\w/, (c) => c.toUpperCase());
        next.meta = `${cat} · ${currentMonthLabel()}`;
      }
      return next;
    });
  }

  onCategoryChange(category: string) {
    const defaults = CATEGORY_DEFAULTS[category] ?? CATEGORY_DEFAULTS['blog'];
    this.form.update((f) => ({
      ...f,
      category,
      badgeClass: defaults.badgeClass,
      cardClass: defaults.cardClass,
      cardIcon: defaults.cardIcon,
      meta: f.meta.trim()
        ? f.meta
        : `${category.replace(/^\w/, (c) => c.toUpperCase())} · ${currentMonthLabel()}`,
    }));
  }

  async onCoverSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.toast.error('Please choose an image file (JPG, PNG, WEBP, or GIF).');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.toast.error('Image must be 5 MB or smaller.');
      return;
    }

    this.uploadingImage.set(true);
    try {
      const res = await firstValueFrom(this.api.uploadImage(file));
      if (res.success && res.imageUrl) {
        this.updateField('imageUrl', res.imageUrl);
        this.toast.success('Cover image uploaded.');
      } else {
        this.toast.error(res.message ?? 'Failed to upload image.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to upload image.'));
    } finally {
      this.uploadingImage.set(false);
    }
  }

  clearCover() {
    this.updateField('imageUrl', '');
  }

  saveAsDraft() {
    this.form.update((f) => ({ ...f, isPublished: false }));
    void this.save();
  }

  saveAndPublish() {
    this.form.update((f) => ({ ...f, isPublished: true }));
    void this.save();
  }

  private applyDefaults(f: BlogFormData): BlogFormData {
    const title = f.title.trim();
    const cat = (f.category || 'blog').trim().toLowerCase();
    const defaults = CATEGORY_DEFAULTS[cat] ?? CATEGORY_DEFAULTS['blog'];
    const catLabel = cat.replace(/^\w/, (c) => c.toUpperCase());

    return {
      ...f,
      title,
      slug: f.slug.trim() || toSlug(title),
      crumb: f.crumb.trim() || title.slice(0, 80),
      dateLabel: f.dateLabel.trim() || currentMonthLabel(),
      meta: f.meta.trim() || `${catLabel} · ${currentMonthLabel()}`,
      badgeText: f.badgeText.trim() || 'MSME',
      badgeClass: f.badgeClass.trim() || defaults.badgeClass,
      cardClass: f.cardClass.trim() || defaults.cardClass,
      cardIcon: f.cardIcon.trim() || defaults.cardIcon,
    };
  }

  async save() {
    const f = this.applyDefaults(this.form());
    this.form.set(f);

    if (!f.title.trim()) {
      this.toast.error('Title is required.');
      return;
    }
    if (!f.slug.trim()) {
      this.toast.error('URL slug is required.');
      return;
    }
    if (!f.content.trim() || f.content === '<p><br></p>') {
      this.toast.error('Article body is required.');
      return;
    }

    this.saving.set(true);
    try {
      if (this.mode() === 'create') {
        const req: CreateBlogRequest = {
          slug: f.slug,
          title: f.title,
          crumb: f.crumb,
          meta: f.meta,
          content: f.content,
          category: f.category,
          dateLabel: f.dateLabel,
          summary: f.summary.trim(),
          badgeText: f.badgeText,
          badgeClass: f.badgeClass,
          cardIcon: f.cardIcon,
          cardClass: f.cardClass,
          imageUrl: f.imageUrl.trim() || null,
          seoTitle: f.seoTitle.trim() || null,
          metaDescription: f.metaDescription.trim() || null,
          isPublished: f.isPublished,
        };
        const res = await firstValueFrom(this.api.create(req));
        if (res.success) {
          this.toast.success(f.isPublished ? 'Article published.' : 'Draft saved.');
          await this.router.navigate(['/admin/blog-management']);
        } else {
          this.toast.error(res.message ?? 'Failed to create article.');
        }
      } else {
        const id = this.blogId()!;
        const req: UpdateBlogRequest = {
          title: f.title,
          crumb: f.crumb,
          meta: f.meta,
          content: f.content,
          category: f.category,
          dateLabel: f.dateLabel,
          summary: f.summary.trim(),
          badgeText: f.badgeText,
          badgeClass: f.badgeClass,
          cardIcon: f.cardIcon,
          cardClass: f.cardClass,
          imageUrl: f.imageUrl.trim() || null,
          seoTitle: f.seoTitle.trim() || null,
          metaDescription: f.metaDescription.trim() || null,
          isPublished: f.isPublished,
        };
        const res = await firstValueFrom(this.api.update(id, req));
        if (res.success) {
          this.toast.success(f.isPublished ? 'Article published.' : 'Draft saved.');
          await this.router.navigate(['/admin/blog-management']);
        } else {
          this.toast.error(res.message ?? 'Failed to update article.');
        }
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to save article.'));
    } finally {
      this.saving.set(false);
    }
  }

  cancel() {
    void this.router.navigate(['/admin/blog-management']);
  }
}
