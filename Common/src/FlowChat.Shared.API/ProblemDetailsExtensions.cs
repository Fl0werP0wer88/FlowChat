using System.Text;
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
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status404NotFound, context, details, errors);

    public static ProblemDetails CreateBadRequest(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status400BadRequest, context, details, errors);

    public static ProblemDetails CreateConflict(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status409Conflict, context, details, errors);

    public static ProblemDetails CreateConcurrencyConflict(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(
            detailsFactory,
            StatusCodes.Status409Conflict,
            context,
            details,
            errors,
            "concurrency_conflict");

    public static ProblemDetails CreateValidation(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status400BadRequest, context, details, errors);

    public static ProblemDetails CreateUnauthorized(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status401Unauthorized, context, details, errors);

    public static ProblemDetails CreateUnexpected(
        this ProblemDetailsFactory detailsFactory,
        HttpContext context,
        string? details = null,
        IEnumerable<string>? errors = null) =>
        CreateProblemDetailsWith(detailsFactory, StatusCodes.Status500InternalServerError, context, details, errors);

    private static ProblemDetails CreateProblemDetailsWith(
        ProblemDetailsFactory detailsFactory,
        int statusCode,
        HttpContext context,
        string? message = null,
        IEnumerable<string>? errors = null,
        string? errorTag = null)
    {
        ProblemDetails problemDetails;

        if (errors is not null && errors.Any())
        {
            var errorList = new StringBuilder();
            errorList.AppendJoin(",", errors);

            problemDetails = detailsFactory.CreateProblemDetails(context, statusCode: statusCode, detail: errorList.ToString());
        }
        else
        {
            problemDetails = detailsFactory.CreateProblemDetails(context, statusCode: statusCode, detail: message);
        }

        if (!string.IsNullOrWhiteSpace(errorTag))
        {
            problemDetails.Extensions["error"] = errorTag;
        }

        return problemDetails;
    }
}

