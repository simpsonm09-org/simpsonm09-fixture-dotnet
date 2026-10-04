using Fixture.Dotnet.Domain;
using Fixture.Dotnet.Store;

namespace Fixture.Dotnet.Service;

/// <summary>
/// Item business logic. It works in the domain <see cref="Item"/> and depends
/// on the <see cref="IItemRepository"/> port, so it never sees a transport DTO
/// or a store record.
/// </summary>
public sealed class ItemService : IItemService
{
  private readonly IItemRepository _repository;

  public ItemService(IItemRepository repository) => _repository = repository;

  public IReadOnlyList<Item> ListItems() => _repository.FindAll();

  public Item GetItem(long id) => _repository.FindById(id) ?? throw new ItemNotFoundException(id);

  public Item CreateItem(Item item) => _repository.Save(item with { Id = null });

  public Item UpdateItem(long id, Item item)
  {
    if (!_repository.ExistsById(id))
    {
      throw new ItemNotFoundException(id);
    }

    return _repository.Save(item with { Id = id });
  }

  public void DeleteItem(long id)
  {
    if (!_repository.ExistsById(id))
    {
      throw new ItemNotFoundException(id);
    }

    _repository.DeleteById(id);
  }
}
