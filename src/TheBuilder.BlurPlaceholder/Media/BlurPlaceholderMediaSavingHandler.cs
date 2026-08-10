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

        foreach (IMedia media in notification.SavedEntities.Where(ShouldGenerate))
        {
            var result = await processor.GenerateAsync(media, cancellationToken);
            if (result.Status == PlaceholderGenerationStatus.RetryableFailure)
            {
                logger.LogWarning(
                    "Cleared the blur placeholder for image media {MediaKey}; maintenance will retry it: {Message}",
                    media.Key,
                    result.Message);
            }
        }
    }

    internal static bool ShouldGenerate(IMedia media) =>
        media.ContentType.Alias.Equals(Constants.DefaultImageMediaTypeAlias, StringComparison.OrdinalIgnoreCase)
        && media.HasProperty(Constants.PropertyAlias)
        && media.IsPropertyDirty(Constants.SourcePropertyAlias);
}
