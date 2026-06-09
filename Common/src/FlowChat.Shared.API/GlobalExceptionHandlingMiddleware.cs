using FlowChat.Core.Exceptions;
using FlowChat.Shared.Domain;
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

            // Preserve the IsolableException category across the HTTP boundary so consumers can route
            // the message to per-item retry instead of treating it as a permanent failure.
            var failureKind = exception is IsolableException ? FailureKind.Isolable : FailureKind.None;

            var problemDetails = problemDetailsFactory.CreateUnexpected(
                context,
                details: "An unexpected error occurred.",
                failureKind: failureKind);

            context.Response.Clear();
            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}

