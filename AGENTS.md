# simpsonm09-fixture-dotnet working agreements

A layered ASP.NET Core item CRUD service. It is the C# fixture for the fleet standard.

## Ground rules

- Business logic lives in `src/Service/` and works in `Domain.Item`. The endpoint layer never touches the store and the service never sees a DTO. Only `src/Api/ItemMapper.cs` bridges DTOs and the domain; only the store adapter bridges the port and its records. Only the `src/Host/` composition root references the store adapter and binds it to the port. The assembly references enforce the boundary.
- `docs/openapi.json` is generated. Annotate the endpoints and the DTOs and run `just spec`; never hand-edit the document. A test fails when the committed document drifts.
- The store is in memory. A restart discards items and restores the three dev seeds.
- No secret, credential, or machine path is committed.

## Commands

- `just install`, `just deps`, `just lint`, `just lint-fix`, `just aislop`, `just test`, `just coverage`, `just spec`, `just serve`, `just verify`, `just prune`.

## Repo facts

- Language and toolchain: .NET 10, ASP.NET Core Minimal API, `Microsoft.AspNetCore.OpenApi` for the OpenAPI document, `System.ComponentModel.DataAnnotations` for request validation, pinned in `mise.toml`.
- Tests: xUnit with `WebApplicationFactory` for the integration suite and plain unit tests for the service and the store. `dotnet test --collect:"XPlat Code Coverage"` writes a cobertura report; `scripts/cobertura-to-lcov.mjs` converts it to `coverage/lcov.info`, the path the fleet patch-coverage gate reads.
- Data: no database. `src/Store/InMemoryItemRepository.cs` holds a `Dictionary` and seeds three items on construction.
- Domain: `GET`, `POST`, `PUT`, and `DELETE` over `/items`. Reads, updates, and deletes of an unknown id throw `ItemNotFoundException`, which `src/Api/ItemNotFoundExceptionHandler.cs` maps to a 404 `application/problem+json` body.
- Contracts: `docs/openapi.json` is generated from the endpoints and the DTOs. Regenerate it with `just spec`.
- Docs: `docs/README.md` indexes the architecture, the items feature, and the OpenAPI contract.

## Skills

No repo-local skills. General best practices and integration come from the plugins.
