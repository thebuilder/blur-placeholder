using System.Security.Cryptography;
using System.Text;

namespace TheBuilder.BlurPlaceholder.Configuration;

internal static class BlurPlaceholderFingerprint
{
    public static string Create(BlurPlaceholderOptions options)
    {
        var canonical = string.Join(
            "|",
            options.Algorithm,
            options.DecodeToDataUrl,
            options.Webp.MaximumDimension,
            options.Webp.Quality,
            options.BlurHash.MaximumDimension,
            options.BlurHash.ComponentsX,
            options.BlurHash.ComponentsY,
            options.ThumbHash.MaximumDimension,
            options.DecodedDataUrl.WebpQuality);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
