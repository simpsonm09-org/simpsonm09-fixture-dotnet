using Fixture.Dotnet.Domain;

namespace Fixture.Dotnet.Service;

/// <summary>Item business logic, expressed in the domain <see cref="Item"/>.</summary>
public interface IItemService
{
  IReadOnlyList<Item> ListItems();

  Item GetItem(long id);

  Item CreateItem(Item item);

  Item UpdateItem(long id, Item item);

  void DeleteItem(long id);
}
