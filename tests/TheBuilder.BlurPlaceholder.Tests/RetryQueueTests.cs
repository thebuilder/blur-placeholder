using Umbraco.Cms.Core.Services;
using TheBuilder.BlurPlaceholder.Media;

namespace TheBuilder.BlurPlaceholder.Tests;

public sealed class RetryQueueTests
{
    [Fact]
    public void Completed_entries_are_compacted_from_the_durable_record()
    {
        var store = new InMemoryKeyValueService();
        store.SetValue(Constants.RetryQueueKey, "[]");
        var queue = new BlurPlaceholderRetryQueue(store);
        var mediaKey = Guid.NewGuid();

        queue.Enqueue(mediaKey);
        queue.Complete(mediaKey);

        Assert.Empty(queue.GetPending());
        Assert.Equal("[]", store.GetValue(Constants.RetryQueueKey));
    }

    [Fact]
    public void A_stale_retry_token_cannot_complete_a_newer_retry()
    {
        var store = new InMemoryKeyValueService();
        store.SetValue(Constants.RetryQueueKey, "[]");
        var queue = new BlurPlaceholderRetryQueue(store);
        var mediaKey = Guid.NewGuid();

        queue.Enqueue(mediaKey);
        var staleRetry = Assert.Single(queue.GetPending());
        queue.Enqueue(mediaKey);

        Assert.False(queue.TryComplete(staleRetry));
        var currentRetry = Assert.Single(queue.GetPending());
        Assert.NotEqual(staleRetry.Token, currentRetry.Token);
    }

    [Fact]
    public void Enqueue_requires_the_migration_to_initialize_the_durable_record()
    {
        var queue = new BlurPlaceholderRetryQueue(new InMemoryKeyValueService());

        var exception = Assert.Throws<InvalidOperationException>(() => queue.Enqueue(Guid.NewGuid()));

        Assert.Contains("package migration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Corrupt_durable_state_is_not_silently_overwritten()
    {
        var store = new InMemoryKeyValueService();
        store.SetValue(Constants.RetryQueueKey, "not-json");
        var queue = new BlurPlaceholderRetryQueue(store);

        var exception = Assert.Throws<InvalidOperationException>(() => queue.Enqueue(Guid.NewGuid()));

        Assert.Contains("invalid JSON", exception.Message, StringComparison.Ordinal);
        Assert.Equal("not-json", store.GetValue(Constants.RetryQueueKey));
    }

    [Fact]
    public void Enqueue_reports_exhausted_concurrent_updates()
    {
        var store = new InMemoryKeyValueService { RejectCompareAndSet = true };
        store.SetValue(Constants.RetryQueueKey, "[]");
        var queue = new BlurPlaceholderRetryQueue(store);

        var exception = Assert.Throws<InvalidOperationException>(() => queue.Enqueue(Guid.NewGuid()));

        Assert.Contains("concurrent changes", exception.Message, StringComparison.Ordinal);
        Assert.Empty(queue.GetPending());
    }

    private sealed class InMemoryKeyValueService : IKeyValueService
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        public bool RejectCompareAndSet { get; init; }

        public string? GetValue(string key) => _values.GetValueOrDefault(key);

        public IReadOnlyDictionary<string, string?> FindByKeyPrefix(string keyPrefix) =>
            _values
                .Where(pair => pair.Key.StartsWith(keyPrefix, StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.Ordinal);

        public void SetValue(string key, string value) => _values[key] = value;

        public void SetValue(string key, string originValue, string newValue)
        {
            if (!string.Equals(GetValue(key), originValue, StringComparison.Ordinal))
                throw new InvalidOperationException("The key value changed before the compare-and-set operation.");

            _values[key] = newValue;
        }

        public bool TrySetValue(string key, string originValue, string newValue)
        {
            if (RejectCompareAndSet) return false;
            if (!string.Equals(GetValue(key), originValue, StringComparison.Ordinal)) return false;

            _values[key] = newValue;
            return true;
        }
    }
}
