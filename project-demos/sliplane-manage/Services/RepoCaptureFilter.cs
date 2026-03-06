namespace SliplaneManage.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Captures ?repo= from the initial GET /sliplane-deploy-app before Ivy SPA strips query params.
/// Parses GitHub URLs (including /tree/branch/subpath) and stores a <see cref="DeployDraft"/>.
/// </summary>
public class RepoCaptureFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Path.StartsWithSegments("/sliplane-deploy-app", StringComparison.OrdinalIgnoreCase))
                {
                    var raw = context.Request.Query["repo"].ToString();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        var draft = DeploymentDraftStore.ParseGitHubUrl(Uri.UnescapeDataString(raw));
                        var store = context.RequestServices.GetRequiredService<DeploymentDraftStore>();
                        store.SaveDraft(draft);
                    }
                }

                await nextMiddleware(context);
            });

            next(app);
        };
    }
}
