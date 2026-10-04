using Fixture.Dotnet.Service;

namespace Fixture.Dotnet.Api;

/// <summary>Item CRUD endpoints. They speak DTOs and never touch the store.</summary>
public static class ItemEndpoints
{
  private const string ProblemJson = "application/problem+json";

  public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/items").WithTags("Items");

    group
      .MapGet("/", (IItemService service) =>
        Results.Ok(service.ListItems().Select(ItemMapper.ToResponse).ToList()))
      .WithName("listItems")
      .WithSummary("List every item")
      .Produces<IReadOnlyList<ItemResponse>>(StatusCodes.Status200OK);

    group
      .MapGet("/{id:long}", (long id, IItemService service) =>
        Results.Ok(ItemMapper.ToResponse(service.GetItem(id))))
      .WithName("getItem")
      .WithSummary("Get one item by id")
      .Produces<ItemResponse>(StatusCodes.Status200OK)
      .ProducesProblem(StatusCodes.Status404NotFound, ProblemJson);

    group
      .MapPost("/", (ItemRequest request, IItemService service) =>
      {
        var created = service.CreateItem(request.Name, request.Description);
        return Results.Created($"/items/{created.Id}", ItemMapper.ToResponse(created));
      })
      .WithName("createItem")
      .WithSummary("Create an item")
      .Produces<ItemResponse>(StatusCodes.Status201Created)
      .ProducesProblem(StatusCodes.Status400BadRequest);

    group
      .MapPut("/{id:long}", (long id, ItemRequest request, IItemService service) =>
        Results.Ok(ItemMapper.ToResponse(
          service.UpdateItem(id, request.Name, request.Description))))
      .WithName("updateItem")
      .WithSummary("Replace an item")
      .Produces<ItemResponse>(StatusCodes.Status200OK)
      .ProducesProblem(StatusCodes.Status400BadRequest)
      .ProducesProblem(StatusCodes.Status404NotFound, ProblemJson);

    group
      .MapDelete("/{id:long}", (long id, IItemService service) =>
      {
        service.DeleteItem(id);
        return Results.NoContent();
      })
      .WithName("deleteItem")
      .WithSummary("Delete an item")
      .Produces(StatusCodes.Status204NoContent)
      .ProducesProblem(StatusCodes.Status404NotFound, ProblemJson);

    return app;
  }
}
