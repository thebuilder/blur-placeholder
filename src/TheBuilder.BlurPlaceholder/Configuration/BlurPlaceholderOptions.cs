namespace TheBuilder.BlurPlaceholder.Configuration;

/// <summary>Configures blur-placeholder generation and maintenance.</summary>
public sealed class BlurPlaceholderOptions
{
    /// <summary>Gets or sets whether generation and maintenance are enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the placeholder algorithm.</summary>
    public BlurPlaceholderAlgorithm Algorithm { get; set; } = BlurPlaceholderAlgorithm.Webp;

    /// <summary>Gets or sets whether native hashes are decoded to WebP data URLs before storage.</summary>
    public bool DecodeToDataUrl { get; set; } = true;

    /// <summary>Gets or sets whether native hash values include their algorithm prefix.</summary>
    public bool IncludeAlgorithmPrefix { get; set; } = true;

    /// <summary>Gets or sets whether existing images are processed when output settings change.</summary>
    public bool BackfillExisting { get; set; } = true;

    /// <summary>Gets or sets the targeted retry-job interval.</summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>Gets the direct WebP settings.</summary>
    public WebpOptions Webp { get; set; } = new();

    /// <summary>Gets the BlurHash settings.</summary>
    public BlurHashOptions BlurHash { get; set; } = new();

    /// <summary>Gets the ThumbHash settings.</summary>
    public ThumbHashOptions ThumbHash { get; set; } = new();

    /// <summary>Gets the settings applied when a native hash is decoded for storage.</summary>
    public DecodedDataUrlOptions DecodedDataUrl { get; set; } = new();
}

/// <summary>Configures direct WebP output.</summary>
public sealed class WebpOptions
{
    /// <summary>Gets or sets the longest output edge in pixels.</summary>
    public int MaximumDimension { get; set; } = 16;

    /// <summary>Gets or sets lossy WebP quality.</summary>
    public int Quality { get; set; } = 60;
}

/// <summary>Configures BlurHash encoding.</summary>
public sealed class BlurHashOptions
{
    /// <summary>Gets or sets the longest input edge passed to the encoder.</summary>
    public int MaximumDimension { get; set; } = 32;

    /// <summary>Gets or sets the horizontal component count.</summary>
    public int ComponentsX { get; set; } = 4;

    /// <summary>Gets or sets the vertical component count.</summary>
    public int ComponentsY { get; set; } = 3;
}

/// <summary>Configures ThumbHash encoding.</summary>
public sealed class ThumbHashOptions
{
    /// <summary>Gets or sets the longest input edge passed to the encoder.</summary>
    public int MaximumDimension { get; set; } = 100;
}

/// <summary>Configures decoded hash output.</summary>
public sealed class DecodedDataUrlOptions
{
    /// <summary>Gets or sets the quality of the decoded WebP data URL.</summary>
    public int WebpQuality { get; set; } = 60;
}
