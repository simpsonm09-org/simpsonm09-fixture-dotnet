using Fixture.Dotnet.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace Fixture.Dotnet.Api;

/// <summary>
/// Maps <see cref="ItemNotFoundException"/> to an RFC 7807 problem detail with
/// content type <c>application/problem+json</c>. Every other exception falls
/// through to the next handler.
/// </summary>
public sealed class ItemNotFoundExceptionHandler : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken)
  {
    if (exception is not ItemNotFoundException notFound)
    {
      return false;
    }

    await Results
      .Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Item not found",
        detail: notFound.Message)
      .ExecuteAsync(httpContext);

    return true;
  }
}
