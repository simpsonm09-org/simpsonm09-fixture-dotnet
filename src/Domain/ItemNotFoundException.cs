namespace Fixture.Dotnet.Domain;

/// <summary>
/// Raised when an item id has no matching record. The API exception handler
/// maps it to an HTTP 404 problem detail.
/// </summary>
public sealed class ItemNotFoundException : Exception
{
  public ItemNotFoundException(long itemId)
    : base($"Item {itemId} was not found")
  {
    ItemId = itemId;
  }

  public long ItemId { get; }
}
