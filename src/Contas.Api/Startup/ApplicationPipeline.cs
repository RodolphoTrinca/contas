using Contas.Api.Http;
using Scalar.AspNetCore;
using Serilog;

namespace Contas.Api.Startup;

internal static class ApplicationPipeline
{
    internal static void ConfigureRequestPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseWhen(http => http.Request.Path.StartsWithSegments("/api"), branch =>
        {
            branch.UseExceptionHandler(errorApp => errorApp.Run(http =>
                Responses.Fail(http, StatusCodes.Status500InternalServerError, "service.internal_error", "Falha interna.").ExecuteAsync(http)));
            branch.UseStatusCodePages(context => Responses.Status(context.HttpContext));
        });
        app.UseAuthentication();
        app.UseAuthorization();
    }

    internal static void MapApplicationEndpoints(this WebApplication app)
    {
        app.MapV1();
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapGet("/health", () => Results.Ok()).ExcludeFromDescription();
    }
}
