# DependencyRisk — Supply Chain Risk Scorer

> Predictive risk scoring for your dependencies — before something goes wrong.

## Quick Start

### 0. Initial Setup

Before running the application, configure your environment settings:

**Create local configuration files from templates:**

```bash
# Copy template to active config (first time only)
cp src/DependencyRisk.Api/appsettings.template.json src/DependencyRisk.Api/appsettings.json
cp src/DependencyRisk.Api/appsettings.Development.template.json src/DependencyRisk.Api/appsettings.Development.json
```

**Update credentials if needed:**

Edit `src/DependencyRisk.Api/appsettings.json` to configure:
- **GitHub Token** (optional): Replace `REPLACE_WITH_GITHUB_PAT` with your token to raise API rate limits
- **Database Connection**: Adjust `ConnectionStrings.Default` if using a different SQL Server instance
- **Ollama Settings**: Modify if running Ollama on a different host/port

> ⚠️ Configuration files (`appsettings*.json`) are git-ignored. Never commit real credentials.

### 1. Start the API

```bash
cd src/DependencyRisk.Api
dotnet run
# Swagger UI → http://localhost:5000/swagger
```

### 2. Start the Angular Frontend

```bash
cd src/DependencyRisk.Client
npm install
ng serve
# App → http://localhost:4200
```

### 3. (Optional) AI Summaries via Ollama

```bash
# Install from https://ollama.com, then:
ollama pull llama3
# Ollama runs on http://localhost:11434 by default
```

---

## Architecture

```
Angular Frontend (localhost:4200)
        │ REST
.NET 8 Web API (localhost:5000)
  ├── File Parsers (.csproj / package.json / requirements.txt)
  ├── GitHub Metrics Client (repo health, contributors, commits, releases, issues)
  ├── Weighted Risk Scorer (6 dimensions → 0-100 score)
  ├── AI Summarizer (Ollama / llama3 — runs locally, zero cost)
  └── Scan Orchestrator (parallel pipeline with rate limiting)
        │
SQL Server LocalDB
  Tables: Projects, Dependencies, Scans, RiskScores
```

## Risk Scoring Algorithm

| Dimension      | Weight | Signals |
|----------------|--------|---------|
| Maintainer     | 25%    | Contributor count, org-backed |
| Activity       | 25%    | Days since last commit, archived status |
| Issue Health   | 15%    | Open/closed ratio, avg response time |
| Releases       | 15%    | Days since last release |
| Community      | 10%    | Stars, forks |
| License        | 10%    | MIT/Apache=100, GPL=70, None=20 |

| Score | Risk Level |
|-------|-----------|
| 80–100 | Low |
| 60–79  | Medium |
| 40–59  | High |
| 0–39   | Critical |

## Supported File Types

- `.csproj` — NuGet packages
- `package.json` — npm packages
- `requirements.txt` — PyPI packages

## API Endpoints

```
POST /api/projects/upload          Upload dependency file
GET  /api/projects                 List all projects
GET  /api/projects/{id}            Project detail + latest scan
POST /api/scans/{projectId}        Trigger scan
GET  /api/scans/{scanId}           Full scan results
GET  /api/scans/{scanId}/heatmap   Heatmap data
GET  /api/scans/project/{id}       Scan history for project
GET  /api/dashboard/summary        Global stats
GET  /api/dashboard/trends/{id}    Score trend over time
GET  /api/dashboard/worst          Top 10 riskiest dependencies
```

## Project Structure

```
DependencyRisk/
├── src/
│   ├── DependencyRisk.Api/          # ASP.NET Core Web API
│   ├── DependencyRisk.Core/         # Entities, interfaces, DTOs
│   ├── DependencyRisk.Infrastructure/ # EF Core, GitHub client, parsers, scorer, AI
│   └── DependencyRisk.Client/       # Angular 19 frontend
└── DependencyRisk.sln
```
