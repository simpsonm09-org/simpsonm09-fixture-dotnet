using Fixture.Dotnet.Domain;
using Fixture.Dotnet.Store;

namespace Fixture.Dotnet.Tests;

/// <summary>
/// A hand-written store port double, so the service tests exercise the business
/// logic without depending on the real adapter.
/// </summary>
internal sealed class FakeItemRepository : IItemRepository
{
  private readonly Dictionary<long, Item> _items = new();
  private long _nextId = 100;

  public int SaveCalls { get; private set; }

  public int DeleteCalls { get; private set; }

  public IReadOnlyList<Item> FindAll() => _items.Values.OrderBy(item => item.Id).ToList();

  public Item? FindById(long id) => _items.TryGetValue(id, out var item) ? item : null;

  public bool ExistsById(long id) => _items.ContainsKey(id);

  public Item Save(Item item)
  {
    SaveCalls++;
    var id = item.Id ?? _nextId++;
    var stored = new Item(id, item.Name, item.Description);
    _items[id] = stored;
    return stored;
  }

  public void DeleteById(long id)
  {
    DeleteCalls++;
    _items.Remove(id);
  }
}
