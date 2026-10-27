using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace TheBuilder.BlurPlaceholder.Generation;

internal static class PlaceholderImageEncoder
{
    public static async Task<string> ToWebpDataUrlAsync(
        Image image,
        int quality,
        CancellationToken cancellationToken)
    {
        StripMetadata(image);
        await using var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, CreateEncoder(quality), cancellationToken);

        return string.Concat(Constants.WebpDataUrlPrefix, Convert.ToBase64String(output.ToArray()));
    }

    public static string ToWebpDataUrl(Image image, int quality)
    {
        StripMetadata(image);
        using var output = new MemoryStream();
        image.SaveAsWebp(output, CreateEncoder(quality));

        return string.Concat(Constants.WebpDataUrlPrefix, Convert.ToBase64String(output.ToArray()));
    }

    // Source EXIF, XMP and ICC profiles would otherwise dwarf the tiny image data.
    private static WebpEncoder CreateEncoder(int quality) => new()
    {
        FileFormat = WebpFileFormatType.Lossy,
        Quality = quality,
        SkipMetadata = true,
    };

    // SkipMetadata does not stop the WebP encoder from writing an ICCP chunk,
    // so the profiles are cleared on the image as well.
    private static void StripMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.CicpProfile = null;
        foreach (var frame in image.Frames)
        {
            frame.Metadata.ExifProfile = null;
            frame.Metadata.XmpProfile = null;
            frame.Metadata.IccProfile = null;
            frame.Metadata.IptcProfile = null;
            frame.Metadata.CicpProfile = null;
        }
    }
}
