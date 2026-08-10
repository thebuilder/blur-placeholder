namespace TheBuilder.BlurPlaceholder.Models;

internal enum PlaceholderGenerationStatus
{
    Generated,
    Empty,
    Disabled,
    Unsupported,
    RetryableFailure,
}

internal sealed record PlaceholderGenerationResult(
    PlaceholderGenerationStatus Status,
    string? Value,
    string? Message = null)
{
    public bool ChangesPlaceholder => Value is not null;

    public static PlaceholderGenerationResult Empty { get; } = new(PlaceholderGenerationStatus.Empty, string.Empty);

    public static PlaceholderGenerationResult Disabled { get; } = new(PlaceholderGenerationStatus.Disabled, null);

    public static PlaceholderGenerationResult Generated(string value) =>
        new(PlaceholderGenerationStatus.Generated, value);

    public static PlaceholderGenerationResult Unsupported(string message) =>
        new(PlaceholderGenerationStatus.Unsupported, string.Empty, message);

    public static PlaceholderGenerationResult RetryableFailure(string message) =>
        new(PlaceholderGenerationStatus.RetryableFailure, null, message);
}
