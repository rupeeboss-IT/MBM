import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  EventCategoryService,
  type EventCategoryItem,
} from '../../../core/services/event-category.service';
import {
  EventCityService,
  type EventCityItem,
} from '../../../core/services/event-city.service';
import {
  EventManagementService,
  type EventHighlightItem,
  type EventPartnerItem,
  type UpsertEventRequest,
} from '../../../core/services/event-management.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { RichEditor } from '../../blog-management/blog-form/rich-editor/rich-editor';

interface HighlightRow {
  text: string;
  sortOrder: number;
}

interface PartnerRow {
  name: string;
  logoUrl: string;
  websiteUrl: string;
  sortOrder: number;
}

interface EventFormData {
  slug: string;
  name: string;
  crumb: string;
  tagline: string;
  shortDescription: string;
  aboutHtml: string;
  highlightsHtml: string;
  associationHtml: string;
  categorySlug: string;
  citySlug: string;
  featuredImageUrl: string;
  dateDisplayText: string;
  timeDisplayText: string;
  startDate: string;
  endDate: string;
  dateISO: string;
  attendanceMode: string;
  locationDisplayText: string;
  venueName: string;
  venueAddress: string;
  landmark: string;
  cityName: string;
  state: string;
  country: string;
  priceDisplay: string;
  regNote: string;
  seoTitle: string;
  metaDescription: string;
  isPublished: boolean;
  isFeatured: boolean;
  sortOrder: number;
}

