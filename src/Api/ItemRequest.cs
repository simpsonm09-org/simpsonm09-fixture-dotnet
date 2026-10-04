using System.ComponentModel.DataAnnotations;

namespace Fixture.Dotnet.Api;

/// <summary>Payload used to create or replace an item. The id is assigned by the store.</summary>
public sealed record ItemRequest
{
  /// <summary>Item name. Required, and at most 200 characters.</summary>
  [Required]
  [MaxLength(200)]
  public string Name { get; init; } = string.Empty;

  /// <summary>Item description. Optional, and at most 2000 characters.</summary>
  [MaxLength(2000)]
  public string? Description { get; init; }
}
