# Blur Placeholder sample

A minimal Umbraco CMS 17.5.3 host for manually verifying the package.

1. Run `dotnet run --project samples/TheBuilder.BlurPlaceholder.Example`.
2. Complete the unattended Umbraco setup at `/umbraco` if the database is new.
3. Upload or replace an Image media item and inspect the read-only `blurPlaceholder` property.

Build the backoffice client first with `pnpm client:build`, or the property editor will not load. See [CONTRIBUTING.md](../../CONTRIBUTING.md) for the full development loop.

The sample registers the Delivery API and enables public content and media responses in `appsettings.json`, so you can run the query below as soon as an Image exists. The package leaves the core media converter unchanged, so a consumer requests the property by name:

```http
GET /umbraco/delivery/api/v2/media/item/{mediaId}?expand=properties[$all]&fields=properties[blurPlaceholder]
```

The same `expand=properties[$all]` and `fields=properties[blurPlaceholder]` selection works for a picker response. A frontend can render the returned value directly when it starts with `data:image/`. Native `blurhash:` and `thumbhash:` values need the matching decoder library.
