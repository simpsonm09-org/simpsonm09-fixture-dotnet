# simpsonm09-fixture-dotnet

An ASP.NET Core item CRUD service. It is the C# fixture for the simpsonm09 fleet standard.

The original lives in `simpsonm09-org/simpsonm09-fixture-dotnet`; work happens on the personal fork. See [`repo-standard`](https://github.com/simpsonm09-org/simpsonm09-repo-standard).

## What it does

The service exposes item CRUD over HTTP behind a layered architecture. An endpoint group speaks DTOs, a service works in the domain `Item`, and an in-memory store adapter implements a repository port. An item has a server-assigned id, a required name, and an optional description. The store is in memory, so restarting the process discards every item and restores three dev seeds.

## Quick start

```bash
just install
just deps
just verify
```

## Commands

| Command | Does |
| --- | --- |
| `just install` | Installs the pinned tools. |
| `just deps` | Restores the NuGet dependencies. |
| `just lint` | Runs the linters. |
| `just test` | Runs the xUnit suite. |
| `just coverage` | Runs the tests and writes `coverage/lcov.info`. |
| `just spec` | Regenerates `docs/openapi.json` from the running application. |
| `just serve` | Builds and serves the API on port 5080. |
| `just verify` | Lints and tests. |

## Documentation

Read [`docs/README.md`](docs/README.md) for the architecture, the items feature, and the OpenAPI contract.

## License

MIT. See [`LICENSE`](LICENSE).
