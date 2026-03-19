namespace PrStagingDeploy.Services;

using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Adds POST /webhook endpoint for GitHub webhooks. Runs first in pipeline to bypass other middleware.
/// </summary>
public class WebhookEndpointFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (!context.Request.Path.StartsWithSegments("/webhook", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    await nextMiddleware(context);
                    return;
                }

                var handler = context.RequestServices.GetRequiredService<GitHubWebhookHandler>();
                var config = context.RequestServices.GetRequiredService<IConfiguration>();
                var secret = (config["GitHub:WebhookSecret"] ?? "").Trim();

                var eventType = context.Request.Headers["X-GitHub-Event"].FirstOrDefault() ?? "unknown";
                var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault() ?? "";

                context.Request.EnableBuffering();
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var payload = await reader.ReadToEndAsync();

                if (!string.IsNullOrEmpty(secret) && !handler.VerifySignature(payload, signature, secret))
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                await handler.HandleAsync(eventType, payload);
                context.Response.StatusCode = 200;
            });

            next(app);
        };
    }
}
