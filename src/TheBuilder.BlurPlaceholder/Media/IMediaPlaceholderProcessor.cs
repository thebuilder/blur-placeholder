using TheBuilder.BlurPlaceholder.Models;
using Umbraco.Cms.Core.Models;

namespace TheBuilder.BlurPlaceholder.Media;

internal interface IMediaPlaceholderProcessor
{
    Task<PlaceholderGenerationResult> GenerateAsync(
        IMedia media,
        CancellationToken cancellationToken = default);
}
