using Fixture.Dotnet.Api;
using Fixture.Dotnet.Domain;

namespace Fixture.Dotnet.Tests;

public class ItemMapperTests
{
  [Fact]
  public void ToResponse_copies_the_item_fields()
  {
    var response = ItemMapper.ToResponse(new Item(7, "Widget", "A small widget"));

    Assert.Equal(new ItemResponse(7, "Widget", "A small widget"), response);
  }

  [Fact]
  public void ToResponse_refuses_a_persisted_item_without_an_id()
  {
    Assert.Throws<InvalidOperationException>(
      () => ItemMapper.ToResponse(new Item(null, "Widget", null)));
  }

  [Fact]
  public void ToDomain_maps_a_request_and_leaves_the_id_unset()
  {
    var item = ItemMapper.ToDomain(new ItemRequest { Name = "Widget", Description = "A small widget" });

    Assert.Equal(new Item(null, "Widget", "A small widget"), item);
  }

  [Fact]
  public void ToDomain_never_yields_a_null_name()
  {
    var item = ItemMapper.ToDomain(new ItemRequest { Name = null!, Description = null });

    Assert.Equal(string.Empty, item.Name);
  }
}
