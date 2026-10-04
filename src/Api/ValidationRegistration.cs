using Microsoft.Extensions.DependencyInjection;

namespace Fixture.Dotnet.Api;

/// <summary>
/// Registers the validation metadata the source generator emits for the DTOs
/// declared in this assembly. The composition root calls this next to its own
/// <c>AddValidation</c>, because the generator only runs in the assembly where
/// <c>AddValidation</c> is called.
/// </summary>
public static class ValidationRegistration
{
  public static IServiceCollection AddApiValidation(this IServiceCollection services) =>
    services.AddValidation();
}
