using ClinicSpace.Application.Common;
using ClinicSpace.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSpace.Api.Errors;

/// <summary>
/// Turns exceptions into RFC 9457 problem details. Expected errors get a clear message and status;
/// unexpected ones are logged with the trace id and return a generic message with no internals.
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => ToValidationProblem(validation),
            NotFoundException notFound => Problem(StatusCodes.Status404NotFound, "Not found", notFound.Message, "not_found"),
            DomainException domain => Problem(StatusCodes.Status409Conflict, "Conflict", domain.Message, domain.Code),
            ConcurrencyConflictException conflict => Problem(StatusCodes.Status409Conflict, "Conflict", conflict.Message, "concurrency_conflict"),
            AntiforgeryValidationException => Problem(StatusCodes.Status400BadRequest, "Bad request", "Your session security token is missing or out of date. Refresh the page and try again.", "csrf_invalid"),
            BadHttpRequestException bad => Problem(bad.StatusCode, "Bad request", "The request could not be read.", "bad_request"),
            UnauthorizedAccessException => Problem(StatusCodes.Status401Unauthorized, "Unauthorised", "You need to sign in.", "unauthorised"),
            _ => Problem(StatusCodes.Status500InternalServerError, "Something went wrong", "Sorry, there is a problem with the service. Try again later.", "server_error"),
        };

        if (problem.Status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request failed with {Status}: {Message}", problem.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        return problem;
    }

    private static ValidationProblemDetails ToValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "There is a problem",
            Detail = "Some of the information you entered is not valid.",
        };
        problem.Extensions["code"] = "validation_failed";
        return problem;
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? string.Empty : char.ToLowerInvariant(name[0]) + name[1..];
}
