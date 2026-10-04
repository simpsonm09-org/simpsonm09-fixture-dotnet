using Fixture.Dotnet.Store;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Fixture.Dotnet.Tests;

/// <summary>
/// Hosts the API in-process and exposes its in-memory store, so a test can
/// arrange the item fixtures before it drives the HTTP surface.
/// </summary>
public sealed class ItemApiFactory : WebApplicationFactory<Program>
{
  public InMemoryItemRepository Store =>
    (InMemoryItemRepository)Services.GetRequiredService<IItemRepository>();

  public void ResetToSeeds() => Store.Seed(InMemoryItemRepository.DefaultItems);

  public void EmptyStore() => Store.Clear();
}
