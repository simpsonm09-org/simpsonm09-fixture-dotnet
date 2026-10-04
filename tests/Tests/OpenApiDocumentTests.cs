using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fixture.Dotnet.Tests;

/// <summary>
/// Generates the OpenAPI document. With <c>WRITE_OPENAPI=1</c> (run by
/// <c>just spec</c> through scripts/write-openapi.mjs) it writes
/// <c>docs/openapi.json</c>. Otherwise it asserts the committed document
/// matches the generated one, so a hand edit or a drifting route fails the
/// suite.
/// </summary>
public class OpenApiDocumentTests : IClassFixture<ItemApiFactory>
{
  private readonly ItemApiFactory _factory;

  public OpenApiDocumentTests(ItemApiFactory factory) => _factory = factory;

  [Fact]
  public async Task Document_matches_the_committed_file()
  {
    var generated = Canonicalize(await _factory.CreateClient().GetStringAsync("/openapi/v1.json"));
    var path = Path.Combine(RepoRoot(), "docs", "openapi.json");

    if (Environment.GetEnvironmentVariable("WRITE_OPENAPI") == "1")
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      await File.WriteAllTextAsync(path, generated);
      return;
    }

    Assert.True(File.Exists(path), $"missing generated contract at {path}; run `just spec`");
    Assert.Equal(Canonicalize(await File.ReadAllTextAsync(path)), generated);
  }

  private static string Canonicalize(string json)
  {
    var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("empty OpenAPI document");
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    return node.ToJsonString(options) + "\n";
  }

  private static string RepoRoot()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      if (File.Exists(Path.Combine(directory.FullName, "simpsonm09-fixture-dotnet.slnx")))
      {
        return directory.FullName;
      }

      directory = directory.Parent;
    }

    throw new InvalidOperationException("repository root not found");
  }
}
