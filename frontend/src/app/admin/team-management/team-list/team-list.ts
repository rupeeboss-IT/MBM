import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  TeamMemberService,
  type TeamMemberItem,
} from '../../../core/services/team-member.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

function reorderMembers(list: TeamMemberItem[], fromIndex: number, toIndex: number): TeamMemberItem[] {
  if (fromIndex === toIndex || fromIndex < 0 || toIndex < 0 || fromIndex >= list.length || toIndex >= list.length) {
    return list;
  }
  const next = [...list];
  const [moved] = next.splice(fromIndex, 1);
  next.splice(toIndex, 0, moved);
  return next.map((m, i) => ({ ...m, sortOrder: i + 1 }));
}

@Component({
  selector: 'app-team-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './team-list.html',
  styleUrls: ['../../admin-shared.css', './team-list.css'],
})
export class TeamList implements OnInit {
  private readonly api = inject(TeamMemberService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly savingOrder = signal(false);
  readonly actionId = signal<number | null>(null);
  readonly members = signal<TeamMemberItem[]>([]);
  readonly search = signal('');
  readonly statusFilter = signal('');
  readonly dragIndex = signal<number | null>(null);
  readonly dragOverIndex = signal<number | null>(null);

  readonly canReorder = computed(() => !this.search().trim() && !this.statusFilter());
  readonly sortedMembers = computed(() => {
    const list = [...this.members()];
    list.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
    return list;
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
        }),
      );
      if (!res.success) {
        this.toast.error(res.message ?? 'Failed to load team members.');
        return;
      }
      this.members.set(res.members ?? []);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load team members.'));
    } finally {
      this.loading.set(false);
    }
  }

  async persistOrder(list: TeamMemberItem[]) {
    this.savingOrder.set(true);
    try {
      const orderedIds = list.map((m) => m.teamMemberId);
      const res = await firstValueFrom(this.api.reorder(orderedIds));
      if (res.success) {
        this.members.set(list);
        this.toast.success('Display order updated.');
      } else {
        this.toast.error(res.message ?? 'Failed to save order.');
        await this.load();
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to save order.'));
      await this.load();
    } finally {
      this.savingOrder.set(false);
    }
  }

  async moveMember(index: number, direction: -1 | 1) {
    if (!this.canReorder() || this.savingOrder()) return;
    const target = index + direction;
    const list = this.sortedMembers();
    if (target < 0 || target >= list.length) return;
    await this.persistOrder(reorderMembers(list, index, target));
  }

  onDragStart(index: number, event: DragEvent) {
    if (!this.canReorder() || this.savingOrder()) {
      event.preventDefault();
      return;
    }
    this.dragIndex.set(index);
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', String(index));
    }
  }

  onDragOver(index: number, event: DragEvent) {
    if (!this.canReorder() || this.dragIndex() === null) return;
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
    this.dragOverIndex.set(index);
  }

  onDragLeave() {
    this.dragOverIndex.set(null);
  }

  async onDrop(index: number, event: DragEvent) {
    event.preventDefault();
    const from = this.dragIndex();
    this.dragIndex.set(null);
    this.dragOverIndex.set(null);
    if (from === null || from === index || !this.canReorder()) return;
    await this.persistOrder(reorderMembers(this.sortedMembers(), from, index));
  }

  onDragEnd() {
    this.dragIndex.set(null);
    this.dragOverIndex.set(null);
  }

  async toggleActive(member: TeamMemberItem) {
    const newState = !member.isActive;
    this.actionId.set(member.teamMemberId);
    try {
      const res = await firstValueFrom(this.api.setActive(member.teamMemberId, newState));
      if (res.success) {
        this.members.update((list) =>
          list.map((m) =>
            m.teamMemberId === member.teamMemberId ? { ...m, isActive: newState } : m,
          ),
        );
        this.toast.success(newState ? 'Team member activated.' : 'Team member deactivated.');
      } else {
        this.toast.error(res.message ?? 'Action failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Action failed.'));
    } finally {
      this.actionId.set(null);
    }
  }

  async deleteMember(member: TeamMemberItem) {
    if (!confirm(`Delete "${member.name}"? This cannot be undone.`)) return;
    this.actionId.set(member.teamMemberId);
    try {
      const res = await firstValueFrom(this.api.delete(member.teamMemberId));
      if (res.success) {
        this.members.update((list) => list.filter((m) => m.teamMemberId !== member.teamMemberId));
        this.toast.success('Team member deleted.');
      } else {
        this.toast.error(res.message ?? 'Delete failed.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Delete failed.'));
    } finally {
      this.actionId.set(null);
    }
  }

  goToEdit(teamMemberId: number) {
    void this.router.navigate(['/admin/team-management/edit', teamMemberId]);
  }

  runSearch() {
    void this.load();
  }

  onStatusChange(v: string) {
    this.statusFilter.set(v);
    void this.load();
  }

  clearFilters() {
    this.search.set('');
    this.statusFilter.set('');
    void this.load();
  }

  statusBadge(isActive: boolean): string {
    return isActive ? 'badge-success' : 'badge-warning';
  }

  statusLabel(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }
}
