import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Authority, Category, CreateReportRequest, CreateReportResult, Municipality,
  PagedResult, PendingReport, ReportDetail, ReportListItem, ReportStatus,
} from './models';

export interface ReportFilter {
  municipalityId?: number;
  categoryId?: number;
  status?: ReportStatus;
  page?: number;
  pageSize?: number;
}

const tokenHeader = 'X-Report-Token';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1';

  municipalities() {
    return this.http.get<Municipality[]>(`${this.base}/municipalities`);
  }

  categories() {
    return this.http.get<Category[]>(`${this.base}/categories`);
  }

  authorities(municipalitySlug: string) {
    return this.http.get<Authority[]>(`${this.base}/municipalities/${municipalitySlug}/authorities`);
  }

  reports(filter: ReportFilter) {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value !== undefined && value !== null) params = params.set(key, String(value));
    }
    return this.http.get<PagedResult<ReportListItem>>(`${this.base}/reports`, { params });
  }

  report(id: string, trackingToken?: string | null) {
    const headers = trackingToken ? new HttpHeaders({ [tokenHeader]: trackingToken }) : undefined;
    return this.http.get<ReportDetail>(`${this.base}/reports/${id}`, { headers });
  }

  createReport(request: CreateReportRequest, photos: File[], captchaToken: string | null) {
    const form = new FormData();
    form.append('report', JSON.stringify(request));
    photos.forEach((p) => form.append('photos', p, p.name));
    if (captchaToken) form.append('captchaToken', captchaToken);
    return this.http.post<CreateReportResult>(`${this.base}/reports`, form);
  }

  pendingReports() {
    return this.http.get<PendingReport[]>(`${this.base}/admin/reports/pending`);
  }

  approveReport(id: string) {
    return this.http.post<void>(`${this.base}/admin/reports/${id}/approve`, null);
  }

  hideReport(id: string) {
    return this.http.post<void>(`${this.base}/admin/reports/${id}/hide`, null);
  }
}
