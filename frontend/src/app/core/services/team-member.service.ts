import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface TeamMemberItem {
  teamMemberId: number;
  name: string;
  designationHtml: string;
  photoUrl: string;
  sortOrder: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface TeamMemberListResponse {
  success: boolean;
  message?: string;
  members?: TeamMemberItem[];
  total?: number;
}

export interface TeamMemberDetailResponse {
  success: boolean;
  message?: string;
  member?: TeamMemberItem;
}

export interface UpsertTeamMemberRequest {
  name: string;
  designationHtml: string;
  photoUrl: string;
  sortOrder: number;
  isActive: boolean;
}

export interface TeamMemberMutationResponse {
  success: boolean;
  message?: string;
  teamMemberId?: number;
}

export interface TeamPhotoUploadResponse {
  success: boolean;
  message?: string;
  photoUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class TeamMemberService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/team-members';
  private readonly adminBase = '/api/team-members/admin';

  listPublic(): Observable<TeamMemberListResponse> {
    return this.http.get<TeamMemberListResponse>(apiUrl(this.base));
  }

  adminList(opts?: { search?: string; status?: string }): Observable<TeamMemberListResponse> {
    let params = new HttpParams();
    if (opts?.search) params = params.set('search', opts.search);
    if (opts?.status) params = params.set('status', opts.status);
    return this.http.get<TeamMemberListResponse>(apiUrl(this.adminBase), { params });
  }

  adminGet(id: number): Observable<TeamMemberDetailResponse> {
    return this.http.get<TeamMemberDetailResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  create(req: UpsertTeamMemberRequest): Observable<TeamMemberMutationResponse> {
    return this.http.post<TeamMemberMutationResponse>(apiUrl(this.adminBase), req);
  }

  update(id: number, req: UpsertTeamMemberRequest): Observable<TeamMemberMutationResponse> {
    return this.http.put<TeamMemberMutationResponse>(apiUrl(`${this.adminBase}/${id}`), req);
  }

  setActive(id: number, isActive: boolean): Observable<TeamMemberMutationResponse> {
    return this.http.patch<TeamMemberMutationResponse>(apiUrl(`${this.adminBase}/${id}/active`), {
      isActive,
    });
  }

  reorder(orderedIds: number[]): Observable<TeamMemberMutationResponse> {
    return this.http.put<TeamMemberMutationResponse>(apiUrl(`${this.adminBase}/reorder`), {
      orderedIds,
    });
  }

  delete(id: number): Observable<TeamMemberMutationResponse> {
    return this.http.delete<TeamMemberMutationResponse>(apiUrl(`${this.adminBase}/${id}`));
  }

  uploadPhoto(file: File): Observable<TeamPhotoUploadResponse> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<TeamPhotoUploadResponse>(apiUrl(`${this.adminBase}/upload-photo`), form);
  }
}
