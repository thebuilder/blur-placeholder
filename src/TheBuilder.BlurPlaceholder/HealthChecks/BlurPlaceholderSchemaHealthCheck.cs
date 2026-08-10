using Microsoft.Extensions.Diagnostics.HealthChecks;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.BlurPlaceholder.HealthChecks;

internal sealed class BlurPlaceholderSchemaHealthCheck(
    IDataTypeService dataTypeService,
    IMediaTypeService mediaTypeService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var dataType = await dataTypeService.GetAsync(BlurPlaceholderSchema.DataTypeKey);
        var image = mediaTypeService.Get(Constants.DefaultImageMediaTypeAlias);
        var property = image?.PropertyTypes.FirstOrDefault(item => item.Alias == Constants.PropertyAlias);
        var propertyDataType = property is null ? null : await dataTypeService.GetAsync(property.DataTypeKey);

        if (image is null || property is null || propertyDataType is null)
        {
            return HealthCheckResult.Unhealthy(
                $"The {Constants.PropertyAlias} schema is not installed. Run the {Constants.PackageName} package migration.");
        }

        try
        {
            BlurPlaceholderSchemaGuard.EnsureCompatible(dataType, property, propertyDataType);
            return HealthCheckResult.Healthy("Blur placeholder schema is compatible.");
        }
        catch (BlurPlaceholderSchemaCollisionException exception)
        {
            return HealthCheckResult.Unhealthy(exception.Message, exception);
        }
    }
}
