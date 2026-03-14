namespace SliplaneDeploy.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Captures ?repo= from the initial GET before Ivy SPA strips query params.
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
                var raw = context.Request.Query["repo"].ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var draft = DeploymentDraftStore.ParseGitHubUrl(Uri.UnescapeDataString(raw));
                    var store = context.RequestServices.GetRequiredService<DeploymentDraftStore>();
                    store.SaveDraft(draft);
                }

                await nextMiddleware(context);
            });

            next(app);
        };
    }
}
