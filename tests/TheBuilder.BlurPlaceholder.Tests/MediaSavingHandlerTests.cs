using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Media;
using TheBuilder.BlurPlaceholder.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace TheBuilder.BlurPlaceholder.Tests;

public sealed class MediaSavingHandlerTests
{
    [Fact]
    public async Task Dirty_image_is_processed_without_a_separate_durable_queue_update()
    {
        var media = CreateMedia();
        var processor = Substitute.For<IMediaPlaceholderProcessor>();
        processor.GenerateAsync(media, Arg.Any<CancellationToken>())
            .Returns(PlaceholderGenerationResult.RetryableFailure("temporary"));
        var handler = new BlurPlaceholderMediaSavingHandler(
            processor,
            Options.Create(new BlurPlaceholderOptions()),
            Substitute.For<ILogger<BlurPlaceholderMediaSavingHandler>>());

        await handler.HandleAsync(
            new MediaSavingNotification([media], new EventMessages()),
            CancellationToken.None);

        await processor.Received(1).GenerateAsync(media, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Disabled_generation_does_not_process_media()
    {
        var media = CreateMedia();
        var processor = Substitute.For<IMediaPlaceholderProcessor>();
        var handler = new BlurPlaceholderMediaSavingHandler(
            processor,
            Options.Create(new BlurPlaceholderOptions { Enabled = false }),
            Substitute.For<ILogger<BlurPlaceholderMediaSavingHandler>>());

        await handler.HandleAsync(
            new MediaSavingNotification([media], new EventMessages()),
            CancellationToken.None);

        await processor.DidNotReceive().GenerateAsync(Arg.Any<IMedia>(), Arg.Any<CancellationToken>());
    }

    private static IMedia CreateMedia()
    {
        var mediaType = Substitute.For<ISimpleContentType>();
        mediaType.Alias.Returns(Constants.DefaultImageMediaTypeAlias);

        var media = Substitute.For<IMedia>();
        media.ContentType.Returns(mediaType);
        media.HasProperty(Constants.PropertyAlias).Returns(true);
        media.IsPropertyDirty(Constants.SourcePropertyAlias).Returns(true);
        return media;
    }
}
