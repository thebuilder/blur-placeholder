using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Generation;
using TheBuilder.BlurPlaceholder.HealthChecks;
using TheBuilder.BlurPlaceholder.Media;
using TheBuilder.BlurPlaceholder.Migrations;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.BackgroundJobs;
using Umbraco.Extensions;

namespace TheBuilder.BlurPlaceholder.Composers;

/// <summary>Registers blur-placeholder services with Umbraco.</summary>
public sealed class BlurPlaceholderComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IValidateOptions<BlurPlaceholderOptions>, BlurPlaceholderOptionsValidator>();
        builder.Services.AddOptions<BlurPlaceholderOptions>()
            .Bind(builder.Config.GetSection(Constants.ConfigurationSection))
            .ValidateOnStart();

        builder.Services.AddSingleton<IPlaceholderGenerator, PlaceholderGenerator>();
        builder.Services.AddSingleton<IMediaPlaceholderProcessor, MediaPlaceholderProcessor>();
        builder.Services.AddSingleton<IDistributedBackgroundJob, BlurPlaceholderMaintenanceJob>();
        builder.AddNotificationAsyncHandler<MediaSavingNotification, BlurPlaceholderMediaSavingHandler>();

        builder.PackageMigrationPlans().Add(typeof(BlurPlaceholderPackageMigrationPlan));
        builder.Services.AddHealthChecks().AddCheck<BlurPlaceholderSchemaHealthCheck>(Constants.PackageName);
    }
}
