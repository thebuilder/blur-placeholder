using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace TheBuilder.BlurPlaceholder.Media;

internal sealed class BlurPlaceholderMaintenanceJob(
    IKeyValueService keyValueService,
    IMediaService mediaService,
    IMediaTypeService mediaTypeService,
    MediaUrlGeneratorCollection mediaUrlGenerators,
    IMediaPlaceholderProcessor processor,
    IOptions<BlurPlaceholderOptions> options,
    ILogger<BlurPlaceholderMaintenanceJob> logger) : IDistributedBackgroundJob
{
    private const int PageSize = 50;

    public string Name => "Blur Placeholder maintenance";

    public TimeSpan Period => BlurPlaceholderMaintenanceSchedule.PollInterval;

    public Task ExecuteAsync() => ExecuteAsync(CancellationToken.None);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Value.Enabled) return;

        var fingerprint = BlurPlaceholderFingerprint.Create(options.Value);
        var previousFingerprint = keyValueService.GetValue(Constants.BackfillFingerprintKey);
        var configurationChanged = options.Value.BackfillExisting
            && !string.Equals(previousFingerprint, fingerprint, StringComparison.Ordinal);
        var forceRegeneration = configurationChanged && previousFingerprint is not null;

        var now = DateTimeOffset.UtcNow;
        var lastScan = ReadLastScan();
        if (lastScan is null && !options.Value.BackfillExisting)
        {
            // Without a backfill the first scan for empty placeholders waits a full RetryInterval.
            WriteLastScan(now);
            lastScan = now;
        }

        var due = BlurPlaceholderMaintenanceSchedule.IsDue(
            configurationChanged,
            HasPendingCursor(fingerprint, forceRegeneration),
            lastScan,
            options.Value.RetryInterval,
            now);
        if (!due) return;

        if (!await ProcessImagesAsync(fingerprint, forceRegeneration, cancellationToken)) return;

        WriteLastScan(DateTimeOffset.UtcNow);
        if (configurationChanged)
            keyValueService.SetValue(Constants.BackfillFingerprintKey, fingerprint);
    }

    private async Task<bool> ProcessImagesAsync(
        string fingerprint,
        bool forceRegeneration,
        CancellationToken cancellationToken)
    {
        var imageMediaType = mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias);
        if (imageMediaType is null || !imageMediaType.PropertyTypes.Any(property => property.Alias == Constants.PropertyAlias))
            return false;

        var cursor = ReadCursor(fingerprint, forceRegeneration);
        var pageIndex = cursor.NextPageIndex;
        long totalRecords;
        var generated = 0;
        var retryCount = 0;
        var skipped = 0;
        var stopwatch = Stopwatch.StartNew();

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var images = mediaService.GetPagedOfType(imageMediaType.Id, pageIndex, PageSize, out totalRecords);
            foreach (var image in images.Where(media => forceRegeneration || NeedsBackfill(media)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var previousValue = image.GetValue<string>(Constants.PropertyAlias);
                var result = await processor.GenerateAsync(image, cancellationToken);
                var currentValue = image.GetValue<string>(Constants.PropertyAlias);
                if (result.Mutation.Kind != PlaceholderMutationKind.Keep
                    && !string.Equals(previousValue, currentValue, StringComparison.Ordinal))
                {
                    mediaService.Save(image);
                }

                if (result.Status == PlaceholderGenerationStatus.Generated)
                {
                    generated++;
                }
                else if (result.Status == PlaceholderGenerationStatus.RetryableFailure)
                {
                    retryCount++;
                }
                else
                {
                    skipped++;
                }
            }

            pageIndex++;
            WriteCursor(new BlurPlaceholderMaintenanceCursor(fingerprint, forceRegeneration, pageIndex));

            if (pageIndex * PageSize < totalRecords && stopwatch.Elapsed >= BlurPlaceholderMaintenanceSchedule.RunBudget)
            {
                logger.LogInformation(
                    "Blur placeholder maintenance paused at page {PageIndex} after generating {GeneratedCount}, retaining {RetryCount} items for retry, and skipping {SkippedCount} media items; the next run resumes it",
                    pageIndex,
                    generated,
                    retryCount,
                    skipped);
                return false;
            }
        }
        while (pageIndex * PageSize < totalRecords);

        ClearCursor();

        logger.LogInformation(
            "Blur placeholder maintenance generated {GeneratedCount}, retained {RetryCount} items for retry, and skipped {SkippedCount} media items",
            generated,
            retryCount,
            skipped);
        return true;
    }

    private BlurPlaceholderMaintenanceCursor ReadCursor(string fingerprint, bool forceRegeneration)
    {
        var stored = keyValueService.GetValue(Constants.MaintenanceCursorKey);
        if (BlurPlaceholderMaintenanceCursor.TryRestore(stored, fingerprint, forceRegeneration, out var cursor))
            return cursor;

        if (!string.IsNullOrWhiteSpace(stored))
        {
            logger.LogWarning("Ignored an invalid or obsolete blur placeholder maintenance cursor");
        }

        return BlurPlaceholderMaintenanceCursor.Start(fingerprint, forceRegeneration);
    }

    private bool HasPendingCursor(string fingerprint, bool forceRegeneration)
    {
        var stored = keyValueService.GetValue(Constants.MaintenanceCursorKey);
        return !string.IsNullOrWhiteSpace(stored)
            && BlurPlaceholderMaintenanceCursor.TryRestore(stored, fingerprint, forceRegeneration, out _);
    }

    private DateTimeOffset? ReadLastScan() =>
        DateTimeOffset.TryParse(
            keyValueService.GetValue(Constants.LastMaintenanceScanKey),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var lastScan)
            ? lastScan
            : null;

    private void WriteLastScan(DateTimeOffset value) =>
        keyValueService.SetValue(Constants.LastMaintenanceScanKey, value.ToString("O", CultureInfo.InvariantCulture));

    private void WriteCursor(BlurPlaceholderMaintenanceCursor cursor) =>
        keyValueService.SetValue(Constants.MaintenanceCursorKey, JsonSerializer.Serialize(cursor));

    private void ClearCursor() => keyValueService.SetValue(Constants.MaintenanceCursorKey, string.Empty);

    private bool NeedsBackfill(IMedia media) =>
        CanProcess(media)
        && media.HasProperty(Constants.PropertyAlias)
        && string.IsNullOrWhiteSpace(media.GetValue<string>(Constants.PropertyAlias));

    private bool CanProcess(IMedia media) =>
        media.ContentType.Alias.Equals(Constants.DefaultImageMediaTypeAlias, StringComparison.OrdinalIgnoreCase)
        && media.HasProperty(Constants.SourcePropertyAlias)
        && media.TryGetMediaPath(Constants.SourcePropertyAlias, mediaUrlGenerators, out string? mediaPath)
        && !string.IsNullOrWhiteSpace(mediaPath);
}

