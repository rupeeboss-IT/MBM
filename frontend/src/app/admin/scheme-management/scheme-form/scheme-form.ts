import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  SchemeCategoryService,
  type SchemeCategoryItem,
} from '../../../core/services/scheme-category.service';
import {
  SchemeManagementService,
  type SchemeBenefitItem,
  type SchemeCardHighlightItem,
  type UpsertSchemeRequest,
} from '../../../core/services/scheme-management.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { RichEditor } from '../../blog-management/blog-form/rich-editor/rich-editor';

interface TextRow {
  text: string;
  sortOrder: number;
}

interface SchemeFormData {
  slug: string;
  name: string;
  crumb: string;
  tagline: string;
  shortDescription: string;
  contentHtml: string;
  categorySlug: string;
  primaryBadgeClass: string;
  secondaryBadgeText: string;
  secondaryBadgeClass: string;
  homeTitle: string;
  homeBadgeText: string;
  homeBadgeClass: string;
  homeDescription: string;
  seoTitle: string;
  metaDescription: string;
  isPublished: boolean;
  isFeatured: boolean;
  sortOrder: number;
}

const BADGE_CLASSES = ['badge-green', 'badge-blue', 'badge-orange', 'badge-red'] as const;

function emptyForm(): SchemeFormData {
  return {
    slug: '',
    name: '',
    crumb: '',
    tagline: '',
    shortDescription: '',
    contentHtml: '',
    categorySlug: '',
    primaryBadgeClass: 'badge-green',
    secondaryBadgeText: '',
    secondaryBadgeClass: 'badge-blue',
    homeTitle: '',
    homeBadgeText: '',
    homeBadgeClass: 'badge-green',
    homeDescription: '',
    seoTitle: '',
    metaDescription: '',
    isPublished: false,
    isFeatured: false,
    sortOrder: 0,
  };
}

function toSlug(name: string): string {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .slice(0, 200);
}

function emptyTextRow(): TextRow {
  return { text: '', sortOrder: 0 };
}

