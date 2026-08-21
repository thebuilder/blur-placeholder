using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;
using TheBuilder.BlurPlaceholder.HealthChecks;

namespace TheBuilder.BlurPlaceholder.Migrations;

internal sealed class InstallBlurPlaceholderSchema : AsyncPackageMigrationBase
{
    private static readonly Guid MigrationUserKey = Umbraco.Cms.Core.Constants.Security.SuperUserKey;
    private readonly IDataTypeService _dataTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _configurationSerializer;
    private readonly IShortStringHelper _shortStringHelper;

    public InstallBlurPlaceholderSchema(
        IPackagingService packagingService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IMigrationContext context,
        IOptions<PackageMigrationSettings> packageMigrationsSettings,
        IDataTypeService dataTypeService,
        IMediaTypeService mediaTypeService,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer configurationSerializer)
        : base(
            packagingService,
            mediaService,
            mediaFileManager,
            mediaUrlGenerators,
            shortStringHelper,
            contentTypeBaseServiceProvider,
            context,
            packageMigrationsSettings)
    {
        _dataTypeService = dataTypeService;
        _mediaTypeService = mediaTypeService;
        _propertyEditors = propertyEditors;
        _configurationSerializer = configurationSerializer;
        _shortStringHelper = shortStringHelper;
    }

    protected override async Task MigrateAsync()
    {
        var existingDataType = await _dataTypeService.GetAsync(BlurPlaceholderSchema.DataTypeKey);
        var imageMediaType = _mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias)
            ?? throw new InvalidOperationException("The default Umbraco Image media type was not found.");
        var existingProperty = imageMediaType.PropertyTypes.FirstOrDefault(item => item.Alias == Constants.PropertyAlias);
        var existingPropertyDataType = existingProperty is null
            ? null
            : await _dataTypeService.GetAsync(existingProperty.DataTypeKey);

        BlurPlaceholderSchemaGuard.EnsureCompatible(existingDataType, existingProperty, existingPropertyDataType);

        var dataType = existingPropertyDataType ?? existingDataType ?? CreateDataType();
        if (existingDataType is null && existingPropertyDataType is null)
            await _dataTypeService.CreateAsync(dataType, MigrationUserKey);

        if (existingProperty is null)
        {
            var property = new PropertyType(_shortStringHelper, dataType, Constants.PropertyAlias)
            {
                Name = "Blur placeholder",
                Description = "Generated placeholder preview value.",
                SortOrder = imageMediaType.PropertyTypes.Count(),
                Mandatory = false,
            };
            imageMediaType.AddPropertyType(property);
            await _mediaTypeService.UpdateAsync(imageMediaType, MigrationUserKey);
        }
    }

    private IDataType CreateDataType()
    {
        if (!_propertyEditors.TryGet(Constants.PropertyEditorAlias, out IDataEditor? editor) || editor is null)
            throw new InvalidOperationException($"The Umbraco editor {Constants.PropertyEditorAlias} is not available.");

        return new DataType(editor, _configurationSerializer, -1)
        {
            Key = BlurPlaceholderSchema.DataTypeKey,
            Name = Constants.DataTypeName,
            EditorUiAlias = "TheBuilder.PropertyEditorUi.BlurPlaceholder",
            ConfigurationData = new Dictionary<string, object>(),
        };
    }
}
