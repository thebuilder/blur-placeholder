namespace TheBuilder.BlurPlaceholder.Media;

internal interface IBlurPlaceholderRetryQueue
{
    IReadOnlyCollection<BlurPlaceholderRetry> GetPending();

    void Enqueue(Guid mediaKey);

    void Complete(Guid mediaKey);

    bool TryComplete(BlurPlaceholderRetry retry);
}

internal readonly record struct BlurPlaceholderRetry(Guid MediaKey, string Token);
