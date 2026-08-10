using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using TheBuilder.BlurPlaceholder;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Generation;

var repositoryRoot = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Directory.GetCurrentDirectory();
var outputDirectory = Path.Combine(repositoryRoot, "apps", "docs", "public", "demos", "generated");
Directory.CreateDirectory(outputDirectory);

var sources = new[]
{
    new DemoSource("alpine-lake", "alpine-lake.png", "Wide alpine lake at sunrise"),
    new DemoSource("greenhouse-portrait", "greenhouse-portrait.png", "Tall bird-of-paradise plant in a greenhouse"),
    new DemoSource("balloon-alpha", "balloon-alpha.png", "Paper hot-air balloon with transparency"),
};

var results = new List<DemoResult>();
foreach (var source in sources)
{
    var sourcePath = Path.Combine(repositoryRoot, "apps", "docs", "demo-sources", source.FileName);
    var webp = await GenerateAsync(sourcePath, BlurPlaceholderAlgorithm.Webp, decodeToDataUrl: true);
    var blurHashNative = await GenerateAsync(sourcePath, BlurPlaceholderAlgorithm.BlurHash, decodeToDataUrl: false);
    var blurHashWebp = await GenerateAsync(sourcePath, BlurPlaceholderAlgorithm.BlurHash, decodeToDataUrl: true);
    var thumbHashNative = await GenerateAsync(sourcePath, BlurPlaceholderAlgorithm.ThumbHash, decodeToDataUrl: false);
    var thumbHashWebp = await GenerateAsync(sourcePath, BlurPlaceholderAlgorithm.ThumbHash, decodeToDataUrl: true);

    await WriteSourcePreviewAsync(Path.Combine(outputDirectory, $"{source.Slug}-source.webp"), sourcePath);
    await WriteDataUrlAsync(Path.Combine(outputDirectory, $"{source.Slug}-webp.webp"), webp);
    await WriteDataUrlAsync(Path.Combine(outputDirectory, $"{source.Slug}-blurhash.webp"), blurHashWebp);
    await WriteDataUrlAsync(Path.Combine(outputDirectory, $"{source.Slug}-thumbhash.webp"), thumbHashWebp);

    results.Add(new DemoResult(
        source.Slug,
        source.Alt,
        new FileInfo(sourcePath).Length,
        DescribeDataUrl(webp),
        DescribeHash(blurHashNative, blurHashWebp),
        DescribeHash(thumbHashNative, thumbHashWebp)));
}

var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(Path.Combine(outputDirectory, "results.json"), json + Environment.NewLine);

static async Task<string> GenerateAsync(
    string sourcePath,
    BlurPlaceholderAlgorithm algorithm,
    bool decodeToDataUrl)
{
    var generator = new PlaceholderGenerator(Options.Create(new BlurPlaceholderOptions
    {
        Algorithm = algorithm,
        DecodeToDataUrl = decodeToDataUrl,
    }));
    await using var source = File.OpenRead(sourcePath);
    var result = await generator.GenerateAsync(source);
    if (string.IsNullOrWhiteSpace(result.Value))
        throw new InvalidOperationException($"{algorithm} generation failed for {sourcePath}: {result.Message}");
    return result.Value;
}

static async Task WriteDataUrlAsync(string path, string dataUrl)
{
    var commaIndex = dataUrl.IndexOf(',');
    if (commaIndex < 0) throw new InvalidOperationException("The generated value is not a data URL.");
    await File.WriteAllBytesAsync(path, Convert.FromBase64String(dataUrl[(commaIndex + 1)..]));
}

static async Task WriteSourcePreviewAsync(string path, string sourcePath)
{
    using var image = await Image.LoadAsync(sourcePath);
    image.Mutate(context =>
    {
        context.AutoOrient();
        context.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(1024, 1024),
        });
    });
    await image.SaveAsWebpAsync(path, new WebpEncoder
    {
        FileFormat = WebpFileFormatType.Lossy,
        Quality = 82,
    });
}

static DataUrlResult DescribeDataUrl(string value)
{
    var commaIndex = value.IndexOf(',');
    return new DataUrlResult(
        value.Length,
        Convert.FromBase64String(value[(commaIndex + 1)..]).Length);
}

static HashResult DescribeHash(string nativeValue, string dataUrl) => new(
    nativeValue,
    Encoding.UTF8.GetByteCount(nativeValue),
    DescribeDataUrl(dataUrl));

internal sealed record DemoSource(string Slug, string FileName, string Alt);
internal sealed record DemoResult(
    string Slug,
    string Alt,
    long SourceBytes,
    DataUrlResult Webp,
    HashResult BlurHash,
    HashResult ThumbHash);
internal sealed record DataUrlResult(int Characters, int BinaryBytes);
internal sealed record HashResult(string NativeValue, int NativeBytes, DataUrlResult DecodedWebp);
