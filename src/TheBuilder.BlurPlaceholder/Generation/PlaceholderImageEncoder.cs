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
        await using var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossy,
            Quality = quality,
        }, cancellationToken);

        return string.Concat(Constants.WebpDataUrlPrefix, Convert.ToBase64String(output.ToArray()));
    }

    public static string ToWebpDataUrl(Image image, int quality)
    {
        using var output = new MemoryStream();
        image.SaveAsWebp(output, new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossy,
            Quality = quality,
        });

        return string.Concat(Constants.WebpDataUrlPrefix, Convert.ToBase64String(output.ToArray()));
    }
}
