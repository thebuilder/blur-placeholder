using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Media;
using TheBuilder.BlurPlaceholder.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.BlurPlaceholder.Tests;

public sealed class MaintenanceJobTests
{
    [Fact]
    public void Job_polls_more_often_than_the_retry_interval()
    {
        var job = new JobFixture(new BlurPlaceholderOptions { RetryInterval = TimeSpan.FromHours(12) }).Job;

        Assert.True(job.Period <= TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task First_run_after_install_backfills_existing_images()
    {
        var fixture = new JobFixture(new BlurPlaceholderOptions());

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.Received(1).GenerateAsync(fixture.Image, Arg.Any<CancellationToken>());
        Assert.Equal(
            BlurPlaceholderFingerprint.Create(new BlurPlaceholderOptions()),
            fixture.Store[Constants.BackfillFingerprintKey]);
        Assert.True(fixture.Store.ContainsKey(Constants.LastMaintenanceScanKey));
    }

    [Fact]
    public async Task Run_within_the_retry_interval_after_a_completed_scan_does_nothing()
    {
        var options = new BlurPlaceholderOptions();
        var fixture = new JobFixture(options);
        fixture.Store[Constants.BackfillFingerprintKey] = BlurPlaceholderFingerprint.Create(options);
        fixture.Store[Constants.LastMaintenanceScanKey] = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O", CultureInfo.InvariantCulture);

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.DidNotReceive().GenerateAsync(Arg.Any<IMedia>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Changed_fingerprint_regenerates_without_waiting_for_the_retry_interval()
    {
        var fixture = new JobFixture(new BlurPlaceholderOptions(), placeholder: "data:image/webp;base64,AA==");
        fixture.Store[Constants.BackfillFingerprintKey] = "fingerprint-from-an-older-release";
        fixture.Store[Constants.LastMaintenanceScanKey] = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O", CultureInfo.InvariantCulture);

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.Received(1).GenerateAsync(fixture.Image, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Without_backfill_the_first_scan_waits_for_the_retry_interval()
    {
        var fixture = new JobFixture(new BlurPlaceholderOptions { BackfillExisting = false });

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.DidNotReceive().GenerateAsync(Arg.Any<IMedia>(), Arg.Any<CancellationToken>());
        Assert.True(fixture.Store.ContainsKey(Constants.LastMaintenanceScanKey));
    }

    [Theory]
    [InlineData(false, false, 30, false)]
    [InlineData(true, false, 30, true)]
    [InlineData(false, true, 30, true)]
    [InlineData(false, false, 720, true)]
    public void Maintenance_is_due_for_a_changed_fingerprint_a_pending_cursor_or_an_elapsed_interval(
        bool configurationChanged,
        bool cursorPending,
        int minutesSinceLastScan,
        bool expected)
    {
        var now = DateTimeOffset.UtcNow;

        var due = BlurPlaceholderMaintenanceSchedule.IsDue(
            configurationChanged,
            cursorPending,
            now.AddMinutes(-minutesSinceLastScan),
            TimeSpan.FromHours(12),
            now);

        Assert.Equal(expected, due);
    }

    private sealed class JobFixture
    {
        private const string MediaPath = "/media/image.jpg";

        public JobFixture(BlurPlaceholderOptions options, string? placeholder = null)
        {
            var keyValueService = Substitute.For<IKeyValueService>();
            keyValueService.GetValue(Arg.Any<string>())
                .Returns(call => Store.GetValueOrDefault(call.Arg<string>()));
            keyValueService.When(service => service.SetValue(Arg.Any<string>(), Arg.Any<string>()))
                .Do(call => Store[call.ArgAt<string>(0)] = call.ArgAt<string>(1));

            var placeholderPropertyType = Substitute.For<IPropertyType>();
            placeholderPropertyType.Alias.Returns(Constants.PropertyAlias);
            var imageMediaType = Substitute.For<IMediaType>();
            imageMediaType.Id.Returns(1);
            imageMediaType.PropertyTypes.Returns([placeholderPropertyType]);
            var mediaTypeService = Substitute.For<IMediaTypeService>();
            mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias).Returns(imageMediaType);

            Image = CreateImage(placeholder);
            var mediaService = Substitute.For<IMediaService>();
            mediaService.GetPagedOfType(Arg.Is(1), Arg.Is(0L), Arg.Any<int>(), out Arg.Any<long>(), Arg.Any<IQuery<IMedia>?>(), Arg.Any<Ordering?>())
                .Returns(call =>
                {
                    call[3] = 1L;
                    return new[] { Image };
                });

            var urlGenerator = Substitute.For<IMediaUrlGenerator>();
            urlGenerator.TryGetMediaPath(Arg.Any<string?>(), Arg.Any<object?>(), out Arg.Any<string?>())
                .Returns(call =>
                {
                    call[2] = MediaPath;
                    return true;
                });

            Processor = Substitute.For<IMediaPlaceholderProcessor>();
            Processor.GenerateAsync(Arg.Any<IMedia>(), Arg.Any<CancellationToken>())
                .Returns(PlaceholderGenerationResult.Generated("data:image/webp;base64,AA=="));

            Job = new BlurPlaceholderMaintenanceJob(
                keyValueService,
                mediaService,
                mediaTypeService,
                new MediaUrlGeneratorCollection(() => [urlGenerator]),
                Processor,
                Options.Create(options),
                Substitute.For<ILogger<BlurPlaceholderMaintenanceJob>>());
        }

        public Dictionary<string, string> Store { get; } = [];

        public IMedia Image { get; }

        public IMediaPlaceholderProcessor Processor { get; }

        public BlurPlaceholderMaintenanceJob Job { get; }

        private static IMedia CreateImage(string? placeholder)
        {
            var mediaType = Substitute.For<ISimpleContentType>();
            mediaType.Alias.Returns(Constants.DefaultImageMediaTypeAlias);

            var sourceProperty = Substitute.For<IProperty>();
            sourceProperty.GetValue().Returns(MediaPath);
            var properties = Substitute.For<IPropertyCollection>();
            properties.TryGetValue(Constants.SourcePropertyAlias, out Arg.Any<IProperty?>())
                .Returns(call =>
                {
                    call[1] = sourceProperty;
                    return true;
                });

            var media = Substitute.For<IMedia>();
            media.ContentType.Returns(mediaType);
            media.Properties.Returns(properties);
            media.HasProperty(Arg.Any<string>()).Returns(true);
            media.GetValue<string>(Constants.PropertyAlias).Returns(placeholder);
            return media;
        }
    }
}
