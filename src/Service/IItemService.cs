using Fixture.Dotnet.Domain;

namespace Fixture.Dotnet.Service;

/// <summary>Item business logic, expressed in the domain <see cref="Item"/>.</summary>
public interface IItemService
{
  IReadOnlyList<Item> ListItems();

  Item GetItem(long id);

  Item CreateItem(string name, string? description);

  Item UpdateItem(long id, string name, string? description);

  void DeleteItem(long id);
}
