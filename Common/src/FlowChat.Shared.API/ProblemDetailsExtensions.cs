using System.Text;
using FlowChat.Core.Http;
using FlowChat.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FlowChat.Shared.API;

public static class ProblemDetailsExtensions
{
    public static ProblemDetails CreateNotFound(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        CreateProblemDetailsWith(
            detailsFactory,
            StatusCodes.Status404NotFound,
            context,
            details,
            errors,
            failureKind);

    public static ProblemDetails CreateBadRequest(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        CreateProblemDetailsWith(
            detailsFactory,
            StatusCodes.Status400BadRequest,
            context,
            details,
            errors,
            failureKind);

    public static ProblemDetails CreateConflict(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        CreateProblemDetailsWith(
            detailsFactory,
            StatusCodes.Status409Conflict,
            context,
            details,
            errors,
            failureKind);

    public static ProblemDetails CreateValidation(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        CreateProblemDetailsWith(
            detailsFactory,
            StatusCodes.Status400BadRequest,
            context,
            details,
            errors,
            failureKind);

    public static ProblemDetails CreateUnauthorized(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        CreateProblemDetailsWith(
            detailsFactory,
            StatusCodes.Status401Unauthorized,
            context,
            details,
            errors,
            failureKind);

    public static ProblemDetails CreateUnexpected(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        CreateProblemDetailsWith(
            detailsFactory,
            StatusCodes.Status500InternalServerError,
            context,
            details,
            errors,
            failureKind);

    private static ProblemDetails CreateProblemDetailsWith(
        ProblemDetailsFactory detailsFactory,
        int statusCode,
        HttpContext context,
        string? message = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None,
        string? errorTag = null)
    {
        ProblemDetails problemDetails;

        if (errors is not null && errors.Any())
        {
            var errorList = new StringBuilder();
            errorList.AppendJoin(",", errors);

            problemDetails = detailsFactory.CreateProblemDetails(
                context,
                statusCode: statusCode,
                detail: errorList.ToString());
        }
        else
        {
            problemDetails = detailsFactory.CreateProblemDetails(context, statusCode: statusCode, detail: message);
        }

        if (!string.IsNullOrWhiteSpace(errorTag))
        {
            problemDetails.Extensions["error"] = errorTag;
        }

        problemDetails.Extensions[ProblemDetailsExtensionNames.FailureKind] = failureKind.ToString();

        return problemDetails;
    }
}
