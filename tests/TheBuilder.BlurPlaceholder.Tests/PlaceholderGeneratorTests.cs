using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Generation;
using TheBuilder.BlurPlaceholder.Models;
using TheBuilder.BlurPlaceholder.ThirdParty.ThumbHash;

namespace TheBuilder.BlurPlaceholder.Tests;

public sealed class PlaceholderGeneratorTests
{
    [Fact]
    public async Task Webp_output_is_a_small_webp_data_url()
    {
        var result = await GenerateAsync(new BlurPlaceholderOptions
        {
            Algorithm = BlurPlaceholderAlgorithm.Webp,
            DecodeToDataUrl = true,
        });

        Assert.Equal(PlaceholderGenerationStatus.Generated, result.Status);
        var value = Assert.IsType<string>(result.Mutation.Value);
        Assert.StartsWith(Constants.WebpDataUrlPrefix, value, StringComparison.Ordinal);

        using var decoded = Image.Load(Convert.FromBase64String(value[Constants.WebpDataUrlPrefix.Length..]));
        Assert.InRange(decoded.Width, 1, 16);
        Assert.InRange(decoded.Height, 1, 16);
    }

    [Fact]
    public async Task BlurHash_can_be_persisted_as_native_output()
    {
        var result = await GenerateAsync(new BlurPlaceholderOptions
        {
            Algorithm = BlurPlaceholderAlgorithm.BlurHash,
            DecodeToDataUrl = false,
        });

        Assert.Equal(PlaceholderGenerationStatus.Generated, result.Status);
        Assert.StartsWith(Constants.BlurHashPrefix, result.Mutation.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlurHash_input_size_is_independent_of_Webp_configuration()
    {
        var first = new BlurPlaceholderOptions
        {
            Algorithm = BlurPlaceholderAlgorithm.BlurHash,
            DecodeToDataUrl = false,
        };
        var second = new BlurPlaceholderOptions
        {
            Algorithm = BlurPlaceholderAlgorithm.BlurHash,
            DecodeToDataUrl = false,
        };
        second.Webp.MaximumDimension = 64;

        var firstResult = await GenerateAsync(first);
        var secondResult = await GenerateAsync(second);

        Assert.Equal(firstResult.Mutation.Value, secondResult.Mutation.Value);
    }

    [Fact]
    public async Task BlurHash_can_be_decoded_to_webp_output()
    {
        var result = await GenerateAsync(new BlurPlaceholderOptions
        {
            Algorithm = BlurPlaceholderAlgorithm.BlurHash,
            DecodeToDataUrl = true,
        });

        Assert.Equal(PlaceholderGenerationStatus.Generated, result.Status);
        Assert.StartsWith(Constants.WebpDataUrlPrefix, result.Mutation.Value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(96, 32, 32, 11)]
    [InlineData(32, 96, 11, 32)]
    [InlineData(64, 64, 32, 32)]
    public void BlurHash_decoded_dimensions_preserve_the_source_aspect_ratio(
        int sourceWidth,
        int sourceHeight,
        int expectedWidth,
        int expectedHeight)
    {
        var decodedSize = PlaceholderGenerator.GetBlurHashDecodedSize(sourceWidth, sourceHeight);

        Assert.Equal(expectedWidth, decodedSize.Width);
        Assert.Equal(expectedHeight, decodedSize.Height);
    }

    [Fact]
    public async Task ThumbHash_can_be_persisted_as_native_output()
    {
        var result = await GenerateAsync(new BlurPlaceholderOptions
        {
            Algorithm = BlurPlaceholderAlgorithm.ThumbHash,
            DecodeToDataUrl = false,
        });

        Assert.Equal(PlaceholderGenerationStatus.Generated, result.Status);
        Assert.StartsWith(Constants.ThumbHashPrefix, result.Mutation.Value, StringComparison.Ordinal);
        Assert.Equal("thumbhash:4RUKNZhwd3eBiHh3iHiIh4BxB+eI", result.Mutation.Value);
    }

    [Fact]
    public async Task ThumbHash_can_be_decoded_to_webp_output()
    {
        var result = await GenerateAsync(new BlurPlaceholderOptions
        {
            Algorithm = BlurPlaceholderAlgorithm.ThumbHash,
            DecodeToDataUrl = true,
        });

        Assert.Equal(PlaceholderGenerationStatus.Generated, result.Status);
        Assert.StartsWith(Constants.WebpDataUrlPrefix, result.Mutation.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void ThumbHash_matches_the_official_javascript_encoder_for_the_gradient_fixture()
    {
        const int width = 48;
        const int height = 32;
        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                rgba[offset] = (byte)(x * 5);
                rgba[offset + 1] = (byte)(y * 7);
                rgba[offset + 2] = 180;
                rgba[offset + 3] = 255;
            }
        }

        var hash = ThumbHashConvert.FromRgba(width, height, rgba);

        Assert.Equal(
            "4RUKNZhwd3eBiHh3iHiIh4BxB+eI",
            Convert.ToBase64String(hash.ToArray()));
    }

    [Fact]
    public async Task Unsupported_input_is_not_retried()
    {
        var generator = new PlaceholderGenerator(Options.Create(new BlurPlaceholderOptions()));
        await using var source = new MemoryStream("not an image"u8.ToArray());

        var result = await generator.GenerateAsync(source);

        Assert.Equal(PlaceholderGenerationStatus.Unsupported, result.Status);
    }

    private static async Task<PlaceholderGenerationResult> GenerateAsync(BlurPlaceholderOptions options)
    {
        var generator = new PlaceholderGenerator(Options.Create(options));
        await using var source = await CreateImageAsync();
        return await generator.GenerateAsync(source);
    }

    private static async Task<MemoryStream> CreateImageAsync()
    {
        var image = new Image<Rgba32>(48, 32);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    row[x] = new Rgba32((byte)(x * 5), (byte)(y * 7), 180, 255);
            }
        });

        await using var output = new MemoryStream();
        await image.SaveAsPngAsync(output, new PngEncoder());
        image.Dispose();
        output.Position = 0;
        var copy = new MemoryStream(output.ToArray());
        return copy;
    }
}
