import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  TeamMemberService,
  type UpsertTeamMemberRequest,
} from '../../../core/services/team-member.service';
import { ToastService } from '../../../core/services/toast.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

interface TeamFormData {
  name: string;
  designationHtml: string;
  photoUrl: string;
  sortOrder: number;
  isActive: boolean;
}

function emptyForm(): TeamFormData {
  return {
    name: '',
    designationHtml: '',
    photoUrl: '',
    sortOrder: 1,
    isActive: true,
  };
}

function nextSortOrder(sortOrders: number[]): number {
  if (!sortOrders.length) return 1;
  return Math.max(...sortOrders) + 1;
}

@Component({
  selector: 'app-team-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './team-form.html',
  styleUrls: ['../../admin-shared.css', './team-form.css'],
})
export class TeamForm implements OnInit {
  private readonly api = inject(TeamMemberService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly acceptImages = 'image/jpeg,image/png,image/webp,image/gif';
  readonly mode = signal<'create' | 'edit'>('create');
  readonly teamMemberId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly uploadingPhoto = signal(false);
  readonly form = signal<TeamFormData>(emptyForm());

  readonly previewName = computed(() => this.form().name.trim() || 'Team member name');
  readonly previewDesignation = computed(
    () => this.form().designationHtml.trim() || 'Role and designation will appear here.',
  );
  readonly hasPhoto = computed(() => !!this.form().photoUrl.trim());

  async ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('teamMemberId');
    if (idParam) {
      const id = Number(idParam);
      this.mode.set('edit');
      this.teamMemberId.set(id);
      await this.loadExisting(id);
    } else {
      await this.initCreateDefaults();
    }
  }

  private async initCreateDefaults() {
    try {
      this.loading.set(true);
      const res = await firstValueFrom(this.api.adminList());
      const orders = (res.members ?? []).map((m) => Number(m.sortOrder) || 0);
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
      if (!res.success || !res.member) {
        this.toast.error(res.message ?? 'Team member not found.');
        await this.router.navigate(['/admin/team-management']);
        return;
      }
      const m = res.member;
      this.form.set({
        name: m.name,
        designationHtml: m.designationHtml,
        photoUrl: m.photoUrl,
        sortOrder: m.sortOrder,
        isActive: m.isActive,
      });
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to load team member.'));
      await this.router.navigate(['/admin/team-management']);
    } finally {
      this.loading.set(false);
    }
  }

  updateField<K extends keyof TeamFormData>(field: K, value: TeamFormData[K]) {
    this.form.update((f) => ({ ...f, [field]: value }));
  }

  async onPhotoSelected(event: Event) {
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

    this.uploadingPhoto.set(true);
    try {
      const res = await firstValueFrom(this.api.uploadPhoto(file));
      if (res.success && res.photoUrl) {
        this.updateField('photoUrl', res.photoUrl);
        this.toast.success('Photo uploaded.');
      } else {
        this.toast.error(res.message ?? 'Failed to upload photo.');
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to upload photo.'));
    } finally {
      this.uploadingPhoto.set(false);
    }
  }

  clearPhoto() {
    this.updateField('photoUrl', '');
  }

  private buildRequest(f: TeamFormData): UpsertTeamMemberRequest {
    return {
      name: f.name.trim(),
      designationHtml: f.designationHtml.trim(),
      photoUrl: f.photoUrl.trim(),
      sortOrder: f.sortOrder,
      isActive: f.isActive,
    };
  }

  async save() {
    const f = this.form();
    if (!f.name.trim()) {
      this.toast.error('Name is required.');
      return;
    }
    if (!f.photoUrl.trim()) {
      this.toast.error('Photo is required. Upload an image or enter a photo URL.');
      return;
    }

    const req = this.buildRequest(f);
    this.saving.set(true);
    try {
      if (this.mode() === 'edit' && this.teamMemberId() != null) {
        const res = await firstValueFrom(this.api.update(this.teamMemberId()!, req));
        if (res.success) {
          this.toast.success(f.isActive ? 'Team member saved.' : 'Saved (inactive — hidden on site).');
          await this.router.navigate(['/admin/team-management']);
        } else {
          this.toast.error(res.message ?? 'Save failed.');
        }
      } else {
        const res = await firstValueFrom(this.api.create(req));
        if (res.success) {
          this.toast.success(f.isActive ? 'Team member created.' : 'Created (inactive — hidden on site).');
          await this.router.navigate(['/admin/team-management']);
        } else {
          this.toast.error(res.message ?? 'Save failed.');
        }
      }
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Save failed.'));
    } finally {
      this.saving.set(false);
    }
  }

  cancel() {
    void this.router.navigate(['/admin/team-management']);
  }
}
