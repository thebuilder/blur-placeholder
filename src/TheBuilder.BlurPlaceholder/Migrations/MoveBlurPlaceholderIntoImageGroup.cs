using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace TheBuilder.BlurPlaceholder.Migrations;

internal sealed class MoveBlurPlaceholderIntoImageGroup : AsyncMigrationBase
{
    private static readonly Guid MigrationUserKey = Umbraco.Cms.Core.Constants.Security.SuperUserKey;
    private readonly IMediaTypeService _mediaTypeService;

    public MoveBlurPlaceholderIntoImageGroup(
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
        var imageGroup = imageMediaType.PropertyGroups.FirstOrDefault(group =>
            group.PropertyTypes?.Any(property => property.Alias == Constants.SourcePropertyAlias) is true)
            ?? throw new InvalidOperationException("The default Image property group was not found.");

        imageMediaType.MovePropertyType(Constants.PropertyAlias, imageGroup.Alias);
        placeholderProperty.SortOrder = imageGroup.PropertyTypes is null
            ? 0
            : imageGroup.PropertyTypes
                .Where(property => property.Alias != Constants.PropertyAlias)
                .Select(property => property.SortOrder)
                .DefaultIfEmpty(-1)
                .Max() + 1;

        await _mediaTypeService.UpdateAsync(imageMediaType, MigrationUserKey);
    }
}
