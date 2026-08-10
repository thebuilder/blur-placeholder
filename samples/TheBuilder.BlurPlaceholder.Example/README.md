# Blur Placeholder sample

This is a minimal Umbraco CMS 17.5.3 host for manually verifying the package.

1. Run `dotnet run --project samples/TheBuilder.BlurPlaceholder.Example`.
2. Complete the unattended Umbraco setup at `/umbraco` if the database is new.
3. Upload or replace an Image media item and inspect the read-only `blurPlaceholder` property.

The sample registers the Delivery API and enables public content/media responses in `appsettings.json`, so the direct media query below can be tried immediately after creating an Image.

The package deliberately leaves the core media converter unchanged. A Delivery API consumer can request the value directly:

```http
GET /umbraco/delivery/api/v2/media/item/{mediaId}?expand=properties[$all]&fields=properties[blurPlaceholder]
```

For a picker response, use the same `expand=properties[$all]` and limit the returned fields to `properties[blurPlaceholder]`. A frontend can render the returned value directly when it starts with `data:image/`; native `blurhash:` and `thumbhash:` values can be decoded with the matching browser libraries.
