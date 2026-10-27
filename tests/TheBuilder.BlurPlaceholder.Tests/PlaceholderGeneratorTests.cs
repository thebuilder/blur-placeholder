using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
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

    [Theory]
    [InlineData(BlurPlaceholderAlgorithm.Webp)]
    [InlineData(BlurPlaceholderAlgorithm.BlurHash)]
    [InlineData(BlurPlaceholderAlgorithm.ThumbHash)]
    public async Task Webp_output_omits_source_metadata(BlurPlaceholderAlgorithm algorithm)
    {
        await using var source = await CreateImageWithMetadataAsync();
        using (var sourceImage = Image.Load(source.ToArray()))
        {
            Assert.NotNull(sourceImage.Metadata.ExifProfile);
            Assert.NotNull(sourceImage.Metadata.XmpProfile);
            Assert.NotNull(sourceImage.Metadata.IccProfile);
            Assert.NotNull(sourceImage.Metadata.IptcProfile);
        }

        var generator = new PlaceholderGenerator(Options.Create(new BlurPlaceholderOptions
        {
            Algorithm = algorithm,
            DecodeToDataUrl = true,
        }));
        var result = await generator.GenerateAsync(source);

        Assert.Equal(PlaceholderGenerationStatus.Generated, result.Status);
        var value = Assert.IsType<string>(result.Mutation.Value);
        Assert.StartsWith(Constants.WebpDataUrlPrefix, value, StringComparison.Ordinal);
        var bytes = Convert.FromBase64String(value[Constants.WebpDataUrlPrefix.Length..]);
        var chunks = ReadWebpChunks(bytes);
        Assert.DoesNotContain("EXIF", chunks);
        Assert.DoesNotContain("XMP ", chunks);
        Assert.DoesNotContain("ICCP", chunks);
        Assert.InRange(bytes.Length, 1, 256);
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

    private static async Task<MemoryStream> CreateImageWithMetadataAsync()
    {
        await using var png = await CreateImageAsync();
        using var image = Image.Load<Rgba32>(png.ToArray());

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Software, "Camera firmware 1.0");
        exif.SetValue(ExifTag.Copyright, new string('c', 400));
        image.Metadata.ExifProfile = exif;

        image.Metadata.XmpProfile = new XmpProfile(Encoding.UTF8.GetBytes(
            "<?xpacket begin=\"\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>"
            + "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">"
            + "<rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">"
            + $"<dc:description>{new string('x', 1000)}</dc:description>"
            + "</rdf:Description></rdf:RDF></x:xmpmeta><?xpacket end=\"w\"?>"));

        image.Metadata.IccProfile = new IccProfile(CreateMinimalIccProfile());

        var iptc = new IptcProfile();
        iptc.SetValue(IptcTag.Caption, "Caption");
        image.Metadata.IptcProfile = iptc;

        var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder());
        output.Position = 0;
        return output;
    }

    private static byte[] CreateMinimalIccProfile()
    {
        // A header-only sRGB-like display profile with an empty tag table.
        var profile = new byte[132];
        BinaryPrimitives.WriteUInt32BigEndian(profile.AsSpan(0), (uint)profile.Length);
        BinaryPrimitives.WriteUInt32BigEndian(profile.AsSpan(8), 0x04300000);
        Encoding.ASCII.GetBytes("mntr").CopyTo(profile, 12);
        Encoding.ASCII.GetBytes("RGB ").CopyTo(profile, 16);
        Encoding.ASCII.GetBytes("XYZ ").CopyTo(profile, 20);
        Encoding.ASCII.GetBytes("acsp").CopyTo(profile, 36);
        return profile;
    }

    private static List<string> ReadWebpChunks(byte[] webp)
    {
        Assert.Equal("RIFF", Encoding.ASCII.GetString(webp, 0, 4));
        Assert.Equal("WEBP", Encoding.ASCII.GetString(webp, 8, 4));

        var chunks = new List<string>();
        var offset = 12;
        while (offset + 8 <= webp.Length)
        {
            chunks.Add(Encoding.ASCII.GetString(webp, offset, 4));
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(webp.AsSpan(offset + 4));
            offset += 8 + size + (size & 1);
        }

        Assert.Equal(webp.Length, offset);
        return chunks;
    }
}
