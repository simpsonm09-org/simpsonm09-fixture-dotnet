using Fixture.Dotnet.Domain;

namespace Fixture.Dotnet.Api;

/// <summary>Translates between the transport DTOs and the domain type.</summary>
public static class ItemMapper
{
  public static ItemResponse ToResponse(Item item)
  {
    if (item.Id is null)
    {
      throw new InvalidOperationException("a persisted item must have an id");
    }

    return new ItemResponse(item.Id.Value, item.Name, item.Description);
  }

  public static Item ToDomain(ItemRequest request) =>
    new(null, request.Name ?? string.Empty, request.Description);
}
