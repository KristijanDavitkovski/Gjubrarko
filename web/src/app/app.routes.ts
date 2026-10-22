import { Routes } from '@angular/router';
import { moderatorGuard } from './core/admin.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/map/reports-map').then((m) => m.ReportsMap) },
  { path: 'reports/new', loadComponent: () => import('./features/reports/create-report').then((m) => m.CreateReport) },
  { path: 'reports/:id', loadComponent: () => import('./features/reports/report-detail').then((m) => m.ReportDetail) },
  { path: 'login', loadComponent: () => import('./features/auth/login').then((m) => m.Login) },
  { path: 'register', loadComponent: () => import('./features/auth/register').then((m) => m.Register) },
  {
    path: 'admin/pending',
    canActivate: [moderatorGuard],
    loadComponent: () => import('./features/admin/pending-reports').then((m) => m.PendingReports),
  },
  { path: '**', redirectTo: '' },
];
