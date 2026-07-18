import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { EventCityService, type EventCityItem } from '../../../core/services/event-city.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-event-city-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './event-city-list.html',
  styleUrls: ['../../admin-shared.css', './event-city-list.css'],
})
export class EventCityList implements OnInit {
  private readonly api = inject(EventCityService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly actionId = signal<number | null>(null);
  readonly cities = signal<EventCityItem[]>([]);

  async ngOnInit() {
    await this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminList());
      if (!res.success) {
        this.toast.error(res.message ?? 'Failed to load cities.');
        return;
      }
      this.cities.set(res.cities ?? []);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load cities.'));
    } finally {
      this.loading.set(false);
    }
  }

  edit(id: number) {
    void this.router.navigate(['/admin/event-cities/edit', id]);
  }

  async remove(city: EventCityItem) {
    if (!confirm(`Delete city "${city.name}"? This cannot be undone.`)) return;

    this.actionId.set(city.eventCityId);
    try {
      const res = await firstValueFrom(this.api.delete(city.eventCityId));
      if (res.success) {
        this.cities.update((list) => list.filter((c) => c.eventCityId !== city.eventCityId));
        this.toast.success('City deleted.');
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
