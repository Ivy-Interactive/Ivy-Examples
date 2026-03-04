namespace SliplaneManage.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

/// <summary>
/// ASP.NET Core startup filter that captures the ?repo= query parameter
/// from the initial HTTP GET request for /sliplane-deploy-app before the
/// Ivy SPA takes over and strips query params from the WebSocket connection.
/// Stores the value in DeploymentDraftStore so DeployView can pre-fill the form.
/// </summary>
public class RepoCaptureFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Path.StartsWithSegments("/sliplane-deploy-app",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var repo = context.Request.Query["repo"].ToString();
                    if (!string.IsNullOrWhiteSpace(repo))
                        DeploymentDraftStore.SaveRepoUrl(Uri.UnescapeDataString(repo));
                }

                await nextMiddleware(context);
            });

            next(app);
        };
    }
}
