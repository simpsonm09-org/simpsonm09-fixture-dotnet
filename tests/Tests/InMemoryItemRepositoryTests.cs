using Fixture.Dotnet.Domain;
using Fixture.Dotnet.Store;

namespace Fixture.Dotnet.Tests;

public class InMemoryItemRepositoryTests
{
  [Fact]
  public void Seeds_the_default_items_on_construction()
  {
    var store = new InMemoryItemRepository();

    Assert.Equal(3, store.FindAll().Count);
    Assert.Equal(new[] { "Widget", "Gadget", "Gizmo" }, store.FindAll().Select(item => item.Name));
  }

  [Fact]
  public void Assigns_increasing_ids_and_reads_back()
  {
    var store = new InMemoryItemRepository();
    store.Clear();

    var first = store.Save(new Item(null, "One", null));
    var second = store.Save(new Item(null, "Two", "second"));

    Assert.Equal(1L, first.Id);
    Assert.Equal(2L, second.Id);
    Assert.Equal(new Item(2, "Two", "second"), store.FindById(2));
  }

  [Fact]
  public void Keeps_the_id_when_replacing()
  {
    var store = new InMemoryItemRepository();
    store.Clear();
    store.Save(new Item(null, "One", null));

    store.Save(new Item(1, "Renamed", null));

    Assert.Single(store.FindAll());
    Assert.Equal("Renamed", store.FindById(1)?.Name);
  }

  [Fact]
  public void Deletes_and_reports_existence()
  {
    var store = new InMemoryItemRepository();
    store.Clear();
    var stored = store.Save(new Item(null, "One", null));

    Assert.True(store.ExistsById(stored.Id!.Value));
    store.DeleteById(stored.Id!.Value);
    Assert.False(store.ExistsById(stored.Id!.Value));
    Assert.Empty(store.FindAll());
  }

  [Fact]
  public void A_new_instance_discards_created_items_and_restores_the_seeds()
  {
    var first = new InMemoryItemRepository();
    first.Clear();
    first.Save(new Item(null, "Ephemeral", null));

    var restarted = new InMemoryItemRepository();

    Assert.Equal(3, restarted.FindAll().Count);
    Assert.DoesNotContain(restarted.FindAll(), item => item.Name == "Ephemeral");
  }

  [Fact]
  public void Assigns_distinct_ids_under_concurrent_saves()
  {
    var store = new InMemoryItemRepository();
    store.Clear();

    Parallel.For(0, 1000, index => store.Save(new Item(null, $"Item {index}", null)));

    var ids = store.FindAll().Select(item => item.Id!.Value).ToList();
    Assert.Equal(1000, ids.Count);
    Assert.Equal(1000, ids.Distinct().Count());
  }
}
