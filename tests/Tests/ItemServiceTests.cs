using Fixture.Dotnet.Domain;
using Fixture.Dotnet.Service;

namespace Fixture.Dotnet.Tests;

public class ItemServiceTests
{
  [Fact]
  public void ListItems_returns_every_item_from_the_port()
  {
    var repository = new FakeItemRepository();
    repository.Save(new Item(null, "Widget", null));
    repository.Save(new Item(null, "Gadget", "A handy gadget"));

    var items = new ItemService(repository).ListItems();

    Assert.Equal(2, items.Count);
    Assert.Equal(new[] { "Widget", "Gadget" }, items.Select(item => item.Name));
  }

  [Fact]
  public void GetItem_returns_the_item()
  {
    var repository = new FakeItemRepository();
    var stored = repository.Save(new Item(null, "Widget", null));

    var item = new ItemService(repository).GetItem(stored.Id!.Value);

    Assert.Equal("Widget", item.Name);
  }

  [Fact]
  public void GetItem_throws_for_a_missing_item()
  {
    var service = new ItemService(new FakeItemRepository());

    var error = Assert.Throws<ItemNotFoundException>(() => service.GetItem(9));

    Assert.Equal(9L, error.ItemId);
    Assert.Equal("Item 9 was not found", error.Message);
  }

  [Fact]
  public void CreateItem_saves_through_the_port()
  {
    var repository = new FakeItemRepository();

    var created = new ItemService(repository).CreateItem("Gadget", "A handy gadget");

    Assert.Equal(1, repository.SaveCalls);
    Assert.Equal("Gadget", created.Name);
    Assert.Equal("A handy gadget", created.Description);
  }

  [Fact]
  public void UpdateItem_replaces_an_existing_item()
  {
    var repository = new FakeItemRepository();
    var stored = repository.Save(new Item(null, "Widget", null));

    var updated = new ItemService(repository).UpdateItem(stored.Id!.Value, "Renamed", null);

    Assert.Equal(stored.Id, updated.Id);
    Assert.Equal("Renamed", updated.Name);
    Assert.Null(updated.Description);
  }

  [Fact]
  public void UpdateItem_refuses_a_missing_item_without_saving()
  {
    var repository = new FakeItemRepository();

    Assert.Throws<ItemNotFoundException>(
      () => new ItemService(repository).UpdateItem(404, "Nope", null));
    Assert.Equal(0, repository.SaveCalls);
  }

  [Fact]
  public void DeleteItem_removes_an_existing_item()
  {
    var repository = new FakeItemRepository();
    var stored = repository.Save(new Item(null, "Widget", null));

    new ItemService(repository).DeleteItem(stored.Id!.Value);

    Assert.Equal(1, repository.DeleteCalls);
    Assert.False(repository.ExistsById(stored.Id!.Value));
  }

  [Fact]
  public void DeleteItem_refuses_a_missing_item_without_deleting()
  {
    var repository = new FakeItemRepository();

    Assert.Throws<ItemNotFoundException>(() => new ItemService(repository).DeleteItem(404));
    Assert.Equal(0, repository.DeleteCalls);
  }
}
