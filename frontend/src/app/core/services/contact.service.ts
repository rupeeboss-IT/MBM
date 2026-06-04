import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface SubmitContactRequest {
  fullName: string;
  mobile: string;
  email: string;
  subjectId: number;
  message: string;
  consentAccepted: boolean;
}

export interface SubmitContactResponse {
  success: boolean;
  message?: string | null;
  submissionId?: number | null;
}

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/contact';

  submit(req: SubmitContactRequest): Observable<SubmitContactResponse> {
    return this.http.post<SubmitContactResponse>(`${this.baseUrl}/submit`, req);
  }
}
