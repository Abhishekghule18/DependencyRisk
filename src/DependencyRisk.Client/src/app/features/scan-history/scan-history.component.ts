import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../shared/services/api.service';
import { ScanSummary, TrendPoint } from '../../shared/models/models';

@Component({
  selector: 'app-scan-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './scan-history.component.html',
  styleUrl: './scan-history.component.scss'
})
export class ScanHistoryComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);

  projectId = 0;
  scans: ScanSummary[] = [];
  trends: TrendPoint[] = [];
  loading = true;

  ngOnInit() {
    this.projectId = Number(this.route.snapshot.paramMap.get('projectId'));
    Promise.all([
      this.api.getScansForProject(this.projectId).toPromise(),
      this.api.getTrends(this.projectId).toPromise()
    ]).then(([scans, trends]) => {
      this.scans = scans!;
      this.trends = trends!;
      this.loading = false;
    });
  }

  scoreColor(score: number): string {
    if (score >= 80) return '#22c55e';
    if (score >= 60) return '#f59e0b';
    if (score >= 40) return '#f97316';
    return '#ef4444';
  }

  // Simple inline SVG sparkline
  sparklinePath(): string {
    if (this.trends.length < 2) return '';
    const w = 300, h = 60, pad = 4;
    const scores = this.trends.map(t => t.overallScore);
    const min = Math.min(...scores), max = Math.max(...scores);
    const range = max - min || 1;
    const pts = scores.map((s, i) => {
      const x = pad + (i / (scores.length - 1)) * (w - 2 * pad);
      const y = h - pad - ((s - min) / range) * (h - 2 * pad);
      return `${x},${y}`;
    });
    return `M${pts.join(' L')}`;
  }
}
