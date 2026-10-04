using Fixture.Dotnet.Domain;

namespace Fixture.Dotnet.Store;

/// <summary>A seed for the in-memory store: a name and an optional description.</summary>
public readonly record struct ItemSeed(string Name, string? Description);

/// <summary>
/// In-memory store adapter. It converges to the seeded state on restart, so a
/// restart discards every item the caller created and restores the three seeds.
/// Every access takes a lock, so concurrent requests cannot corrupt the
/// dictionary or hand out a duplicate id.
/// </summary>
public sealed class InMemoryItemRepository : IItemRepository
{
  public static readonly IReadOnlyList<ItemSeed> DefaultItems =
  [
    new("Widget", "A small widget"),
    new("Gadget", "A handy gadget"),
    new("Gizmo", "A clever gizmo"),
  ];

  private readonly object _gate = new();
  private readonly Dictionary<long, Item> _items = new();
  private long _nextId = 1;

  public InMemoryItemRepository() => Seed(DefaultItems);

  public IReadOnlyList<Item> FindAll()
  {
    lock (_gate)
    {
      return _items.Values.OrderBy(item => item.Id).ToList();
    }
  }

  public Item? FindById(long id)
  {
    lock (_gate)
    {
      return _items.TryGetValue(id, out var item) ? item : null;
    }
  }

  public Item Save(Item item)
  {
    lock (_gate)
    {
      var id = item.Id ?? _nextId++;
      var stored = new Item(id, item.Name, item.Description);
      _items[id] = stored;
      return stored;
    }
  }

  public void DeleteById(long id)
  {
    lock (_gate)
    {
      _items.Remove(id);
    }
  }

  public bool ExistsById(long id)
  {
    lock (_gate)
    {
      return _items.ContainsKey(id);
    }
  }

  /// <summary>Replaces every record with the given seeds. Used by the dev seed and tests.</summary>
  public void Seed(IReadOnlyList<ItemSeed> seeds)
  {
    lock (_gate)
    {
      _items.Clear();
      _nextId = 1;
      foreach (var seed in seeds)
      {
        Save(new Item(null, seed.Name, seed.Description));
      }
    }
  }

  /// <summary>Empties the store.</summary>
  public void Clear()
  {
    lock (_gate)
    {
      _items.Clear();
      _nextId = 1;
    }
  }
}
