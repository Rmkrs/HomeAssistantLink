namespace HomeAssistantLink.Host.WebApi;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    public async Task Invoke(HttpContext context)
    {
        var configuredKey = config["HomeAssistantLink:Api:Key"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKey) ||
            providedKey != configuredKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await next(context);
    }
}
