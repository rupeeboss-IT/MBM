import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { BlogManagementService, type BlogListItem } from '../../../core/services/blog-management.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';
import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';
import { AdminPagination } from '../../../core/components/admin-pagination/admin-pagination';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-blog-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocalDatePipe, AdminPagination],
  templateUrl: './blog-list.html',
  styleUrls: ['../../admin-shared.css', './blog-list.css'],
})
export class BlogList implements OnInit {
  private readonly api = inject(BlogManagementService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly actionId = signal<number | null>(null);
  readonly blogs = signal<BlogListItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly search = signal('');
  readonly statusFilter = signal('');
  readonly pageSize = PAGE_SIZE;

  readonly pagedBlogs = computed(() => {
    const start = (this.page() - 1) * PAGE_SIZE;
    return this.blogs().slice(start, start + PAGE_SIZE);
  });

  async ngOnInit() {
    await this.load();
  }

  async load() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(
        this.api.adminList({
          search: this.search().trim() || undefined,
          status: this.statusFilter() || undefined,
          page: 1,
          pageSize: 200,
        }),
      );
      if (!res.success) {
        this.toast.error(res.message ?? 'Failed to load blogs.');
        return;
      }
      this.blogs.set(res.blogs ?? []);
      this.total.set(res.blogs?.length ?? 0);
      this.page.set(1);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load blogs.'));
    } finally {
      this.loading.set(false);
    }
  }

  async togglePublish(blog: BlogListItem) {
    const newState = !blog.isPublished;
    this.actionId.set(blog.blogId);
    try {
      const res = await firstValueFrom(this.api.setPublished(blog.blogId, newState));
      if (res.success) {
        this.blogs.update((list) =>
          list.map((b) => (b.blogId === blog.blogId ? { ...b, isPublished: newState } : b)),
        );
        this.toast.success(newState ? 'Blog published.' : 'Blog unpublished (draft).');
      } else {
        this.toast.error(res.message ?? 'Action failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Action failed.'));
    } finally {
      this.actionId.set(null);
    }
  }

  async deleteBlog(blog: BlogListItem) {
    if (!confirm(`Delete "${blog.title}"? This cannot be undone.`)) return;
    this.actionId.set(blog.blogId);
    try {
      const res = await firstValueFrom(this.api.delete(blog.blogId));
      if (res.success) {
        this.blogs.update((list) => list.filter((b) => b.blogId !== blog.blogId));
        this.total.update((t) => t - 1);
        this.toast.success('Blog deleted.');
      } else {
        this.toast.error(res.message ?? 'Delete failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Delete failed.'));
    } finally {
      this.actionId.set(null);
    }
  }

  goToEdit(blogId: number) {
    void this.router.navigate(['/admin/blog-management/edit', blogId]);
  }

  onPageChange(p: number) {
    this.page.set(p);
  }

  runSearch() {
    void this.load();
  }

  onStatusChange(v: string) {
    this.statusFilter.set(v);
    void this.load();
  }

  publishBadge(isPublished: boolean): string {
    return isPublished ? 'badge-success' : 'badge-warning';
  }

  publishLabel(isPublished: boolean): string {
    return isPublished ? 'Published' : 'Draft';
  }
}
