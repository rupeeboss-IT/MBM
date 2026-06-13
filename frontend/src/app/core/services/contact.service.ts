import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../utils/api-url';

export interface SubmitContactRequest {
  fullName: string;
  mobile: string;
  email: string;
  subjectId: number;
  message: string;
  consentAccepted: boolean;
}

export interface SubmitCallbackRequest {
  fullName: string;
  mobile: string;
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

  submit(req: SubmitContactRequest): Observable<SubmitContactResponse> {
    return this.http.post<SubmitContactResponse>(apiUrl('/api/contact/submit'), req);
  }

  submitCallback(req: SubmitCallbackRequest): Observable<SubmitContactResponse> {
    return this.http.post<SubmitContactResponse>(apiUrl('/api/contact/callback'), req);
  }
}
