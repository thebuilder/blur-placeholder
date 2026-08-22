# Contributing to Blur Placeholder

Thanks for your interest in improving Blur Placeholder for Umbraco. This guide covers the repository layout, how to set up a local development environment, and how to get a change merged. For how released package versions reach NuGet, see [docs/releasing.md](docs/releasing.md).

## Repository layout

This repository is a pnpm and .NET workspace. The most relevant folders:

| Path | What it is |
| --- | --- |
| `src/TheBuilder.BlurPlaceholder/` | The NuGet package: generation, media handling, migrations, health checks, and the packaging targets. |
| `src/TheBuilder.BlurPlaceholder/Client/` | The backoffice property editor (TypeScript, Lit, Vite). Built assets are emitted to `wwwroot/App_Plugins/`. |
| `samples/TheBuilder.BlurPlaceholder.Example/` | A runnable Umbraco site that references the package for local development. |
| `tests/TheBuilder.BlurPlaceholder.Tests/` | The .NET (xUnit) test project. |
| `tools/TheBuilder.BlurPlaceholder.DemoGenerator/` | Regenerates the measured placeholder demos used by the docs site. |
| `apps/docs/` | The documentation site (Blume) published to <https://blur.thebuilder.dk/>. |
| `docs/releasing.md` | How package versions are published to NuGet. |

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download): builds the package, sample, and tests.
- [Node.js 24](https://nodejs.org/): builds the backoffice client and the docs site.
- [pnpm](https://pnpm.io/) via [Corepack](https://nodejs.org/api/corepack.html). Run `corepack enable` once; the pinned pnpm version resolves automatically from `package.json`.

Install the JavaScript dependencies from the repository root:

```sh
pnpm install
```

## Build the backoffice client

The property editor is a separate frontend build. A plain `dotnet build` of the sample does **not** rebuild it; the client is only built automatically when the NuGet package is packed. During development, build (or watch) the client yourself so its assets land in `wwwroot/App_Plugins/`:

```sh
pnpm client:build   # one-off build
pnpm client:watch   # rebuild on change
```

The bundled `blurhash` and `thumbhash` decoders are verified separately:

```sh
pnpm client:test
```

## Run the sample site

The example project references the package directly, so it always uses your local source. With the client already built, run it from the repository root:

```sh
dotnet run --project samples/TheBuilder.BlurPlaceholder.Example
```

On first launch Umbraco installs unattended and creates a local SQLite database. Upload or replace an Image media item, then inspect the read-only `blurPlaceholder` property. The sample also enables the Delivery API with public media access, so you can request the property straight away.

For an efficient loop, run `pnpm client:watch` in one terminal and `dotnet run` in another.

## Run the tests

```sh
dotnet test tests/TheBuilder.BlurPlaceholder.Tests/TheBuilder.BlurPlaceholder.Tests.csproj
```

The .NET suite runs against Umbraco 17.1, the latest 17.x, and the latest 18.x in CI. Target a specific line locally by passing the version, for example `-p:UmbracoVersion=18.*`.

## Work on the documentation site

```sh
pnpm docs:dev        # local preview with hot reload
pnpm docs:build      # production build, including Open Graph cards
pnpm docs:check      # type-check the site
pnpm docs:validate   # validate internal, anchor, asset, and external links
```

Content lives in `apps/docs/content/` as Markdown and MDX. The landing page is a custom Astro page in `apps/docs/pages/index.astro`.

The measured comparison on the [Overview](https://blur.thebuilder.dk/overview) page is generated, not hand-written. Regenerate it from the repository root after changing generation defaults or demo sources:

```sh
dotnet run --project tools/TheBuilder.BlurPlaceholder.DemoGenerator -- .
```

## Submitting a change

1. Create a branch for your change.
2. Keep pull requests focused, and update the relevant docs under `apps/docs/` when behavior changes.
3. Run the frontend and .NET tests above so CI passes on the first try. The `Verify package` workflow builds the client and docs, runs the .NET suite across the supported Umbraco versions, builds the sample host, and validates the packed NuGet archive.
4. Open a pull request against `main` with a clear description of the change and its motivation.

## Releasing

Publishing to NuGet is release-driven and documented separately in [docs/releasing.md](docs/releasing.md). GitHub Releases are the authoritative changelog and are surfaced in the [documentation changelog](https://blur.thebuilder.dk/changelog/).
