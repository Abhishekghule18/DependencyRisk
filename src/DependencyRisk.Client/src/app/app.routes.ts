import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/upload/upload.component').then(m => m.UploadComponent) },
  { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'scan/:id', loadComponent: () => import('./features/scan-results/scan-results.component').then(m => m.ScanResultsComponent) },
  { path: 'history/:projectId', loadComponent: () => import('./features/scan-history/scan-history.component').then(m => m.ScanHistoryComponent) },
  { path: '**', redirectTo: '' }
];
