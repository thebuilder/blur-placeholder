using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Models;

namespace TheBuilder.BlurPlaceholder.Media;

internal sealed class BlurPlaceholderMediaSavingHandler(
    IMediaPlaceholderProcessor processor,
    IOptions<BlurPlaceholderOptions> options,
    ILogger<BlurPlaceholderMediaSavingHandler> logger)
    : INotificationAsyncHandler<MediaSavingNotification>
{
    public async Task HandleAsync(MediaSavingNotification notification, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return;

        foreach (IMedia media in notification.SavedEntities.Where(IsImageWithPlaceholder))
        {
            if (BlurPlaceholderMaintenanceSave.IsActive)
            {
                logger.LogDebug(
                    "Left the blur placeholder for image media {MediaKey} to maintenance, which is saving it",
                    media.Key);
                continue;
            }

            var reason = GetGenerationReason(media);
            if (reason is null)
            {
                logger.LogDebug(
                    "Kept the blur placeholder for image media {MediaKey}; the source file is unchanged",
                    media.Key);
                continue;
            }

            var result = await processor.GenerateAsync(media, cancellationToken);
            if (result.Status == PlaceholderGenerationStatus.RetryableFailure)
            {
                logger.LogWarning(
                    "Cleared the blur placeholder for image media {MediaKey}; maintenance will retry it: {Message}",
                    media.Key,
                    result.Message);
            }
            else
            {
                logger.LogDebug(
                    "Processed the blur placeholder for image media {MediaKey} because the {Reason}: {Status}",
                    media.Key,
                    reason,
                    result.Status);
            }
        }
    }

    // An empty placeholder is filled on any save, so re-saving an image fixes it without waiting
    // for maintenance. Saves made by maintenance itself are skipped above.
    internal static string? GetGenerationReason(IMedia media)
    {
        if (media.IsPropertyDirty(Constants.SourcePropertyAlias)) return "source file changed";
        if (string.IsNullOrWhiteSpace(media.GetValue<string>(Constants.PropertyAlias))) return "placeholder is empty";
        return null;
    }

    private static bool IsImageWithPlaceholder(IMedia media) =>
        media.ContentType.Alias.Equals(Constants.DefaultImageMediaTypeAlias, StringComparison.OrdinalIgnoreCase)
        && media.HasProperty(Constants.PropertyAlias);
}
