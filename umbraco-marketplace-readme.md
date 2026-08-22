![Preview and copy a generated placeholder from an Image media item](https://raw.githubusercontent.com/thebuilder/blur-placeholder/refs/heads/main/apps/docs/content/screenshots/blur-placeholder-media.png)

Blur Placeholder generates a compact loading placeholder whenever an Image changes in Umbraco. The result is stored on the media item, ready for a frontend to request without any image processing during a public request.

## Key features

- **Tiny WebP by default.** Pass the generated data URL straight to an image component. No client decoder involved.
- **BlurHash and ThumbHash when you need them.** Store the compact native representation, or let the package decode it to WebP before storage.
- **One predictable property.** Every result is a self-describing `blurPlaceholder` string, never a JSON envelope.
- **Safe for existing media.** A fingerprinted background pass processes the library once per output configuration and resumes if it is interrupted.
- **Visible in the backoffice.** Preview the generated result and inspect its representation, dimensions, and stored size.

## Install

Blur Placeholder supports Umbraco CMS 17.1 through 18.x. Add it to the Umbraco web project:

```sh
dotnet add package TheBuilder.BlurPlaceholder
```

The package registers its services and backoffice extension automatically. Then:

1. Restart the Umbraco application. The first startup adds the read-only `blurPlaceholder` property to the default Image media type.
2. Upload or replace an Image media item and save it.
3. Check the **Blur placeholder** property on that media item for the generated preview.

## Use it from the Delivery API

Request `blurPlaceholder` by name, which keeps the extra media payload opt-in:

```http
GET /umbraco/delivery/api/v2/media/item/{mediaId}?expand=properties[$all]&fields=properties[blurPlaceholder]
```

With the default configuration, the returned value works directly as a blur data URL. See the [Delivery API guide](https://blur.thebuilder.dk/delivery-api) for Next.js, Nuxt, BlurHash, and ThumbHash examples.

[Read the documentation](https://blur.thebuilder.dk/) · [View on GitHub](https://github.com/thebuilder/blur-placeholder) · [Install from NuGet](https://www.nuget.org/packages/TheBuilder.BlurPlaceholder)
