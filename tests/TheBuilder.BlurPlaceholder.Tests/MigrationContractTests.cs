using System.Reflection;
using NSubstitute;
using TheBuilder.BlurPlaceholder.HealthChecks;
using TheBuilder.BlurPlaceholder.Migrations;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace TheBuilder.BlurPlaceholder.Tests;

public sealed class MigrationContractTests
{
    [Fact]
    public async Task Existing_property_is_placed_last_and_repeated_migration_is_stable()
    {
        var sourceProperty = Substitute.For<IPropertyType>();
        sourceProperty.Alias.Returns(Constants.SourcePropertyAlias);
        sourceProperty.SortOrder.Returns(3);
        var siblingProperty = Substitute.For<IPropertyType>();
        siblingProperty.Alias.Returns("umbracoWidth");
        siblingProperty.SortOrder.Returns(7);
        var placeholderProperty = Substitute.For<IPropertyType>();
        placeholderProperty.Alias.Returns(Constants.PropertyAlias);

        var imageGroup = new PropertyGroup(new PropertyTypeCollection(
            true,
            [sourceProperty, siblingProperty, placeholderProperty]))
        {
            Alias = "image",
        };
        var mediaType = Substitute.For<IMediaType>();
        mediaType.PropertyTypes.Returns([sourceProperty, siblingProperty, placeholderProperty]);
        mediaType.PropertyGroups.Returns([imageGroup]);

        var mediaTypeService = Substitute.For<IMediaTypeService>();
        mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias).Returns(mediaType);
        var migration = new MoveBlurPlaceholderIntoImageGroup(
            Substitute.For<IMigrationContext>(),
            mediaTypeService);

        await InvokeMigrationAsync(migration);
        await InvokeMigrationAsync(migration);

        Assert.Equal(8, placeholderProperty.SortOrder);
        await mediaTypeService.Received(2).UpdateAsync(mediaType, Arg.Any<Guid>());
    }

    [Fact]
    public async Task Missing_image_group_does_not_fail_package_boot()
    {
        var property = Substitute.For<IPropertyType>();
        property.Alias.Returns(Constants.PropertyAlias);

        var mediaType = Substitute.For<IMediaType>();
        mediaType.PropertyTypes.Returns([property]);
        mediaType.PropertyGroups.Returns([]);

        var mediaTypeService = Substitute.For<IMediaTypeService>();
        mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias).Returns(mediaType);
        var migration = new MoveBlurPlaceholderIntoImageGroup(
            Substitute.For<IMigrationContext>(),
            mediaTypeService);

        await InvokeMigrationAsync(migration);

        await mediaTypeService.DidNotReceive().UpdateAsync(Arg.Any<IMediaType>(), Arg.Any<Guid>());
    }

    [Fact]
    public void Compatible_owned_schema_is_reused()
    {
        var key = Guid.NewGuid();
        var dataType = CreateCompatibleDataType(key);
        var property = Substitute.For<IPropertyType>();
        property.DataTypeKey.Returns(key);

        BlurPlaceholderSchemaGuard.EnsureCompatible(dataType, property, dataType);
    }

    [Fact]
    public void Incompatible_existing_property_is_rejected()
    {
        var property = Substitute.For<IPropertyType>();
        property.DataTypeKey.Returns(Guid.NewGuid());

        Assert.Throws<BlurPlaceholderSchemaCollisionException>(() =>
            BlurPlaceholderSchemaGuard.EnsureCompatible(null, property, CreateCompatibleDataType()));
    }

    private static IDataType CreateCompatibleDataType(Guid? key = null)
    {
        var dataType = Substitute.For<IDataType>();
        dataType.Key.Returns(key ?? Guid.NewGuid());
        dataType.EditorAlias.Returns(Constants.PropertyEditorAlias);
        dataType.EditorUiAlias.Returns("TheBuilder.PropertyEditorUi.BlurPlaceholder");
        return dataType;
    }

    private static async Task InvokeMigrationAsync(AsyncMigrationBase migration)
    {
        var method = migration.GetType().GetMethod("MigrateAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Migration entry point was not found.");
        var task = (Task?)method.Invoke(migration, null)
            ?? throw new InvalidOperationException("Migration did not return a task.");
        await task;
    }
}
