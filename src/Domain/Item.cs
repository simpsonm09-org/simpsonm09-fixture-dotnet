namespace Fixture.Dotnet.Domain;

/// <summary>
/// An item as the service layer reasons about it, free of transport and store
/// detail. <see cref="Id"/> is null until the store assigns it on save.
/// </summary>
public sealed record Item(long? Id, string Name, string? Description);
