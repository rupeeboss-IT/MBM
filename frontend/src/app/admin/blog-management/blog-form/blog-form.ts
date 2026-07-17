import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  BlogCategoryService,
  type BlogCategoryItem,
} from '../../../core/services/blog-category.service';
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
  badgeSlug: string;
  dateLabel: string;
  summary: string;
  imageUrl: string;
  seoTitle: string;
  metaDescription: string;
  isPublished: boolean;
}

function emptyForm(): BlogFormData {
  return {
    slug: '',
    title: '',
    crumb: '',
    meta: '',
    content: '',
    category: '',
    badgeSlug: '',
    dateLabel: '',
    summary: '',
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

function isAutoMeta(meta: string, labels: string[]): boolean {
  const trimmed = meta.trim();
  if (!trimmed) return true;
  if (!/ · [A-Za-z]+ \d{4}$/.test(trimmed)) return false;
  const prefix = trimmed.split(' · ')[0]?.trim().toLowerCase() ?? '';
  return labels.some((l) => l.toLowerCase() === prefix);
}

function metaForCategoryLabel(label: string, dateLabel?: string): string {
  return `${label} · ${dateLabel?.trim() || currentMonthLabel()}`;
}

function resolveMeta(meta: string, categoryLabel: string, dateLabel: string, labels: string[]): string {
  const trimmed = meta.trim();
  const when = dateLabel.trim() || currentMonthLabel();

  if (!trimmed || isAutoMeta(trimmed, labels)) {
    return metaForCategoryLabel(categoryLabel, when);
  }

  const prefixMatch = trimmed.match(/^(.+?)( · .+)$/);
  if (prefixMatch && isAutoMeta(trimmed, labels)) {
    return `${categoryLabel}${prefixMatch[2]}`;
  }

  return trimmed;
}

@Component({
  selector: 'app-blog-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RichEditor, RouterLink],
  templateUrl: './blog-form.html',
  styleUrls: ['../../admin-shared.css', './blog-form.css'],
})
export class BlogForm implements OnInit {
  private readonly api = inject(BlogManagementService);
  private readonly categoryApi = inject(BlogCategoryService);
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
  readonly categories = signal<BlogCategoryItem[]>([]);

  readonly acceptImages = 'image/jpeg,image/png,image/webp,image/gif,.jpg,.jpeg,.png,.webp,.gif';

  readonly selectedCategory = computed(() => {
    const slug = this.form().category;
    return this.categories().find((c) => c.slug === slug) ?? null;
  });

  readonly categoryLabels = computed(() => this.categories().map((c) => c.label));

  async ngOnInit() {
    await this.loadCategories();

    const idParam = this.route.snapshot.paramMap.get('blogId');
    if (idParam) {
      const id = Number(idParam);
      this.mode.set('edit');
      this.blogId.set(id);
      await this.loadExisting(id);
    } else {
      const firstCat = this.categories()[0];
      if (firstCat) this.applyCategory(firstCat.slug, { resetMeta: true });
    }
  }

  private async loadCategories() {
    try {
      const res = await firstValueFrom(this.categoryApi.listPublic());
      const items = (res.categories ?? []).filter((c) => c.isActive);
      this.categories.set(items);
      if (!items.length) {
        this.toast.error('No active blog categories. Add categories first.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load categories.'));
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
        badgeSlug: '',
        dateLabel: b.dateLabel,
        summary: b.summary,
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

    const cat = this.selectedCategory();
    const labels = this.categoryLabels();

    this.form.update((f) => {
      const next = { ...f };
      if (this.mode() === 'create' && !f.slug.trim()) {
        next.slug = toSlug(title);
      }
      if (!f.crumb.trim()) next.crumb = title.slice(0, 80);
      if (!f.dateLabel.trim()) next.dateLabel = currentMonthLabel();
      if (cat && (!f.meta.trim() || isAutoMeta(f.meta, labels))) {
        next.meta = metaForCategoryLabel(cat.label, next.dateLabel || f.dateLabel);
      }
      return next;
    });
  }

  onCategoryChange(slug: string) {
    this.applyCategory(slug, { resetMeta: true });
  }

  private applyCategory(slug: string, opts?: { resetMeta?: boolean }) {
    const cat = this.categories().find((c) => c.slug === slug);
    if (!cat) return;

    const labels = this.categoryLabels();
    this.form.update((f) => ({
      ...f,
      category: cat.slug,
      meta:
        opts?.resetMeta || !f.meta.trim() || isAutoMeta(f.meta, labels)
          ? metaForCategoryLabel(cat.label, f.dateLabel)
          : f.meta,
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
    const cat = this.categories().find((c) => c.slug === f.category);
    const dateLabel = f.dateLabel.trim() || currentMonthLabel();
    const labels = this.categoryLabels();

    return {
      ...f,
      title,
      slug: f.slug.trim() || toSlug(title),
      crumb: f.crumb.trim() || title.slice(0, 80),
      dateLabel,
      category: cat?.slug ?? f.category,
      badgeSlug: '',
      meta: cat ? resolveMeta(f.meta, cat.label, dateLabel, labels) : f.meta,
    };
  }

  async save() {
    if (!this.form().category) {
      this.toast.error('Please choose a category.');
      return;
    }

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
          badgeSlug: f.badgeSlug,
          dateLabel: f.dateLabel,
          summary: f.summary.trim(),
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
          badgeSlug: f.badgeSlug,
          dateLabel: f.dateLabel,
          summary: f.summary.trim(),
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
