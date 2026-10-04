using Fixture.Dotnet.Domain;

namespace Fixture.Dotnet.Store;

/// <summary>
/// The store port the service depends on. It speaks domain types, so the
/// service never sees a store record and the adapter can be swapped without
/// touching business logic.
/// </summary>
public interface IItemRepository
{
  IReadOnlyList<Item> FindAll();

  Item? FindById(long id);

  Item Save(Item item);

  void DeleteById(long id);

  bool ExistsById(long id);
}
