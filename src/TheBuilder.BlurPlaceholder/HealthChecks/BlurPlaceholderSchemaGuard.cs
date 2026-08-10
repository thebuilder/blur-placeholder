using Umbraco.Cms.Core.Models;

namespace TheBuilder.BlurPlaceholder.HealthChecks;

internal static class BlurPlaceholderSchema
{
    public static readonly Guid DataTypeKey = new("b8b3c2d6-a9b7-4ae7-b6d1-5d2b6bde0d8c");
}

internal sealed class BlurPlaceholderSchemaCollisionException(string message) : InvalidOperationException(message);

internal static class BlurPlaceholderSchemaGuard
{
    public static void EnsureCompatible(
        IDataType? ownedDataType,
        IPropertyType? property,
        IDataType? propertyDataType = null)
    {
        if (ownedDataType is not null && !IsCompatibleDataType(ownedDataType))
        {
            throw new BlurPlaceholderSchemaCollisionException(
                $"The data type for {Constants.PropertyAlias} exists but is not owned by {Constants.PackageName}.");
        }

        if (property is not null
            && (propertyDataType is null
                || property.DataTypeKey != propertyDataType.Key
                || !IsCompatibleDataType(propertyDataType)))
        {
            throw new BlurPlaceholderSchemaCollisionException(
                $"The media property {Constants.PropertyAlias} exists with an incompatible data type or editor.");
        }
    }

    private static bool IsCompatibleDataType(IDataType dataType) =>
        string.Equals(dataType.EditorAlias, Constants.PropertyEditorAlias, StringComparison.Ordinal)
        && string.Equals(
            dataType.EditorUiAlias,
            "TheBuilder.PropertyEditorUi.BlurPlaceholder",
            StringComparison.Ordinal);
}
