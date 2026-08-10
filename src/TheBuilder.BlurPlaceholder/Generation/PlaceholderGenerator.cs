using Blurhash.ImageSharp;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Models;
using TheBuilder.BlurPlaceholder.ThirdParty.ThumbHash;

namespace TheBuilder.BlurPlaceholder.Generation;

internal sealed class PlaceholderGenerator(IOptions<BlurPlaceholderOptions> options) : IPlaceholderGenerator
{
    private readonly BlurPlaceholderOptions _options = options.Value;

    public async Task<PlaceholderGenerationResult> GenerateAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return PlaceholderGenerationResult.Disabled;
        if (source is null || (source.CanSeek && source.Length == 0)) return PlaceholderGenerationResult.Empty;

        try
        {
            return _options.Algorithm switch
            {
                BlurPlaceholderAlgorithm.Webp => await GenerateWebpAsync(source, cancellationToken),
                BlurPlaceholderAlgorithm.BlurHash => await GenerateBlurHashAsync(source, cancellationToken),
                BlurPlaceholderAlgorithm.ThumbHash => await GenerateThumbHashAsync(source, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported algorithm {_options.Algorithm}."),
            };
        }
        catch (UnknownImageFormatException exception)
        {
            return PlaceholderGenerationResult.Unsupported(exception.Message);
        }
        catch (InvalidImageContentException exception)
        {
            return PlaceholderGenerationResult.Unsupported(exception.Message);
        }
        catch (NotSupportedException exception)
        {
            return PlaceholderGenerationResult.Unsupported(exception.Message);
        }
        catch (IOException exception)
        {
            return PlaceholderGenerationResult.RetryableFailure(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return PlaceholderGenerationResult.RetryableFailure(exception.Message);
        }
    }

    private async Task<PlaceholderGenerationResult> GenerateWebpAsync(Stream source, CancellationToken cancellationToken)
    {
        using var image = await ImageRasterizer.LoadAsync(source, _options.Webp.MaximumDimension, cancellationToken);
        var value = await PlaceholderImageEncoder.ToWebpDataUrlAsync(image, _options.Webp.Quality, cancellationToken);
        return PlaceholderGenerationResult.Generated(value);
    }

    private async Task<PlaceholderGenerationResult> GenerateBlurHashAsync(Stream source, CancellationToken cancellationToken)
    {
        using var image = await ImageRasterizer.LoadAsync(source, _options.BlurHash.MaximumDimension, cancellationToken);
        var hash = Blurhasher.Encode(image, _options.BlurHash.ComponentsX, _options.BlurHash.ComponentsY);
        if (!_options.DecodeToDataUrl)
            return PlaceholderGenerationResult.Generated(PrefixNativeValue(Constants.BlurHashPrefix, hash));

        var decodedSize = GetBlurHashDecodedSize(image.Width, image.Height);
        using var decoded = Blurhasher.Decode(hash, decodedSize.Width, decodedSize.Height, 1);
        var dataUrl = PlaceholderImageEncoder.ToWebpDataUrl(decoded, _options.DecodedDataUrl.WebpQuality);
        return PlaceholderGenerationResult.Generated(dataUrl);
    }

    private async Task<PlaceholderGenerationResult> GenerateThumbHashAsync(Stream source, CancellationToken cancellationToken)
    {
        using var image = await ImageRasterizer.LoadAsync(source, _options.ThumbHash.MaximumDimension, cancellationToken);
        var rgba = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(rgba);
        var hash = ThumbHashConvert.FromRgba(image.Width, image.Height, rgba);
        var nativeValue = PrefixNativeValue(Constants.ThumbHashPrefix, Convert.ToBase64String(hash.ToArray()));
        if (!_options.DecodeToDataUrl) return PlaceholderGenerationResult.Generated(nativeValue);

        var decoded = ThumbHashConvert.ToRgba(hash);
        using var decodedImage = Image.LoadPixelData<Rgba32>(decoded.Item3, decoded.Item1, decoded.Item2);
        var dataUrl = PlaceholderImageEncoder.ToWebpDataUrl(decodedImage, _options.DecodedDataUrl.WebpQuality);
        return PlaceholderGenerationResult.Generated(dataUrl);
    }

    internal static Size GetBlurHashDecodedSize(int width, int height, int maximumDimension = 32)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (maximumDimension <= 0) throw new ArgumentOutOfRangeException(nameof(maximumDimension));

        return width >= height
            ? new Size(maximumDimension, Math.Max(1, (int)Math.Round(maximumDimension * height / (double)width)))
            : new Size(Math.Max(1, (int)Math.Round(maximumDimension * width / (double)height)), maximumDimension);
    }

    private string PrefixNativeValue(string prefix, string value) =>
        _options.IncludeAlgorithmPrefix ? prefix + value : value;
}
