using System.Text.Json;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.BlurPlaceholder.Media;

internal sealed class BlurPlaceholderRetryQueue(IKeyValueService keyValueService) : IBlurPlaceholderRetryQueue
{
    private const int MaxCompareAndSetAttempts = 8;

    public IReadOnlyCollection<BlurPlaceholderRetry> GetPending() =>
        ReadEntries()
            .Where(entry => entry.MediaKey != Guid.Empty && !string.IsNullOrWhiteSpace(entry.Token))
            .Select(entry => new BlurPlaceholderRetry(entry.MediaKey, entry.Token))
            .ToArray();

    public void Enqueue(Guid mediaKey)
    {
        var token = Guid.NewGuid().ToString("N");
        Update(entries =>
        {
            entries.RemoveAll(entry => entry.MediaKey == mediaKey);
            entries.Add(new RetryEntry(mediaKey, token));
            return true;
        });
    }

    public void Complete(Guid mediaKey) =>
        Update(entries => entries.RemoveAll(entry => entry.MediaKey == mediaKey) > 0);

    public bool TryComplete(BlurPlaceholderRetry retry)
    {
        for (var attempt = 0; attempt < MaxCompareAndSetAttempts; attempt++)
        {
            var current = ReadStoredValue();
            var entries = Deserialize(current);
            var index = entries.FindIndex(entry =>
                entry.MediaKey == retry.MediaKey && string.Equals(entry.Token, retry.Token, StringComparison.Ordinal));

            if (index < 0)
                return !entries.Any(entry => entry.MediaKey == retry.MediaKey);

            entries.RemoveAt(index);
            var updated = Serialize(entries);
            if (keyValueService.TrySetValue(Constants.RetryQueueKey, current, updated)) return true;
        }

        return false;
    }

    private void Update(Func<List<RetryEntry>, bool> mutation)
    {
        for (var attempt = 0; attempt < MaxCompareAndSetAttempts; attempt++)
        {
            var current = ReadStoredValue();
            var entries = Deserialize(current);
            if (!mutation(entries)) return;

            var updated = Serialize(entries);
            if (keyValueService.TrySetValue(Constants.RetryQueueKey, current, updated)) return;
        }

        throw new InvalidOperationException(
            $"The {Constants.PackageName} retry queue could not be updated after {MaxCompareAndSetAttempts} concurrent changes.");
    }

    private List<RetryEntry> ReadEntries() => Deserialize(ReadStoredValue());

    private string ReadStoredValue() =>
        keyValueService.GetValue(Constants.RetryQueueKey) ?? throw QueueNotInitialized();

    private static List<RetryEntry> Deserialize(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<List<RetryEntry>>(value)
                ?? throw CorruptQueue();
        }
        catch (JsonException exception)
        {
            throw CorruptQueue(exception);
        }
    }

    private static string Serialize(List<RetryEntry> entries) => JsonSerializer.Serialize(entries);

    private static InvalidOperationException QueueNotInitialized() => new(
        $"The {Constants.PackageName} retry queue is not initialized. Run the package migration before processing media.");

    private static InvalidOperationException CorruptQueue(Exception? innerException = null) => new(
        $"The {Constants.PackageName} retry queue contains invalid JSON. Repair {Constants.RetryQueueKey} before processing media.",
        innerException);

    private sealed record RetryEntry(Guid MediaKey, string Token);
}
