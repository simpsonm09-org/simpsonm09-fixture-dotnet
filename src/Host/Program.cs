using Fixture.Dotnet.Api;
using Fixture.Dotnet.Service;
using Fixture.Dotnet.Store;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1", options =>
{
  options.AddDocumentTransformer((document, _, _) =>
  {
    document.Info = new OpenApiInfo
    {
      Title = "Simpsonm09 Fixture Dotnet API",
      Description = "Item CRUD service for the simpsonm09 repository fixture",
      Version = "0.1.0",
    };
    return Task.CompletedTask;
  });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ItemNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BadRequestBodyHandler>();
builder.Services.AddApiValidation();
builder.Services.AddValidation();
builder.Services.AddSingleton<IItemRepository, InMemoryItemRepository>();
builder.Services.AddSingleton<IItemService, ItemService>();

var app = builder.Build();

app.MapOpenApi();
app.UseExceptionHandler();
app.MapItemEndpoints();

app.Run();

/// <summary>Entry point marker so the test project can host the application.</summary>
public partial class Program;
