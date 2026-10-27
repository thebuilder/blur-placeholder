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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Resaving_an_image_with_an_empty_placeholder_generates_one(string? placeholder)
    {
        var media = CreateMedia(sourceDirty: false, placeholder: placeholder);
        var processor = Substitute.For<IMediaPlaceholderProcessor>();
        processor.GenerateAsync(media, Arg.Any<CancellationToken>())
            .Returns(PlaceholderGenerationResult.Generated("data:image/webp;base64,AA=="));
        var handler = CreateHandler(processor);

        await handler.HandleAsync(
            new MediaSavingNotification([media], new EventMessages()),
            CancellationToken.None);

        await processor.Received(1).GenerateAsync(media, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resaving_an_image_with_a_placeholder_keeps_it()
    {
        var media = CreateMedia(sourceDirty: false, placeholder: "data:image/webp;base64,AA==");
        var processor = Substitute.For<IMediaPlaceholderProcessor>();
        var handler = CreateHandler(processor);

        await handler.HandleAsync(
            new MediaSavingNotification([media], new EventMessages()),
            CancellationToken.None);

        await processor.DidNotReceive().GenerateAsync(Arg.Any<IMedia>(), Arg.Any<CancellationToken>());
    }

    private static BlurPlaceholderMediaSavingHandler CreateHandler(IMediaPlaceholderProcessor processor) =>
        new(
            processor,
            Options.Create(new BlurPlaceholderOptions()),
            Substitute.For<ILogger<BlurPlaceholderMediaSavingHandler>>());

    private static IMedia CreateMedia(bool sourceDirty = true, string? placeholder = null)
    {
        var mediaType = Substitute.For<ISimpleContentType>();
        mediaType.Alias.Returns(Constants.DefaultImageMediaTypeAlias);

        var media = Substitute.For<IMedia>();
        media.ContentType.Returns(mediaType);
        media.HasProperty(Constants.PropertyAlias).Returns(true);
        media.IsPropertyDirty(Constants.SourcePropertyAlias).Returns(sourceDirty);
        media.GetValue<string>(Constants.PropertyAlias).Returns(placeholder);
        return media;
    }
}
