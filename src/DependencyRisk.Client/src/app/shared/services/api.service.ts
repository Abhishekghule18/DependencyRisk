import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  DashboardSummary,
  ProjectSummary,
  ScanResult,
  ScanSummary,
  TrendPoint,
  WorstDependency
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = 'http://localhost:5000/api';

  // Projects
  uploadFile(file: File): Observable<any> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post(`${this.base}/projects/upload`, form);
  }

  getProjects(): Observable<ProjectSummary[]> {
    return this.http.get<ProjectSummary[]>(`${this.base}/projects`);
  }

  getProject(id: number): Observable<any> {
    return this.http.get(`${this.base}/projects/${id}`);
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/projects/${id}`);
  }

  // Scans
  triggerScan(projectId: number): Observable<ScanResult> {
    return this.http.post<ScanResult>(`${this.base}/scans/${projectId}`, {});
  }

  getScan(scanId: number): Observable<ScanResult> {
    return this.http.get<ScanResult>(`${this.base}/scans/${scanId}`);
  }

  getHeatmap(scanId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/scans/${scanId}/heatmap`);
  }

  getScansForProject(projectId: number): Observable<ScanSummary[]> {
    return this.http.get<ScanSummary[]>(`${this.base}/scans/project/${projectId}`);
  }

  // Dashboard
  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.base}/dashboard/summary`);
  }

  getTrends(projectId: number): Observable<TrendPoint[]> {
    return this.http.get<TrendPoint[]>(`${this.base}/dashboard/trends/${projectId}`);
  }

  getWorstDeps(): Observable<WorstDependency[]> {
    return this.http.get<WorstDependency[]>(`${this.base}/dashboard/worst`);
  }
}
