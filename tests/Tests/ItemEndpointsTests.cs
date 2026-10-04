using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Fixture.Dotnet.Tests;

public class ItemEndpointsTests : IClassFixture<ItemApiFactory>
{
  private readonly ItemApiFactory _factory;
  private readonly HttpClient _client;

  public ItemEndpointsTests(ItemApiFactory factory)
  {
    _factory = factory;
    _factory.EmptyStore();
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task Lists_no_items_after_the_store_is_emptied()
  {
    var items = await _client.GetFromJsonAsync<List<ItemDto>>("/items");

    Assert.NotNull(items);
    Assert.Empty(items!);
  }

  [Fact]
  public async Task Drives_the_full_create_read_update_delete_lifecycle()
  {
    var created = await _client.PostAsJsonAsync(
      "/items", new { name = "Widget", description = "A small widget" });
    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    var createdItem = await created.Content.ReadFromJsonAsync<ItemDto>();
    Assert.NotNull(createdItem);
    Assert.True(createdItem!.Id > 0);
    Assert.Equal("Widget", createdItem.Name);
    Assert.Equal("A small widget", createdItem.Description);

    var list = await _client.GetFromJsonAsync<List<ItemDto>>("/items");
    Assert.Single(list!);

    var one = await _client.GetFromJsonAsync<ItemDto>($"/items/{createdItem.Id}");
    Assert.Equal(new ItemDto(createdItem.Id, "Widget", "A small widget"), one);

    var updated = await _client.PutAsJsonAsync(
      $"/items/{createdItem.Id}", new { name = "Renamed", description = "Still here" });
    Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
    var updatedItem = await updated.Content.ReadFromJsonAsync<ItemDto>();
    Assert.Equal(new ItemDto(createdItem.Id, "Renamed", "Still here"), updatedItem);

    var deleted = await _client.DeleteAsync($"/items/{createdItem.Id}");
    Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

    var afterDelete = await _client.GetAsync($"/items/{createdItem.Id}");
    Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
  }

  [Fact]
  public async Task Returns_the_id_as_a_json_number_not_a_string()
  {
    var created = await _client.PostAsJsonAsync("/items", new { name = "Widget" });

    using var document = JsonDocument.Parse(await created.Content.ReadAsStringAsync());

    Assert.Equal(JsonValueKind.Number, document.RootElement.GetProperty("id").ValueKind);
  }

  [Fact]
  public async Task Stores_a_missing_description_as_null()
  {
    var created = await _client.PostAsJsonAsync("/items", new { name = "No description" });

    var item = await created.Content.ReadFromJsonAsync<ItemDto>();

    Assert.Null(item!.Description);
  }

  [Fact]
  public async Task Returns_a_404_problem_detail_for_an_unknown_id_on_get()
  {
    var response = await _client.GetAsync("/items/999");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
    Assert.Equal("Item not found", problem!.Title);
    Assert.Equal(404, problem.Status);
    Assert.Equal("Item 999 was not found", problem.Detail);
  }

  [Fact]
  public async Task Returns_a_404_for_an_unknown_id_on_put()
  {
    var response = await _client.PutAsJsonAsync("/items/999", new { name = "X" });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
    Assert.Equal("Item not found", problem!.Title);
    Assert.Equal(404, problem.Status);
  }

  [Fact]
  public async Task Returns_a_404_for_an_unknown_id_on_delete()
  {
    var response = await _client.DeleteAsync("/items/999");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
    Assert.Equal("Item not found", problem!.Title);
    Assert.Equal(404, problem.Status);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task Rejects_a_blank_name_with_400(string name)
  {
    var response = await _client.PostAsJsonAsync("/items", new { name });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
    Assert.Equal(400, problem!.Status);
  }

  [Fact]
  public async Task Rejects_an_over_length_name_with_400()
  {
    var response = await _client.PostAsJsonAsync("/items", new { name = new string('a', 201) });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
    Assert.Equal(400, problem!.Status);
  }

  [Fact]
  public async Task Rejects_an_over_length_description_with_400()
  {
    var response = await _client.PostAsJsonAsync(
      "/items", new { name = "Ok", description = new string('a', 2001) });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
    Assert.Equal(400, problem!.Status);
  }

  [Fact]
  public async Task Rejects_malformed_json_with_400()
  {
    using var content = new StringContent("{\"name\": \"Broken\"", Encoding.UTF8, "application/json");

    var response = await _client.PostAsync("/items", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
    Assert.Equal("Bad Request", problem!.Title);
  }

  [Fact]
  public async Task Seeds_three_items_on_startup()
  {
    _factory.ResetToSeeds();

    var items = await _client.GetFromJsonAsync<List<ItemDto>>("/items");

    Assert.Equal(3, items!.Count);
    Assert.Equal(new[] { "Widget", "Gadget", "Gizmo" }, items.Select(item => item.Name));
  }

  private sealed record ItemDto(long Id, string Name, string? Description);

  private sealed record ProblemDto(string? Type, string? Title, int? Status, string? Detail);
}
