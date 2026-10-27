using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Media;
using TheBuilder.BlurPlaceholder.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.BlurPlaceholder.Tests;

public sealed class MaintenanceJobTests
{
    [Fact]
    public void Job_polls_every_five_minutes_regardless_of_the_retry_interval()
    {
        var job = new JobFixture(new BlurPlaceholderOptions { RetryInterval = TimeSpan.FromHours(12) }).Job;

        Assert.Equal(TimeSpan.FromMinutes(5), job.Period);
    }

    [Fact]
    public void Run_budget_leaves_a_margin_before_another_server_can_take_the_job_over()
    {
        // Umbraco lets another server take a job over after Period + MaximumExecutionTime (5 minutes by default).
        var takeover = BlurPlaceholderMaintenanceSchedule.PollInterval + TimeSpan.FromMinutes(5);

        Assert.True(takeover - BlurPlaceholderMaintenanceSchedule.RunBudget >= TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task First_run_after_install_backfills_existing_images()
    {
        var fixture = new JobFixture(new BlurPlaceholderOptions());

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[0], Arg.Any<CancellationToken>());
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
        fixture.Store[Constants.LastMaintenanceScanKey] = fixture.Clock.GetUtcNow().AddMinutes(-5).ToString("O", CultureInfo.InvariantCulture);

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.DidNotReceive().GenerateAsync(Arg.Any<IMedia>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Changed_fingerprint_regenerates_without_waiting_for_the_retry_interval()
    {
        var fixture = new JobFixture(new BlurPlaceholderOptions(), placeholder: "data:image/webp;base64,AA==");
        fixture.Store[Constants.BackfillFingerprintKey] = "fingerprint-from-an-older-release";
        fixture.Store[Constants.LastMaintenanceScanKey] = fixture.Clock.GetUtcNow().AddMinutes(-5).ToString("O", CultureInfo.InvariantCulture);

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[0], Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Without_backfill_the_first_scan_waits_for_the_retry_interval()
    {
        var fixture = new JobFixture(new BlurPlaceholderOptions { BackfillExisting = false });

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.DidNotReceive().GenerateAsync(Arg.Any<IMedia>(), Arg.Any<CancellationToken>());
        Assert.True(fixture.Store.ContainsKey(Constants.LastMaintenanceScanKey));
    }

    [Fact]
    public async Task Pass_that_exceeds_the_run_budget_pauses_and_resumes_from_the_next_page()
    {
        var options = new BlurPlaceholderOptions();
        var fixture = new JobFixture(options, pageCount: 4)
        {
            // Two pages exceed the five-minute budget, so each run covers two pages.
            TimePerImage = TimeSpan.FromMinutes(3),
        };

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[0], Arg.Any<CancellationToken>());
        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[1], Arg.Any<CancellationToken>());
        await fixture.Processor.DidNotReceive().GenerateAsync(fixture.Images[2], Arg.Any<CancellationToken>());
        var cursor = JsonSerializer.Deserialize<BlurPlaceholderMaintenanceCursor>(fixture.Store[Constants.MaintenanceCursorKey]);
        Assert.Equal(2, cursor?.NextPageIndex);
        Assert.False(fixture.Store.ContainsKey(Constants.BackfillFingerprintKey));
        Assert.False(fixture.Store.ContainsKey(Constants.LastMaintenanceScanKey));

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[0], Arg.Any<CancellationToken>());
        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[1], Arg.Any<CancellationToken>());
        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[2], Arg.Any<CancellationToken>());
        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[3], Arg.Any<CancellationToken>());
        Assert.Equal(string.Empty, fixture.Store[Constants.MaintenanceCursorKey]);
        Assert.Equal(BlurPlaceholderFingerprint.Create(options), fixture.Store[Constants.BackfillFingerprintKey]);
        Assert.True(fixture.Store.ContainsKey(Constants.LastMaintenanceScanKey));
    }

    [Fact]
    public async Task Failed_attempt_is_not_repeated_by_the_save_handler_during_the_maintenance_save()
    {
        var fixture = new JobFixture(new BlurPlaceholderOptions(), placeholder: "data:image/webp;base64,AA==")
        {
            Result = PlaceholderGenerationResult.RetryableFailure("temporary"),
        };
        fixture.Store[Constants.BackfillFingerprintKey] = "fingerprint-from-an-older-release";
        var handler = new BlurPlaceholderMediaSavingHandler(
            fixture.Processor,
            Options.Create(new BlurPlaceholderOptions()),
            Substitute.For<ILogger<BlurPlaceholderMediaSavingHandler>>());
        fixture.OnSave = media => handler
            .HandleAsync(new MediaSavingNotification([media], new EventMessages()), CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        await fixture.Job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, fixture.SaveCount);
        await fixture.Processor.Received(1).GenerateAsync(fixture.Images[0], Arg.Any<CancellationToken>());
        Assert.False(BlurPlaceholderMaintenanceSave.IsActive);
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

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private TimeSpan _elapsed;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow() => _start + _elapsed;

        public override long GetTimestamp() => _elapsed.Ticks;

        public void Advance(TimeSpan duration) => _elapsed += duration;
    }

    private sealed class JobFixture
    {
        private const string MediaPath = "/media/image.jpg";
        private readonly Dictionary<IMedia, string?> _placeholders = [];

        public JobFixture(BlurPlaceholderOptions options, string? placeholder = null, int pageCount = 1)
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

            // One image per page keeps the page arithmetic visible in the tests.
            Images = Enumerable.Range(0, pageCount).Select(_ => CreateImage(placeholder)).ToArray();
            var mediaService = Substitute.For<IMediaService>();
            mediaService.GetPagedOfType(Arg.Is(1), Arg.Any<long>(), Arg.Any<int>(), out Arg.Any<long>(), Arg.Any<IQuery<IMedia>?>(), Arg.Any<Ordering?>())
                .Returns(call =>
                {
                    var pageIndex = call.ArgAt<long>(1);
                    call[3] = (long)pageCount * call.ArgAt<int>(2);
                    return pageIndex < pageCount ? new[] { Images[pageIndex] } : [];
                });
            mediaService.When(service => service.Save(Arg.Any<IMedia>(), Arg.Any<int>()))
                .Do(call =>
                {
                    SaveCount++;
                    OnSave?.Invoke(call.Arg<IMedia>());
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
                .Returns(call =>
                {
                    var media = call.Arg<IMedia>();
                    Clock.Advance(TimePerImage);
                    _placeholders[media] = Result.Mutation.Kind switch
                    {
                        PlaceholderMutationKind.Set => Result.Mutation.Value,
                        PlaceholderMutationKind.Clear => string.Empty,
                        _ => _placeholders[media],
                    };
                    return Result;
                });

            Job = new BlurPlaceholderMaintenanceJob(
                keyValueService,
                mediaService,
                mediaTypeService,
                new MediaUrlGeneratorCollection(() => [urlGenerator]),
                Processor,
                Options.Create(options),
                Substitute.For<ILogger<BlurPlaceholderMaintenanceJob>>(),
                Clock);
        }

        public Dictionary<string, string> Store { get; } = [];

        public ManualTimeProvider Clock { get; } = new();

        public IMedia[] Images { get; }

        public IMediaPlaceholderProcessor Processor { get; }

        public BlurPlaceholderMaintenanceJob Job { get; }

        public TimeSpan TimePerImage { get; init; }

        public PlaceholderGenerationResult Result { get; init; } =
            PlaceholderGenerationResult.Generated("data:image/webp;base64,AQ==");

        public Action<IMedia>? OnSave { get; set; }

        public int SaveCount { get; private set; }

        private IMedia CreateImage(string? placeholder)
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
            media.GetValue<string>(Constants.PropertyAlias).Returns(_ => _placeholders[media]);
            _placeholders[media] = placeholder;
            return media;
        }
    }
}
