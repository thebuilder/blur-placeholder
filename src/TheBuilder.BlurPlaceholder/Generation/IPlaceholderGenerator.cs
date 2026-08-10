using TheBuilder.BlurPlaceholder.Models;

namespace TheBuilder.BlurPlaceholder.Generation;

internal interface IPlaceholderGenerator
{
    Task<PlaceholderGenerationResult> GenerateAsync(Stream source, CancellationToken cancellationToken = default);
}
