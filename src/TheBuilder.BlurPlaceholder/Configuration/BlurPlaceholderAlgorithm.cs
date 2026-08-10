namespace TheBuilder.BlurPlaceholder.Configuration;

/// <summary>Selects the representation generated for image media.</summary>
public enum BlurPlaceholderAlgorithm
{
    /// <summary>A small WebP data URL.</summary>
    Webp,
    /// <summary>A BlurHash string, optionally decoded to a WebP data URL.</summary>
    BlurHash,
    /// <summary>A ThumbHash value, optionally decoded to a WebP data URL.</summary>
    ThumbHash,
}
