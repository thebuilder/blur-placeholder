using Microsoft.Extensions.Logging;
using TheBuilder.BlurPlaceholder.Generation;
using TheBuilder.BlurPlaceholder.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace TheBuilder.BlurPlaceholder.Media;

internal sealed class MediaPlaceholderProcessor(
    IPlaceholderGenerator generator,
    IMediaService mediaService,
    MediaUrlGeneratorCollection mediaUrlGenerators,
    ILogger<MediaPlaceholderProcessor> logger)
{
    public async Task<PlaceholderGenerationResult> GenerateAsync(
        IMedia media,
        CancellationToken cancellationToken = default)
    {
        if (!media.TryGetMediaPath(Constants.SourcePropertyAlias, mediaUrlGenerators, out string? mediaPath)
            || string.IsNullOrWhiteSpace(mediaPath))
        {
            var unsupported = PlaceholderGenerationResult.Unsupported("The image file path could not be resolved.");
            Apply(media, unsupported);
            return unsupported;
        }

        try
        {
            using Stream source = mediaService.GetMediaFileContentStream(mediaPath);
            var result = await generator.GenerateAsync(source, cancellationToken);
            Apply(media, result);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not read image media {MediaKey} at {MediaPath}", media.Key, mediaPath);
            return PlaceholderGenerationResult.RetryableFailure(exception.Message);
        }
    }

    private static void Apply(IMedia media, PlaceholderGenerationResult result)
    {
        if (result.ChangesPlaceholder)
            media.SetValue(Constants.PropertyAlias, result.Value);
    }
}
