using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace TheBuilder.BlurPlaceholder.Migrations;

internal sealed class UpdateBlurPlaceholderDescription : AsyncMigrationBase
{
    private static readonly Guid MigrationUserKey = Umbraco.Cms.Core.Constants.Security.SuperUserKey;
    private readonly IMediaTypeService _mediaTypeService;

    public UpdateBlurPlaceholderDescription(
        IMigrationContext context,
        IMediaTypeService mediaTypeService)
        : base(context)
    {
        _mediaTypeService = mediaTypeService;
    }

    protected override async Task MigrateAsync()
    {
        var imageMediaType = _mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias)
            ?? throw new InvalidOperationException("The default Umbraco Image media type was not found.");
        var placeholderProperty = imageMediaType.PropertyTypes.FirstOrDefault(property => property.Alias == Constants.PropertyAlias)
            ?? throw new InvalidOperationException("The Blur placeholder property was not found.");

        placeholderProperty.Description = "Generated placeholder preview value.";
        await _mediaTypeService.UpdateAsync(imageMediaType, MigrationUserKey);
    }
}
