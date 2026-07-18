import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  EventCityService,
  type UpsertEventCityRequest,
} from '../../../core/services/event-city.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

interface CityFormData {
  slug: string;
  name: string;
  badgeClass: string;
  sortOrder: number;
  isActive: boolean;
}

function emptyForm(): CityFormData {
  return {
    slug: '',
    name: '',
    badgeClass: '',
    sortOrder: 1,
    isActive: true,
  };
}

function toSlug(name: string): string {
  return name
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
  selector: 'app-event-city-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './event-city-form.html',
  styleUrls: ['../../admin-shared.css', './event-city-form.css'],
})
export class EventCityForm implements OnInit {
  private readonly api = inject(EventCityService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly cityId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly form = signal<CityFormData>(emptyForm());

  async ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('cityId');
    if (idParam) {
      const id = Number(idParam);
      this.mode.set('edit');
      this.cityId.set(id);
      await this.loadExisting(id);
    } else {
      await this.initCreateDefaults();
    }
  }

  private async initCreateDefaults() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminList());
      const orders = (res.cities ?? []).map((c) => Number(c.sortOrder) || 0);
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
      if (!res.success || !res.city) {
        this.toast.error(res.message ?? 'City not found.');
        await this.router.navigate(['/admin/event-cities']);
        return;
      }
      const c = res.city;
      this.form.set({
        slug: c.slug,
        name: c.name,
        badgeClass: c.badgeClass ?? '',
        sortOrder: c.sortOrder,
        isActive: c.isActive,
      });
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load city.'));
      await this.router.navigate(['/admin/event-cities']);
    } finally {
      this.loading.set(false);
    }
  }

  updateField<K extends keyof CityFormData>(field: K, value: CityFormData[K]) {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  onNameBlur() {
    const name = this.form().name.trim();
    if (!name) return;
    if (this.mode() === 'create' && !this.form().slug.trim()) {
      this.updateField('slug', toSlug(name));
    }
  }

  async save() {
    const f = this.form();
    if (!f.name.trim()) {
      this.toast.error('Name is required.');
      return;
    }
    if (!f.slug.trim()) {
      this.toast.error('Slug is required.');
      return;
    }

    const req: UpsertEventCityRequest = {
      slug: f.slug.trim(),
      name: f.name.trim(),
      badgeClass: f.badgeClass.trim() || null,
      sortOrder: Number(f.sortOrder) || 0,
      isActive: f.isActive,
    };

    this.saving.set(true);
    try {
      if (this.mode() === 'create') {
        const res = await firstValueFrom(this.api.create(req));
        if (res.success) {
          this.toast.success('City created.');
          await this.router.navigate(['/admin/event-cities']);
        } else {
          this.toast.error(res.message ?? 'Failed to create city.');
        }
      } else {
        const res = await firstValueFrom(this.api.update(this.cityId()!, req));
        if (res.success) {
          this.toast.success('City updated.');
          await this.router.navigate(['/admin/event-cities']);
        } else {
          this.toast.error(res.message ?? 'Failed to update city.');
        }
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to save city.'));
    } finally {
      this.saving.set(false);
    }
  }

  cancel() {
    void this.router.navigate(['/admin/event-cities']);
  }
}