internal static class BlurPlaceholderMaintenanceSchedule
{
    // Umbraco registers a new distributed job as if it had just run, so a Period equal to
    // RetryInterval delayed the first backfill by 12 hours. The job polls often instead and
    // enforces RetryInterval from the stored time of the last completed scan.
    public static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    // Another server may take over a run that lasts longer than Period plus Umbraco's
    // MaximumExecutionTime (5 minutes by default), so long passes continue on the next run.
    public static readonly TimeSpan RunBudget = TimeSpan.FromMinutes(2);

    public static bool IsDue(
        bool configurationChanged,
        bool cursorPending,
        DateTimeOffset? lastScan,
        TimeSpan retryInterval,
        DateTimeOffset now) =>
        configurationChanged
        || cursorPending
        || lastScan is null
        || now - lastScan.Value >= retryInterval;
}

internal sealed record BlurPlaceholderMaintenanceCursor(
    string Fingerprint,
    bool ForceRegeneration,
    long NextPageIndex)
{
    public static BlurPlaceholderMaintenanceCursor Start(string fingerprint, bool forceRegeneration) =>
        new(fingerprint, forceRegeneration, 0);

    public static bool TryRestore(
        string? stored,
        string fingerprint,
        bool forceRegeneration,
        out BlurPlaceholderMaintenanceCursor cursor)
    {
        cursor = Start(fingerprint, forceRegeneration);
        if (string.IsNullOrWhiteSpace(stored)) return true;

        try
        {
            var restored = JsonSerializer.Deserialize<BlurPlaceholderMaintenanceCursor>(stored);
            if (restored is null
                || restored.NextPageIndex < 0
                || restored.Fingerprint != fingerprint
                || restored.ForceRegeneration != forceRegeneration)
            {
                return false;
            }

            cursor = restored;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
