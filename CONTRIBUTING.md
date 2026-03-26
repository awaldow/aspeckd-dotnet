# Contributing to Aspeckd

Thank you for your interest in contributing! This guide explains the project structure and the conventions to follow when making changes.

---

## Project structure

```
aspeckd-dotnet/
├── src/
│   ├── Aspeckd.Core/          # Stable core abstractions — no ASP.NET Core dependency
│   └── Aspeckd/               # ASP.NET Core integration — implementation + routing
├── tests/
│   └── Aspeckd.Tests/         # xUnit integration tests (net10.0)
└── .github/workflows/
    └── ci.yml                 # Build & test on every PR and push to main
```

---

## The Core / implementation split

The repository is intentionally divided into two projects.

### `Aspeckd.Core`

| Property | Value |
|---|---|
| Dependencies | BCL only (no ASP.NET Core, no `Microsoft.OpenApi`) |
| Target frameworks | `net8.0;net9.0;net10.0` |
| Contents | Companion attributes, response models, `AspeckdOptions`, `IAgentSpecProvider` |

`Aspeckd.Core` is the **stable public API surface**.  
Because it carries zero framework dependencies it can be referenced by class libraries and domain layers that should not take a hard dependency on ASP.NET Core.

**Rules for `Aspeckd.Core`:**

- No `FrameworkReference` to `Microsoft.AspNetCore.App`.
- No `PackageReference` to anything outside the BCL.
- New public types (attributes, models, options) belong here.
- Breaking changes to this project require a major version bump.

### `Aspeckd`

| Property | Value |
|---|---|
| Dependencies | `Microsoft.AspNetCore.App` (framework reference) + `Aspeckd.Core` |
| Target frameworks | `net8.0;net9.0;net10.0` |
| Contents | `AgentSpecProvider`, DI extensions (`AddAgentSpec`), routing extensions (`MapAgentSpec`) |

`Aspeckd` is the **ASP.NET Core integration layer**.  
All `IApiDescriptionGroupCollectionProvider` / `Microsoft.OpenApi` usage lives here so that TFM-specific or library-version-specific differences can be isolated with `#if NET9_0_OR_GREATER` / `#if NET10_0_OR_GREATER` guards without touching the stable core abstractions.

**Rules for `Aspeckd`:**

- Keep `AgentSpecProvider` `internal sealed` — consumers depend on `IAgentSpecProvider`.
- TFM-specific code (e.g. `Microsoft.OpenApi` v1 → v2 breaking changes) belongs here, guarded by `#if` preprocessor directives.
- DI and routing extension methods registered via `AddAgentSpec` / `MapAgentSpec` are the only public entry points.

---

## Adding a new companion attribute

1. Create the attribute class in `src/Aspeckd.Core/Attributes/`.
2. Register its usage in `AgentSpecProvider.BuildName` / `BuildDescription` (or a new helper) in `src/Aspeckd/Services/AgentSpecProvider.cs`.
3. Add a test in `tests/Aspeckd.Tests/AttributeTests.cs` (or a new focused test file).

---

## Development setup

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download), [.NET 9 SDK](https://dotnet.microsoft.com/download), and [.NET 10 SDK (preview)](https://dotnet.microsoft.com/download).

```bash
# Restore dependencies
dotnet restore

# Build all projects across all TFMs
dotnet build

# Run all tests
dotnet test
```

---

## Coding conventions

- **Nullable reference types** are enabled across all projects (`<Nullable>enable</Nullable>`).
- **Implicit usings** are enabled — add explicit `using` statements only when not covered by the implicit set.
- Keep new public API additions in `Aspeckd.Core`; keep ASP.NET Core integration code in `Aspeckd`.
- Internal implementation types (`AgentSpecProvider`, etc.) should remain `internal sealed` unless there is a strong reason to expose them.
- XML doc comments are required on all public types and members in `Aspeckd.Core`.

---

## Running CI locally

The same steps the GitHub Actions workflow runs:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

---

## Versioning and releases

This repo uses **[MinVer](https://github.com/adamralph/minver)** for automatic semantic versioning and **[release-please](https://github.com/googleapis/release-please)** to automate CHANGELOG maintenance and GitHub Releases.

### How it works

1. **Version inference** — MinVer derives the NuGet package version from the nearest git tag (e.g. `v1.2.3`). No manual `<Version>` property is needed in the `.csproj` files.
2. **Conventional commits** — Commit messages on `main` must follow [Conventional Commits](https://www.conventionalcommits.org/). The prefix drives the next version bump:
   - `fix:` → patch bump (1.2.3 → 1.2.4)
   - `feat:` → minor bump (1.2.3 → 1.3.0)
   - `feat!:` or `BREAKING CHANGE:` footer → major bump (1.2.3 → 2.0.0)
3. **Release PR** — The `Release Please` GitHub Actions workflow watches `main`. When it detects conventional commits since the last release, it opens (or updates) a release PR that bumps the version in `.release-please-manifest.json` and updates `CHANGELOG.md`.
4. **Tagging** — Merging the release PR causes release-please to create a git tag (e.g. `v1.3.0`) and a GitHub Release. MinVer picks up this tag on the next build.

> **NuGet publishing** is not yet automated. A follow-up PR will wire up the publish step once a NuGet account is available.
