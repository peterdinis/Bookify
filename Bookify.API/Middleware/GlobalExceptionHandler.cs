using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.API.Middleware
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger = logger;

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken
        )
        {
            _logger.LogError(
                exception,
                "An unhandled exception has occurred: {Message}",
                exception.Message
            );

            var problemDetails = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Server Error",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Detail = "An unexpected error occurred. Please try again later.",
            };

            // In development, provide more details
            if (
                httpContext
                    .RequestServices.GetRequiredService<IWebHostEnvironment>()
                    .IsDevelopment()
            )
            {
                problemDetails.Detail = exception.Message;
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                var inner = exception.InnerException;
                var depth = 0;
                while (inner != null && depth < 5)
                {
                    problemDetails.Extensions[$"innerException{depth}"] = inner.Message;
                    inner = inner.InnerException;
                    depth++;
                }
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
