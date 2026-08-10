using Microsoft.Extensions.Options;

namespace TheBuilder.BlurPlaceholder.Configuration;

internal sealed class BlurPlaceholderOptionsValidator : IValidateOptions<BlurPlaceholderOptions>
{
    public ValidateOptionsResult Validate(string? name, BlurPlaceholderOptions options)
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(options.Algorithm))
            errors.Add("Algorithm must be Webp, BlurHash, or ThumbHash.");

        if (options.Webp.MaximumDimension is < 16 or > 64)
            errors.Add("Webp.MaximumDimension must be between 16 and 64.");

        if (options.Webp.Quality is < 1 or > 100)
            errors.Add("Webp.Quality must be between 1 and 100.");

        if (options.BlurHash.ComponentsX is < 1 or > 9)
            errors.Add("BlurHash.ComponentsX must be between 1 and 9.");

        if (options.BlurHash.MaximumDimension is < 16 or > 100)
            errors.Add("BlurHash.MaximumDimension must be between 16 and 100.");

        if (options.BlurHash.ComponentsY is < 1 or > 9)
            errors.Add("BlurHash.ComponentsY must be between 1 and 9.");

        if (options.ThumbHash.MaximumDimension is < 1 or > 100)
            errors.Add("ThumbHash.MaximumDimension must be between 1 and 100.");

        if (options.DecodedDataUrl.WebpQuality is < 1 or > 100)
            errors.Add("DecodedDataUrl.WebpQuality must be between 1 and 100.");

        if (options.RetryInterval < TimeSpan.FromMinutes(1))
            errors.Add("RetryInterval must be at least one minute.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
