using GitHubDashboard.Apps;
using GitHubDashboard.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient<IGitHubApiService, GitHubApiService>();
builder.Services.AddMemoryCache();
builder.Services.AddLogging();

// Configure GitHub API settings
builder.Services.Configure<GitHubApiSettings>(builder.Configuration.GetSection("GitHub"));

var app = builder.Build();

// Configure the application
app.UseIvy();

// Register the main dashboard application
app.MapIvyApp<GitHubDashboardApp>();

app.Run();

public class GitHubApiSettings
{
    public string? ApiToken { get; set; }
    public string RepositoryOwner { get; set; } = "Ivy-Interactive";
    public string RepositoryName { get; set; } = "Ivy-Framework";
}
