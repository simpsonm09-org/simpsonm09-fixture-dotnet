using Fixture.Dotnet.Api;
using Fixture.Dotnet.Service;
using Fixture.Dotnet.Store;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core reads numbers from JSON strings by default, which widens the
// generated schema for every numeric field. Values on the wire are numbers, so
// pin strict handling and keep the contract honest.
builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

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
