namespace SliplaneManage.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Captures ?repo= from the initial GET /sliplane-deploy-app before Ivy SPA strips query params.
/// Stores it in <see cref="DeploymentDraftStore"/> so DeployView can pre-fill the form.
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
                    var repo = context.Request.Query["repo"].ToString();
                    if (!string.IsNullOrWhiteSpace(repo))
                    {
                        var store = context.RequestServices.GetRequiredService<DeploymentDraftStore>();
                        store.SaveRepoUrl(Uri.UnescapeDataString(repo));
                    }
                }

                await nextMiddleware(context);
            });

            next(app);
        };
    }
}