@Component({
  selector: 'app-scheme-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RichEditor, RouterLink],
  templateUrl: './scheme-form.html',
  styleUrls: ['../../admin-shared.css', './scheme-form.css'],
})
export class SchemeForm implements OnInit {
  private readonly api = inject(SchemeManagementService);
  private readonly categoryApi = inject(SchemeCategoryService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly schemeId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly showSeo = signal(false);
  readonly form = signal<SchemeFormData>(emptyForm());
  readonly benefits = signal<TextRow[]>([]);
  readonly cardHighlights = signal<TextRow[]>([]);
  readonly categories = signal<SchemeCategoryItem[]>([]);
  readonly badgeClasses = BADGE_CLASSES;

  async ngOnInit() {
    await this.loadCategories();

    const idParam = this.route.snapshot.paramMap.get('schemeId');
    if (idParam) {
      const id = Number(idParam);
      this.mode.set('edit');
      this.schemeId.set(id);
      await this.loadExisting(id);
    } else {
      const firstCat = this.categories()[0];
      if (firstCat) this.onCategoryChange(firstCat.slug);
    }
  }

  private async loadCategories() {
    try {
      const res = await firstValueFrom(this.categoryApi.adminList());
      const items = (res.categories ?? []).filter((c) => c.isActive);
      this.categories.set(items);
      if (!items.length) {
        this.toast.error('No active scheme categories. Add categories first.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load categories.'));
    }
  }

  private async loadExisting(id: number) {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminGet(id));
      if (!res.success || !res.scheme) {
        this.toast.error(res.message ?? 'Scheme not found.');
        await this.router.navigate(['/admin/scheme-management']);
        return;
      }
      const s = res.scheme;
      this.form.set({
        slug: s.slug,
        name: s.name,
        crumb: s.crumb ?? '',
        tagline: s.tagline ?? '',
        shortDescription: s.shortDescription ?? '',
        contentHtml: s.contentHtml ?? '',
        categorySlug: s.categorySlug ?? '',
        primaryBadgeClass: s.primaryBadgeClass || 'badge-green',
        secondaryBadgeText: s.secondaryBadgeText ?? '',
        secondaryBadgeClass: s.secondaryBadgeClass || 'badge-blue',
        homeTitle: s.homeTitle ?? '',
        homeBadgeText: s.homeBadgeText ?? '',
        homeBadgeClass: s.homeBadgeClass || 'badge-green',
        homeDescription: s.homeDescription ?? '',
        seoTitle: s.seoTitle ?? '',
        metaDescription: s.metaDescription ?? '',
        isPublished: s.isPublished,
        isFeatured: s.isFeatured,
        sortOrder: s.sortOrder ?? 0,
      });

      this.benefits.set(
        (s.benefits ?? []).map((b, i) => ({
          text: b.text ?? '',
          sortOrder: b.sortOrder ?? i,
        })),
      );
      this.cardHighlights.set(
        (s.cardHighlights ?? []).map((h, i) => ({
          text: h.text ?? '',
          sortOrder: h.sortOrder ?? i,
        })),
      );

      if (s.seoTitle || s.metaDescription) this.showSeo.set(true);

      if (s.categorySlug && !this.categories().some((c) => c.slug === s.categorySlug)) {
        this.categories.update((list) => [
          ...list,
          {
            schemeCategoryId: 0,
            slug: s.categorySlug,
            name: s.categoryName || s.categorySlug,
            sortOrder: 0,
            isActive: true,
            showInFilter: true,
          },
        ]);
      }
    } catch (err: unknown) {
      this.toast.error(getHttpErrorMessage(err, 'Failed to load scheme.'));
      await this.router.navigate(['/admin/scheme-management']);
    } finally {
      this.loading.set(false);
    }
  }

  updateField<K extends keyof SchemeFormData>(field: K, value: SchemeFormData[K]) {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  categoryName(slug: string): string {
    return this.categories().find((c) => c.slug === slug)?.name ?? slug;
  }

  onCategoryChange(slug: string) {
    this.form.update((f) => ({ ...f, categorySlug: slug }));
  }

  onNameBlur() {
    const name = this.form().name.trim();
    if (!name) return;
    this.form.update((f) => {
      const next = { ...f };
      if (this.mode() === 'create' && !f.slug.trim()) {
        next.slug = toSlug(name);
      }
      if (!f.crumb.trim()) next.crumb = name.slice(0, 80);
      return next;
    });
  }

  addBenefit() {
    this.benefits.update((list) => [...list, { ...emptyTextRow(), sortOrder: list.length }]);
  }

  removeBenefit(index: number) {
    this.benefits.update((list) =>
      list.filter((_, i) => i !== index).map((b, i) => ({ ...b, sortOrder: i })),
    );
  }

  moveBenefit(index: number, dir: -1 | 1) {
    const list = [...this.benefits()];
    const target = index + dir;
    if (target < 0 || target >= list.length) return;
    [list[index], list[target]] = [list[target], list[index]];
    this.benefits.set(list.map((b, i) => ({ ...b, sortOrder: i })));
  }

  updateBenefit(index: number, text: string) {
    this.benefits.update((list) => list.map((b, i) => (i === index ? { ...b, text } : b)));
  }

  addCardHighlight() {
    this.cardHighlights.update((list) => [...list, { ...emptyTextRow(), sortOrder: list.length }]);
  }

  removeCardHighlight(index: number) {
    this.cardHighlights.update((list) =>
      list.filter((_, i) => i !== index).map((h, i) => ({ ...h, sortOrder: i })),
    );
  }

  moveCardHighlight(index: number, dir: -1 | 1) {
    const list = [...this.cardHighlights()];
    const target = index + dir;
    if (target < 0 || target >= list.length) return;
    [list[index], list[target]] = [list[target], list[index]];
    this.cardHighlights.set(list.map((h, i) => ({ ...h, sortOrder: i })));
  }

  updateCardHighlight(index: number, text: string) {
    this.cardHighlights.update((list) =>
      list.map((h, i) => (i === index ? { ...h, text } : h)),
    );
  }

  saveAsDraft() {
    this.form.update((f) => ({ ...f, isPublished: false }));
    void this.save();
  }

  saveAndPublish() {
    this.form.update((f) => ({ ...f, isPublished: true }));
    void this.save();
  }

  private buildRequest(f: SchemeFormData): UpsertSchemeRequest {
    const benefitItems: SchemeBenefitItem[] = this.benefits()
      .map((b, i) => ({ text: b.text.trim(), sortOrder: i }))
      .filter((b) => b.text);

    const highlightItems: SchemeCardHighlightItem[] = this.cardHighlights()
      .map((h, i) => ({ text: h.text.trim(), sortOrder: i }))
      .filter((h) => h.text);

    return {
      slug: f.slug.trim() || toSlug(f.name),
      name: f.name.trim(),
      crumb: f.crumb.trim() || f.name.trim().slice(0, 80),
      tagline: f.tagline.trim(),
      shortDescription: f.shortDescription.trim(),
      contentHtml: f.contentHtml,
      categorySlug: f.categorySlug,
      primaryBadgeText: this.categoryName(f.categorySlug),
      primaryBadgeClass: f.primaryBadgeClass,
      secondaryBadgeText: f.secondaryBadgeText.trim(),
      secondaryBadgeClass: f.secondaryBadgeClass,
      homeTitle: f.homeTitle.trim(),
      homeBadgeText: f.homeBadgeText.trim(),
      homeBadgeClass: f.homeBadgeClass,
      homeDescription: f.homeDescription.trim(),
      seoTitle: f.seoTitle.trim() || null,
      metaDescription: f.metaDescription.trim() || null,
      isPublished: f.isPublished,
      isFeatured: f.isFeatured,
      sortOrder: Number(f.sortOrder) || 0,
      benefits: benefitItems,
      cardHighlights: highlightItems,
    };
  }

  async save() {
    const raw = this.form();
    if (!raw.name.trim()) {
      this.toast.error('Name is required.');
      return;
    }
    if (!raw.categorySlug) {
      this.toast.error('Please choose a category.');
      return;
    }

    const f = {
      ...raw,
      name: raw.name.trim(),
      slug: raw.slug.trim() || toSlug(raw.name),
      crumb: raw.crumb.trim() || raw.name.trim().slice(0, 80),
    };
    this.form.set(f);

    if (!f.slug.trim()) {
      this.toast.error('URL slug is required.');
      return;
    }

    const req = this.buildRequest(f);
    this.saving.set(true);
    try {
      if (this.mode() === 'create') {
        const res = await firstValueFrom(this.api.create(req));
        if (res.success) {
          this.toast.success(f.isPublished ? 'Scheme published.' : 'Draft saved.');
          await this.router.navigate(['/admin/scheme-management']);
        } else {
          this.toast.error(res.message ?? 'Failed to create scheme.');
        }
      } else {
        const res = await firstValueFrom(this.api.update(this.schemeId()!, req));
        if (res.success) {
          this.toast.success(f.isPublished ? 'Scheme published.' : 'Draft saved.');
          await this.router.navigate(['/admin/scheme-management']);
        } else {
          this.toast.error(res.message ?? 'Failed to update scheme.');
        }
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to save scheme.'));
    } finally {
      this.saving.set(false);
    }
  }

  cancel() {
    void this.router.navigate(['/admin/scheme-management']);
  }
}
