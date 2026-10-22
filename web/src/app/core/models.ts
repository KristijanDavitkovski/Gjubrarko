export type ReportStatus = 'New' | 'Received' | 'InProgress' | 'Resolved' | 'Rejected';
export type Severity = 'Low' | 'Medium' | 'High';

export interface Municipality {
  id: number;
  slug: string;
  nameMk: string;
  nameSq: string;
  nameEn: string;
  parentId: number | null;
  latitude: number | null;
  longitude: number | null;
}

export interface Category {
  id: number;
  code: string;
  nameMk: string;
  nameSq: string;
  nameEn: string;
  icon: string | null;
  defaultAuthorityTypeId: number;
}

export interface Authority {
  id: number;
  name: string;
  authorityTypeId: number;
  authorityType: string;
  isNational: boolean;
}

export interface ReportListItem {
  id: string;
  publicNumber: string;
  title: string;
  municipality: string;
  category: string;
  status: ReportStatus;
  latitude: number;
  longitude: number;
  thumbnailUrl: string | null;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ReportDetail {
  id: string;
  publicNumber: string;
  title: string;
  description: string | null;
  municipality: string;
  category: string;
  latitude: number;
  longitude: number;
  addressText: string | null;
  severity: Severity;
  status: ReportStatus;
  reporterName: string;
  createdAt: string;
  resolvedAt: string | null;
  photos: { id: string; url: string; thumbnailUrl: string; kind: string }[];
  assignments: { authorityId: number; authorityName: string; assignedAt: string; acceptedAt: string | null }[];
  timeline: { from: ReportStatus; to: ReportStatus; note: string | null; changedAt: string }[];
}

export interface CreateReportRequest {
  municipalityId: number;
  categoryId: number;
  title: string;
  description: string | null;
  latitude: number;
  longitude: number;
  addressText: string | null;
  severity: Severity;
  authorityIds: number[];
  anonymous: boolean;
  contactEmail: string | null;
}

export interface CreateReportResult {
  id: string;
  publicNumber: string;
  trackingToken: string | null;
}

export interface PendingReport {
  id: string;
  publicNumber: string;
  title: string;
  municipality: string;
  createdAt: string;
  photoCount: number;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  displayName: string;
  roles: string[];
}

export const statusLabels: Record<ReportStatus, string> = {
  New: 'Ново',
  Received: 'Примено',
  InProgress: 'Во тек',
  Resolved: 'Решено',
  Rejected: 'Одбиено',
};
