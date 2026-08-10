namespace TheBuilder.BlurPlaceholder.Models;

internal enum PlaceholderGenerationStatus
{
    Generated,
    Empty,
    Disabled,
    Unsupported,
    RetryableFailure,
}

internal enum PlaceholderMutationKind
{
    Keep,
    Clear,
    Set,
}

internal sealed record PlaceholderMutation(PlaceholderMutationKind Kind, string? Value)
{
    public static PlaceholderMutation Keep { get; } = new(PlaceholderMutationKind.Keep, null);

    public static PlaceholderMutation Clear { get; } = new(PlaceholderMutationKind.Clear, null);

    public static PlaceholderMutation Set(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new PlaceholderMutation(PlaceholderMutationKind.Set, value);
    }
}

internal sealed record PlaceholderGenerationResult(
    PlaceholderGenerationStatus Status,
    PlaceholderMutation Mutation,
    string? Message = null)
{
    public static PlaceholderGenerationResult Empty { get; } = new(PlaceholderGenerationStatus.Empty, PlaceholderMutation.Clear);

    public static PlaceholderGenerationResult Disabled { get; } = new(PlaceholderGenerationStatus.Disabled, PlaceholderMutation.Keep);

    public static PlaceholderGenerationResult Generated(string value) =>
        new(PlaceholderGenerationStatus.Generated, PlaceholderMutation.Set(value));

    public static PlaceholderGenerationResult Unsupported(string message) =>
        new(PlaceholderGenerationStatus.Unsupported, PlaceholderMutation.Clear, message);

    public static PlaceholderGenerationResult RetryableFailure(string message) =>
        new(PlaceholderGenerationStatus.RetryableFailure, PlaceholderMutation.Clear, message);
}
