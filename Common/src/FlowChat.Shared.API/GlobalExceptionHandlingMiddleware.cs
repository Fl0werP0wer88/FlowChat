using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Shared.API;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                logger.LogWarning(
                    "Cannot return ProblemDetails for TraceId {TraceId} because the response has already started.",
                    context.TraceIdentifier);

                throw;
            }

            var problemDetailsFactory = context.RequestServices.GetRequiredService<ProblemDetailsFactory>();

            var problemDetails = problemDetailsFactory.CreateUnexpected(
                context,
                details: "An unexpected error occurred.");

            context.Response.Clear();
            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}

