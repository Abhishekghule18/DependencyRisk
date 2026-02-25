export interface ProjectSummary {
  projectId: number;
  projectName: string;
  fileType: string;
  lastScannedAt: string | null;
  latestScore: number | null;
  totalDeps: number;
  highRiskCount: number;
}

export interface DependencyRisk {
  dependencyId: number;
  packageName: string;
  version: string | null;
  ecosystem: string;
  gitHubRepoUrl: string | null;
  maintainerScore: number;
  activityScore: number;
  issueHealthScore: number;
  releaseScore: number;
  communityScore: number;
  licenseScore: number;
  overallScore: number;
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  aiSummary: string | null;
}

export interface ScanResult {
  scanId: number;
  projectId: number;
  projectName: string;
  scannedAt: string;
  totalDeps: number;
  highRiskCount: number;
  mediumRiskCount: number;
  lowRiskCount: number;
  overallScore: number;
  dependencies: DependencyRisk[];
}

export interface DashboardSummary {
  totalProjects: number;
  totalDependencies: number;
  totalScans: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
}

export interface TrendPoint {
  scannedAt: string;
  overallScore: number;
  highRiskCount: number;
}

export interface WorstDependency {
  packageName: string;
  ecosystem: string;
  gitHubRepoUrl: string | null;
  overallScore: number;
  riskLevel: string;
  projectName: string;
  aiSummary: string | null;
}

export interface ScanSummary {
  id: number;
  scannedAt: string;
  totalDeps: number;
  highRiskCount: number;
  mediumRiskCount: number;
  lowRiskCount: number;
  overallScore: number;
}
