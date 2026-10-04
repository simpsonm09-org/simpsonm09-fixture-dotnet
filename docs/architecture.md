# Architecture

The service is a layered ASP.NET Core application. A request enters at the endpoint
group, which speaks DTOs, and travels down through the service, which speaks the domain
type, to an in-memory store adapter, which speaks the repository port. Each layer is a
separate assembly and depends only on the layer below it.

```mermaid
flowchart TD
    Client -->|HTTP/JSON| Endpoints[Api.ItemEndpoints]
    Endpoints -->|ItemRequest / ItemResponse| Mapper[Api.ItemMapper]
    Endpoints --> Service[Service.ItemService]
    Service -->|Domain.Item| Port[Store.IItemRepository]
    Port --> Adapter[Store.InMemoryItemRepository]
    Adapter --> Map[(in-memory Dictionary)]
```

## Layers

- `src/Api/` holds the endpoint group, the transport DTOs, the RFC 7807 exception handlers, and the mapper between DTOs and the domain. It carries the `Microsoft.AspNetCore.OpenApi` annotations, so the OpenAPI document is generated from this code.
- `src/Service/` holds the business logic. It works in `Domain.Item` and depends on the `Store.IItemRepository` port, not on the adapter.
- `src/Domain/` holds `Item`, the type the service reasons about, and `ItemNotFoundException`. It has no framework references.
- `src/Store/` holds the domain-facing port and the in-memory adapter that implements it, along with the three dev seeds.
- `src/Api/Program.cs` wires the OpenAPI document, the validation, the exception handlers, and the endpoints.

## Containment

The endpoint layer never sees a store record and the service never sees a DTO. The adapter
is the only place that writes to the dictionary, and the mapper is the only place that
converts between `Item` and the DTOs. The assembly references enforce the boundary, so the
framework stays out of the domain and the business logic.

## Generated contract

`docs/openapi.json` is produced from the running application by
`Microsoft.AspNetCore.OpenApi`. `just spec` regenerates it; a test fails when the
committed document drifts from the generated one.
