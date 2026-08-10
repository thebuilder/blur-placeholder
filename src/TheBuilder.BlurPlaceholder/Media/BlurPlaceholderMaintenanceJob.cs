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
    MediaPlaceholderProcessor processor,
    IBlurPlaceholderRetryQueue retryQueue,
    IOptions<BlurPlaceholderOptions> options,
    ILogger<BlurPlaceholderMaintenanceJob> logger) : IDistributedBackgroundJob
{
    private const int PageSize = 50;

    public string Name => "Blur Placeholder maintenance";

    public TimeSpan Period => options.Value.RetryInterval;

    public Task ExecuteAsync() => ExecuteAsync(CancellationToken.None);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Value.Enabled) return;

        var fingerprint = BlurPlaceholderFingerprint.Create(options.Value);
        var previousFingerprint = keyValueService.GetValue(Constants.BackfillFingerprintKey);
        if (options.Value.BackfillExisting && !string.Equals(previousFingerprint, fingerprint, StringComparison.Ordinal))
        {
            if (await BackfillAsync(forceRegeneration: previousFingerprint is not null, cancellationToken))
                keyValueService.SetValue(Constants.BackfillFingerprintKey, fingerprint);
            return;
        }

        await RetryPendingAsync(cancellationToken);
    }

    private async Task<bool> BackfillAsync(bool forceRegeneration, CancellationToken cancellationToken)
    {
        var imageMediaType = mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias);
        if (imageMediaType is null || !imageMediaType.PropertyTypes.Any(property => property.Alias == Constants.PropertyAlias))
            return false;

        long pageIndex = 0;
        long totalRecords;
        var generated = 0;
        var queued = 0;
        var skipped = 0;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var images = mediaService.GetPagedOfType(imageMediaType.Id, pageIndex, PageSize, out totalRecords);
            foreach (var image in images.Where(media => forceRegeneration || NeedsBackfill(media)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await processor.GenerateAsync(image, cancellationToken);
                if (result.ChangesPlaceholder)
                    mediaService.Save(image);

                if (result.Status == PlaceholderGenerationStatus.Generated)
                {
                    generated++;
                }
                else if (result.Status == PlaceholderGenerationStatus.RetryableFailure)
                {
                    retryQueue.Enqueue(image.Key);
                    queued++;
                }
                else
                {
                    skipped++;
                }
            }

            pageIndex++;
        }
        while (pageIndex * PageSize < totalRecords);

        logger.LogInformation(
            "Blur placeholder backfill generated {GeneratedCount}, queued {QueuedCount}, and skipped {SkippedCount} media items",
            generated,
            queued,
            skipped);
        return true;
    }

    private async Task RetryPendingAsync(CancellationToken cancellationToken)
    {
        var pending = retryQueue.GetPending();
        var completed = 0;

        foreach (var retry in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var media = mediaService.GetById(retry.MediaKey);
            if (media is null || !CanProcess(media))
            {
                if (retryQueue.TryComplete(retry)) completed++;
                continue;
            }

            var result = await processor.GenerateAsync(media, cancellationToken);
            if (result.ChangesPlaceholder)
                mediaService.Save(media);
            if (result.Status == PlaceholderGenerationStatus.RetryableFailure) continue;
            if (retryQueue.TryComplete(retry)) completed++;
        }

        logger.LogInformation(
            "Blur placeholder retry processed {PendingCount} queued media items and completed {CompletedCount}",
            pending.Count,
            completed);
    }

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
