using System.Text.Json;
using System.Text.Json.Serialization;
using DependencyRisk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DependencyRisk.Infrastructure.GitHub;

public class GitHubMetricsClient : IGitHubClient
{
    private readonly HttpClient _http;
    private readonly ILogger<GitHubMetricsClient> _logger;

    public GitHubMetricsClient(HttpClient http, IConfiguration config, ILogger<GitHubMetricsClient> logger)
    {
        _http = http;
        _logger = logger;

        _http.BaseAddress = new Uri("https://api.github.com");
        _http.DefaultRequestHeaders.Add("User-Agent", "DependencyRisk/1.0");
        _http.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        var token = config["GitHub:Token"];
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Add("Authorization", $"token {token}");
    }

    public async Task<RepoMetrics?> GetMetrics(string owner, string repo)
    {
        try
        {
            var repoData = await GetAsync<GitHubRepo>($"/repos/{owner}/{repo}");
            if (repoData == null) return null;

            var contributors = await GetAsync<List<GitHubContributor>>(
                $"/repos/{owner}/{repo}/contributors?per_page=100") ?? new();
            var commits = await GetAsync<List<GitHubCommit>>(
                $"/repos/{owner}/{repo}/commits?per_page=1") ?? new();
            var releases = await GetAsync<List<GitHubRelease>>(
                $"/repos/{owner}/{repo}/releases?per_page=5") ?? new();
            var issues = await GetAsync<List<GitHubIssue>>(
                $"/repos/{owner}/{repo}/issues?state=all&per_page=100") ?? new();

            return new RepoMetrics(
                Stars: repoData.StargazersCount,
                Forks: repoData.ForksCount,
                OpenIssues: repoData.OpenIssuesCount,
                IsOrgOwned: repoData.Owner?.Type == "Organization",
                ContributorCount: contributors.Count,
                LastCommitDate: commits.FirstOrDefault()?.Commit?.Author?.Date,
                LastReleaseDate: releases.FirstOrDefault()?.PublishedAt,
                License: repoData.License?.SpdxId,
                Archived: repoData.Archived,
                IssueCloseRatio: CalculateCloseRatio(issues),
                AvgIssueResponseHours: CalculateAvgResponseTime(issues)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get GitHub metrics for {Owner}/{Repo}", owner, repo);
            return null;
        }
    }

    public async Task<string?> ResolveGitHubUrl(string packageName, string ecosystem)
    {
        try
        {
            return ecosystem switch
            {
                "nuget" => await ResolveNuGet(packageName),
                "npm" => await ResolveNpm(packageName),
                "pypi" => await ResolvePyPi(packageName),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve GitHub URL for {Package} ({Ecosystem})", packageName, ecosystem);
            return null;
        }
    }

    private async Task<string?> ResolveNuGet(string packageName)
    {
        using var nugetHttp = new HttpClient();
        nugetHttp.DefaultRequestHeaders.Add("User-Agent", "DependencyRisk/1.0");
        var url = $"https://api.nuget.org/v3/registration5-gz-semver2/{packageName.ToLower()}/index.json";
        var json = await nugetHttp.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            var lastPage = items[items.GetArrayLength() - 1];
            if (lastPage.TryGetProperty("items", out var pageItems) && pageItems.GetArrayLength() > 0)
            {
                var lastItem = pageItems[pageItems.GetArrayLength() - 1];
                if (lastItem.TryGetProperty("catalogEntry", out var entry) &&
                    entry.TryGetProperty("projectUrl", out var projectUrl))
                {
                    var urlStr = projectUrl.GetString();
                    if (!string.IsNullOrEmpty(urlStr) && urlStr.Contains("github.com"))
                        return NormalizeGitHubUrl(urlStr);
                }
            }
        }
        return null;
    }

    private async Task<string?> ResolveNpm(string packageName)
    {
        using var npmHttp = new HttpClient();
        npmHttp.DefaultRequestHeaders.Add("User-Agent", "DependencyRisk/1.0");
        var json = await npmHttp.GetStringAsync($"https://registry.npmjs.org/{packageName}");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("repository", out var repo))
        {
            string? repoUrl = null;
            if (repo.ValueKind == JsonValueKind.Object && repo.TryGetProperty("url", out var urlProp))
                repoUrl = urlProp.GetString();
            else if (repo.ValueKind == JsonValueKind.String)
                repoUrl = repo.GetString();

            if (repoUrl != null && repoUrl.Contains("github.com"))
                return NormalizeGitHubUrl(repoUrl);
        }
        return null;
    }

    private async Task<string?> ResolvePyPi(string packageName)
    {
        using var pypiHttp = new HttpClient();
        pypiHttp.DefaultRequestHeaders.Add("User-Agent", "DependencyRisk/1.0");
        var json = await pypiHttp.GetStringAsync($"https://pypi.org/pypi/{packageName}/json");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("info", out var info) &&
            info.TryGetProperty("project_urls", out var urls))
        {
            foreach (var prop in urls.EnumerateObject())
            {
                var val = prop.Value.GetString();
                if (val != null && val.Contains("github.com"))
                    return NormalizeGitHubUrl(val);
            }
        }
        return null;
    }

    private static string NormalizeGitHubUrl(string url)
    {
        url = url.Replace("git+", "").Replace("git://", "https://")
                 .Replace("ssh://git@", "https://").TrimEnd('/');
        if (url.EndsWith(".git")) url = url[..^4];
        if (url.StartsWith("github:"))
            url = "https://github.com/" + url[7..];
        return url;
    }

    private static double CalculateCloseRatio(List<GitHubIssue> issues)
    {
        if (issues.Count == 0) return 0.5;
        var closed = issues.Count(i => i.State == "closed");
        return (double)closed / issues.Count;
    }

    private static double CalculateAvgResponseTime(List<GitHubIssue> issues)
    {
        var withResponse = issues
            .Where(i => i.ClosedAt.HasValue)
            .Select(i => (i.ClosedAt!.Value - i.CreatedAt).TotalHours)
            .ToList();
        return withResponse.Count > 0 ? withResponse.Average() : 720;
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }
}

// GitHub API response models
public record GitHubOwner([property: JsonPropertyName("type")] string? Type);
public record GitHubLicense([property: JsonPropertyName("spdx_id")] string? SpdxId);

public record GitHubRepo(
    [property: JsonPropertyName("stargazers_count")] int StargazersCount,
    [property: JsonPropertyName("forks_count")] int ForksCount,
    [property: JsonPropertyName("open_issues_count")] int OpenIssuesCount,
    [property: JsonPropertyName("owner")] GitHubOwner? Owner,
    [property: JsonPropertyName("license")] GitHubLicense? License,
    [property: JsonPropertyName("archived")] bool Archived
);

public record GitHubContributor([property: JsonPropertyName("login")] string Login);

public record GitHubCommitAuthor([property: JsonPropertyName("date")] DateTime? Date);
public record GitHubCommitDetail([property: JsonPropertyName("author")] GitHubCommitAuthor? Author);
public record GitHubCommit([property: JsonPropertyName("commit")] GitHubCommitDetail? Commit);

public record GitHubRelease([property: JsonPropertyName("published_at")] DateTime? PublishedAt);

public record GitHubIssue(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("closed_at")] DateTime? ClosedAt
);
