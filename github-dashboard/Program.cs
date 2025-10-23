using GitHubDashboard.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient<IGitHubApiService, GitHubApiService>();
builder.Services.AddMemoryCache();
builder.Services.AddLogging();

var app = builder.Build();

// Configure the application
app.UseIvy();

app.Run();
