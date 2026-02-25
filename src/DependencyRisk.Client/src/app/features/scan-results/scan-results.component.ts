import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../shared/services/api.service';
import { DependencyRisk, ScanResult } from '../../shared/models/models';

@Component({
  selector: 'app-scan-results',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './scan-results.component.html',
  styleUrl: './scan-results.component.scss'
})
export class ScanResultsComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);

  scan: ScanResult | null = null;
  selected: DependencyRisk | null = null;
  loading = true;
  error = '';
  filter: 'All' | 'Critical' | 'High' | 'Medium' | 'Low' = 'All';

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getScan(id).subscribe({
      next: scan => { this.scan = scan; this.loading = false; },
      error: () => { this.error = 'Scan not found.'; this.loading = false; }
    });
  }

  get filtered(): DependencyRisk[] {
    if (!this.scan) return [];
    return this.filter === 'All'
      ? this.scan.dependencies
      : this.scan.dependencies.filter(d => d.riskLevel === this.filter);
  }

  tileColor(level: string): string {
    return { Low: '#22c55e', Medium: '#f59e0b', High: '#f97316', Critical: '#ef4444' }[level] ?? '#94a3b8';
  }

  radarData(dep: DependencyRisk) {
    return [
      { label: 'Maintainer', value: dep.maintainerScore },
      { label: 'Activity', value: dep.activityScore },
      { label: 'Issues', value: dep.issueHealthScore },
      { label: 'Releases', value: dep.releaseScore },
      { label: 'Community', value: dep.communityScore },
      { label: 'License', value: dep.licenseScore },
    ];
  }

  setFilter(level: string) {
    this.filter = level as any;
  }

  scoreBar(value: number): string {
    if (value >= 80) return '#22c55e';
    if (value >= 60) return '#f59e0b';
    if (value >= 40) return '#f97316';
    return '#ef4444';
  }
}
