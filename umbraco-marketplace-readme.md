![Preview and copy a generated placeholder from an Image media item](https://raw.githubusercontent.com/thebuilder/blur-placeholder/refs/heads/main/apps/docs/content/screenshots/blur-placeholder-media.png)

Blur Placeholder generates a compact loading placeholder whenever an Image changes in Umbraco. The result is stored on the media item, ready for a frontend to request without doing image processing during a public request.

## Why use it?

- **Tiny WebP by default:** pass the generated data URL directly to an image component with no client decoder.
- **BlurHash and ThumbHash when needed:** choose a compact native representation or have the package decode it to WebP before storage.
- **One predictable field:** every result is stored as a self-describing `blurPlaceholder` string, never a JSON envelope.
- **Safe for existing media:** fingerprinted background processing backfills images once per output configuration and resumes interrupted work.
- **Visible in the backoffice:** preview the generated result and inspect its representation, dimensions, and stored size.

## Install

Add the package to an Umbraco CMS 17.1 or later web project:

```sh
dotnet add package TheBuilder.BlurPlaceholder
```

The package registers itself, adds the read-only property to the default Image media type, and generates the default tiny WebP when an image is saved.

## Use it from the Delivery API

Request `blurPlaceholder` explicitly to keep the extra media payload opt-in:

```http
GET /umbraco/delivery/api/v2/media/item/{mediaId}?expand=properties[$all]&fields=properties[blurPlaceholder]
```

With the default configuration, the returned value can be used directly as a blur data URL. See the [Delivery API guide](https://blur-placeholder.vercel.app/delivery-api) for Next.js, Nuxt, BlurHash, and ThumbHash examples.

[Read the documentation](https://blur-placeholder.vercel.app/) · [View on GitHub](https://github.com/thebuilder/blur-placeholder) · [Install from NuGet](https://www.nuget.org/packages/TheBuilder.BlurPlaceholder)