function emptyForm(): EventFormData {
  return {
    slug: '',
    name: '',
    crumb: '',
    tagline: '',
    shortDescription: '',
    aboutHtml: '',
    highlightsHtml: '',
    associationHtml: '',
    categorySlug: '',
    citySlug: '',
    featuredImageUrl: '',
    dateDisplayText: '',
    timeDisplayText: '',
    startDate: '',
    endDate: '',
    dateISO: '',
    attendanceMode: '',
    locationDisplayText: '',
    venueName: '',
    venueAddress: '',
    landmark: '',
    cityName: '',
    state: '',
    country: 'India',
    priceDisplay: '',
    regNote: '',
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

function toDateInput(value: string | null | undefined): string {
  if (!value) return '';
  const s = String(value);
  if (/^\d{4}-\d{2}-\d{2}/.test(s)) return s.slice(0, 10);
  const d = new Date(s);
  if (Number.isNaN(d.getTime())) return '';
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function emptyHighlight(): HighlightRow {
  return { text: '', sortOrder: 0 };
}

function emptyPartner(): PartnerRow {
  return { name: '', logoUrl: '', websiteUrl: '', sortOrder: 0 };
}

@Component({
  selector: 'app-event-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RichEditor, RouterLink],
  templateUrl: './event-form.html',
  styleUrls: ['../../admin-shared.css', './event-form.css'],
})
export class EventForm implements OnInit {
  private readonly api = inject(EventManagementService);
  private readonly categoryApi = inject(EventCategoryService);
  private readonly cityApi = inject(EventCityService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly eventId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly uploadingImage = signal(false);
  readonly uploadingPartnerLogo = signal<number | null>(null);
  readonly showSeo = signal(false);
  readonly showHighlightsHtml = signal(false);
  readonly showAssociationHtml = signal(false);
  readonly form = signal<EventFormData>(emptyForm());
  readonly highlights = signal<HighlightRow[]>([]);
  readonly partners = signal<PartnerRow[]>([]);
  readonly categories = signal<EventCategoryItem[]>([]);
  readonly cities = signal<EventCityItem[]>([]);

  readonly acceptImages = 'image/jpeg,image/png,image/webp,image/gif,.jpg,.jpeg,.png,.webp,.gif';
  readonly attendanceModes = ['', 'Online', 'Offline', 'Hybrid'];

  async ngOnInit() {
    await Promise.all([this.loadCategories(), this.loadCities()]);

    const idParam = this.route.snapshot.paramMap.get('eventId');
    if (idParam) {
      const id = Number(idParam);
      this.mode.set('edit');
      this.eventId.set(id);
      await this.loadExisting(id);
    } else {
      const firstCat = this.categories()[0];
      if (firstCat) this.updateField('categorySlug', firstCat.slug);
    }
  }

  private async loadCategories() {
    try {
      const res = await firstValueFrom(this.categoryApi.adminList());
      const items = (res.categories ?? []).filter((c) => c.isActive);
      this.categories.set(items);
      if (!items.length) {
        this.toast.error('No active event categories. Add categories first.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load categories.'));
    }
  }

  private async loadCities() {
    try {
      const res = await firstValueFrom(this.cityApi.adminList());
      this.cities.set((res.cities ?? []).filter((c) => c.isActive));
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load cities.'));
    }
  }

  private async loadExisting(id: number) {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminGet(id));
      if (!res.success || !res.event) {
        this.toast.error(res.message ?? 'Event not found.');
        await this.router.navigate(['/admin/event-management']);
        return;
      }
      const e = res.event;
      this.form.set({
        slug: e.slug,
        name: e.name,
        crumb: e.crumb ?? '',
        tagline: e.tagline ?? '',
        shortDescription: e.shortDescription ?? '',
        aboutHtml: e.aboutHtml ?? '',
        highlightsHtml: e.highlightsHtml ?? '',
        associationHtml: e.associationHtml ?? '',
        categorySlug: e.categorySlug ?? '',
        citySlug: e.citySlug ?? '',
        featuredImageUrl: e.featuredImageUrl ?? '',
        dateDisplayText: e.dateDisplayText ?? '',
        timeDisplayText: e.timeDisplayText ?? '',
        startDate: toDateInput(e.startDate),
        endDate: toDateInput(e.endDate),
        dateISO: e.dateISO ?? '',
        attendanceMode: e.attendanceMode ?? '',
        locationDisplayText: e.locationDisplayText ?? '',
        venueName: e.venueName ?? '',
        venueAddress: e.venueAddress ?? '',
        landmark: e.landmark ?? '',
        cityName: e.cityNameField ?? e.cityName ?? '',
        state: e.state ?? '',
        country: e.country || 'India',
        priceDisplay: e.priceDisplay ?? '',
        regNote: e.regNote ?? '',
        seoTitle: e.seoTitle ?? '',
        metaDescription: e.metaDescription ?? '',
        isPublished: e.isPublished,
        isFeatured: e.isFeatured,
        sortOrder: e.sortOrder ?? 0,
      });

      this.highlights.set(
        (e.highlights ?? []).map((h, i) => ({
          text: h.text ?? '',
          sortOrder: h.sortOrder ?? i,
        })),
      );
      this.partners.set(
        (e.partners ?? []).map((p, i) => ({
          name: p.name ?? '',
          logoUrl: p.logoUrl ?? '',
          websiteUrl: p.websiteUrl ?? '',
          sortOrder: p.sortOrder ?? i,
        })),
      );

      if (e.seoTitle || e.metaDescription) this.showSeo.set(true);
      if (e.highlightsHtml?.trim()) this.showHighlightsHtml.set(true);
      if (e.associationHtml?.trim()) this.showAssociationHtml.set(true);

      // Ensure current category/city appear even if inactive
      if (e.categorySlug && !this.categories().some((c) => c.slug === e.categorySlug)) {
        this.categories.update((list) => [
          ...list,
          {
            eventCategoryId: 0,
            slug: e.categorySlug,
            name: e.categoryName || e.categorySlug,
            sortOrder: 0,
            isActive: true,
            showInFilter: true,
          },
        ]);
      }
      if (e.citySlug && !this.cities().some((c) => c.slug === e.citySlug)) {
        this.cities.update((list) => [
          ...list,
          {
            eventCityId: 0,
            slug: e.citySlug!,
            name: e.cityName || e.citySlug!,
            sortOrder: 0,
            isActive: true,
          },
        ]);
      }
    } catch (err: unknown) {
      this.toast.error(getHttpErrorMessage(err, 'Failed to load event.'));
      await this.router.navigate(['/admin/event-management']);
    } finally {
      this.loading.set(false);
    }
  }

  updateField<K extends keyof EventFormData>(field: K, value: EventFormData[K]) {
    this.form.update((f) => ({ ...f, [field]: value }));
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

  onCityChange(slug: string) {
    this.updateField('citySlug', slug);
    if (!slug) return;
    const city = this.cities().find((c) => c.slug === slug);
    if (city && !this.form().cityName.trim()) {
      this.updateField('cityName', city.name);
    }
  }

  async onFeaturedSelected(event: Event) {
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
        this.updateField('featuredImageUrl', res.imageUrl);
        this.toast.success('Featured image uploaded.');
      } else {
        this.toast.error(res.message ?? 'Failed to upload image.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to upload image.'));
    } finally {
      this.uploadingImage.set(false);
    }
  }

  clearFeatured() {
    this.updateField('featuredImageUrl', '');
  }

  addHighlight() {
    this.highlights.update((list) => [...list, { ...emptyHighlight(), sortOrder: list.length }]);
  }

  removeHighlight(index: number) {
    this.highlights.update((list) => list.filter((_, i) => i !== index).map((h, i) => ({ ...h, sortOrder: i })));
  }

  moveHighlight(index: number, dir: -1 | 1) {
    const list = [...this.highlights()];
    const target = index + dir;
    if (target < 0 || target >= list.length) return;
    [list[index], list[target]] = [list[target], list[index]];
    this.highlights.set(list.map((h, i) => ({ ...h, sortOrder: i })));
  }

  updateHighlight(index: number, text: string) {
    this.highlights.update((list) => list.map((h, i) => (i === index ? { ...h, text } : h)));
  }

  addPartner() {
    this.partners.update((list) => [...list, { ...emptyPartner(), sortOrder: list.length }]);
  }

  removePartner(index: number) {
    this.partners.update((list) => list.filter((_, i) => i !== index).map((p, i) => ({ ...p, sortOrder: i })));
  }

  updatePartner<K extends keyof PartnerRow>(index: number, field: K, value: PartnerRow[K]) {
    this.partners.update((list) =>
      list.map((p, i) => (i === index ? { ...p, [field]: value } : p)),
    );
  }

  async onPartnerLogoSelected(index: number, event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.toast.error('Please choose an image file.');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.toast.error('Image must be 5 MB or smaller.');
      return;
    }

    this.uploadingPartnerLogo.set(index);
    try {
      const res = await firstValueFrom(this.api.uploadImage(file));
      if (res.success && res.imageUrl) {
        this.updatePartner(index, 'logoUrl', res.imageUrl);
        this.toast.success('Partner logo uploaded.');
      } else {
        this.toast.error(res.message ?? 'Failed to upload logo.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to upload logo.'));
    } finally {
      this.uploadingPartnerLogo.set(null);
    }
  }

  saveAsDraft() {
    this.form.update((f) => ({ ...f, isPublished: false }));
    void this.save();
  }

  saveAndPublish() {
    this.form.update((f) => ({ ...f, isPublished: true }));
    void this.save();
  }

  private buildRequest(f: EventFormData): UpsertEventRequest {
    const highlightItems: EventHighlightItem[] = this.highlights()
      .map((h, i) => ({ text: h.text.trim(), sortOrder: i }))
      .filter((h) => h.text);

    const partnerItems: EventPartnerItem[] = this.partners()
      .map((p, i) => ({
        name: p.name.trim(),
        logoUrl: p.logoUrl.trim() || null,
        websiteUrl: p.websiteUrl.trim() || null,
        sortOrder: i,
      }))
      .filter((p) => p.name);

    return {
      slug: f.slug.trim() || toSlug(f.name),
      name: f.name.trim(),
      crumb: f.crumb.trim() || f.name.trim().slice(0, 80),
      tagline: f.tagline.trim(),
      shortDescription: f.shortDescription.trim(),
      aboutHtml: f.aboutHtml,
      highlightsHtml: f.highlightsHtml,
      associationHtml: f.associationHtml,
      categorySlug: f.categorySlug,
      citySlug: f.citySlug.trim() || null,
      featuredImageUrl: f.featuredImageUrl.trim() || null,
      bannerImageUrl: null,
      thumbnailUrl: null,
      dateDisplayText: f.dateDisplayText.trim(),
      timeDisplayText: f.timeDisplayText.trim(),
      startDate: f.startDate.trim() || null,
      endDate: f.endDate.trim() || null,
      dateISO: f.dateISO.trim() || null,
      attendanceMode: f.attendanceMode,
      locationDisplayText: f.locationDisplayText.trim(),
      venueName: f.venueName.trim(),
      venueAddress: f.venueAddress.trim(),
      landmark: f.landmark.trim(),
      cityName: f.cityName.trim(),
      state: f.state.trim(),
      country: f.country.trim() || 'India',
      mapsUrl: null,
      latitude: null,
      longitude: null,
      priceDisplay: f.priceDisplay.trim(),
      regNote: f.regNote.trim(),
      seoTitle: f.seoTitle.trim() || null,
      metaDescription: f.metaDescription.trim() || null,
      isPublished: f.isPublished,
      isFeatured: f.isFeatured,
      sortOrder: 0,
      highlights: highlightItems,
      partners: partnerItems,
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
    if (!raw.dateDisplayText.trim()) {
      this.toast.error('Date display text is required.');
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
          this.toast.success(f.isPublished ? 'Event published.' : 'Draft saved.');
          await this.router.navigate(['/admin/event-management']);
        } else {
          this.toast.error(res.message ?? 'Failed to create event.');
        }
      } else {
        const res = await firstValueFrom(this.api.update(this.eventId()!, req));
        if (res.success) {
          this.toast.success(f.isPublished ? 'Event published.' : 'Draft saved.');
          await this.router.navigate(['/admin/event-management']);
        } else {
          this.toast.error(res.message ?? 'Failed to update event.');
        }
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to save event.'));
    } finally {
      this.saving.set(false);
    }
  }

  cancel() {
    void this.router.navigate(['/admin/event-management']);
  }
}
