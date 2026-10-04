using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace Fixture.Dotnet.Api;

/// <summary>
/// Maps a malformed request body to the 400 the contract promises. Minimal API
/// body binding raises <see cref="BadHttpRequestException"/> for malformed JSON;
/// the exception handler middleware would otherwise report it as a 500.
/// </summary>
public sealed class BadRequestBodyHandler : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken)
  {
    var status = exception switch
    {
      BadHttpRequestException badRequest => badRequest.StatusCode,
      JsonException => StatusCodes.Status400BadRequest,
      _ => 0,
    };

    if (status == 0)
    {
      return false;
    }

    await Results
      .Problem(
        statusCode: status,
        title: "Bad Request",
        detail: "The request body could not be read.")
      .ExecuteAsync(httpContext);

    return true;
  }
}
