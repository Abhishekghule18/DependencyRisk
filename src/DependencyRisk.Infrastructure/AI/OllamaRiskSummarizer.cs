using System.Net.Http.Json;
using System.Text.Json;
using DependencyRisk.Core.Entities;
using DependencyRisk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DependencyRisk.Infrastructure.AI;

public class OllamaRiskSummarizer : IAiSummarizer
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaRiskSummarizer> _logger;
    private readonly string _model;

    public OllamaRiskSummarizer(HttpClient http, IConfiguration config, ILogger<OllamaRiskSummarizer> logger)
    {
        _http = http;
        _logger = logger;
        var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _http.BaseAddress = new Uri(baseUrl);
        _model = config["Ollama:Model"] ?? "llama3";
    }

    public async Task<string> Summarize(string packageName, RiskScore score, RepoMetrics metrics)
    {
        try
        {
            var prompt = $"""
                You are a software supply chain security analyst.
                Analyze this dependency and give a 2-3 sentence risk summary in plain English.
                Be specific about what actions the team should take.

                Package: {packageName}
                Overall Risk: {score.RiskLevel} ({score.OverallScore}/100)
                Contributors: {metrics.ContributorCount}
                Last commit: {metrics.LastCommitDate:yyyy-MM-dd}
                Last release: {metrics.LastReleaseDate:yyyy-MM-dd}
                Open issues: {metrics.OpenIssues}
                Issue close ratio: {metrics.IssueCloseRatio:P0}
                License: {metrics.License ?? "None detected"}
                Archived: {metrics.Archived}
                Stars: {metrics.Stars}

                Respond with ONLY the risk summary, no preamble.
                """;

            var response = await _http.PostAsJsonAsync("/api/generate",
                new { model = _model, prompt, stream = false });

            if (!response.IsSuccessStatusCode)
                return $"Risk level: {score.RiskLevel}. Unable to generate AI summary (Ollama not available).";

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("response", out var r)
                ? r.GetString() ?? "Unable to generate summary."
                : "Unable to generate summary.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama summarizer failed for {Package}", packageName);
            return $"Risk level: {score.RiskLevel}. AI summary unavailable — ensure Ollama is running locally.";
        }
    }
}
