namespace Fixture.Dotnet.Api;

/// <summary>An item returned by the API.</summary>
public sealed record ItemResponse(long Id, string Name, string? Description);
