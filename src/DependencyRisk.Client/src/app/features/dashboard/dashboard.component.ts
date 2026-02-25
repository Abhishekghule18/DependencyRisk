import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApiService } from '../../shared/services/api.service';
import { DashboardSummary, ProjectSummary, WorstDependency } from '../../shared/models/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private api = inject(ApiService);

  summary: DashboardSummary | null = null;
  projects: ProjectSummary[] = [];
  worstDeps: WorstDependency[] = [];
  loading = true;
  scanningProjectId: number | null = null;
  error = '';

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    Promise.all([
      this.api.getDashboardSummary().toPromise(),
      this.api.getProjects().toPromise(),
      this.api.getWorstDeps().toPromise()
    ]).then(([summary, projects, worst]) => {
      this.summary = summary!;
      this.projects = projects!;
      this.worstDeps = worst!;
      this.loading = false;
    }).catch(err => {
      this.error = 'Failed to load dashboard. Is the API running?';
      this.loading = false;
    });
  }

  triggerScan(projectId: number) {
    this.scanningProjectId = projectId;
    this.api.triggerScan(projectId).subscribe({
      next: () => {
        this.scanningProjectId = null;
        this.load();
      },
      error: () => {
        this.scanningProjectId = null;
      }
    });
  }

  deleteProject(projectId: number) {
    if (!confirm('Delete this project and all its scan history?')) return;
    this.api.deleteProject(projectId).subscribe(() => this.load());
  }

  riskColor(level: string): string {
    return { Low: '#22c55e', Medium: '#f59e0b', High: '#f97316', Critical: '#ef4444' }[level] ?? '#94a3b8';
  }

  scoreGradient(score: number | null): string {
    if (score == null) return '#94a3b8';
    if (score >= 80) return '#22c55e';
    if (score >= 60) return '#f59e0b';
    if (score >= 40) return '#f97316';
    return '#ef4444';
  }
}
