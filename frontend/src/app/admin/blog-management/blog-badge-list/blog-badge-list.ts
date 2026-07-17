import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { BlogBadgeService, type BlogBadgeItem } from '../../../core/services/blog-badge.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

@Component({
  selector: 'app-blog-badge-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './blog-badge-list.html',
  styleUrls: ['../../admin-shared.css', './blog-badge-list.css'],
})
export class BlogBadgeList implements OnInit {
  private readonly api = inject(BlogBadgeService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly actionId = signal<number | null>(null);
  readonly badges = signal<BlogBadgeItem[]>([]);

  async ngOnInit() {
    await this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminList());
      if (!res.success) {
        this.toast.error(res.message ?? 'Failed to load card labels.');
        return;
      }
      this.badges.set(res.badges ?? []);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load card labels.'));
    } finally {
      this.loading.set(false);
    }
  }

  edit(id: number) {
    void this.router.navigate(['/admin/blog-badges/edit', id]);
  }

  async remove(badge: BlogBadgeItem) {
    if (!confirm(`Delete card label "${badge.label}"? This cannot be undone.`)) return;

    this.actionId.set(badge.blogBadgeId);
    try {
      const res = await firstValueFrom(this.api.delete(badge.blogBadgeId));
      if (res.success) {
        this.badges.update((list) => list.filter((b) => b.blogBadgeId !== badge.blogBadgeId));
        this.toast.success('Card label deleted.');
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
