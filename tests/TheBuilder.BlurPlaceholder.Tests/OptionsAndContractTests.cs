using Microsoft.Extensions.Options;
using TheBuilder.BlurPlaceholder.Configuration;
using TheBuilder.BlurPlaceholder.Models;

namespace TheBuilder.BlurPlaceholder.Tests;

public sealed class OptionsAndContractTests
{
    [Fact]
    public void Defaults_are_valid_and_have_a_stable_fingerprint()
    {
        var options = new BlurPlaceholderOptions();
        var validation = new BlurPlaceholderOptionsValidator().Validate(Options.DefaultName, options);

        Assert.True(validation.Succeeded);
        Assert.Equal(64, BlurPlaceholderFingerprint.Create(options).Length);
        Assert.Equal(BlurPlaceholderFingerprint.Create(options), BlurPlaceholderFingerprint.Create(options));
    }

    [Fact]
    public void Output_affecting_options_change_the_fingerprint()
    {
        var first = new BlurPlaceholderOptions();
        var second = new BlurPlaceholderOptions();
        second.DecodedDataUrl.WebpQuality = 61;

        Assert.NotEqual(BlurPlaceholderFingerprint.Create(first), BlurPlaceholderFingerprint.Create(second));
    }

    [Fact]
    public void Operational_options_do_not_change_the_output_fingerprint()
    {
        var first = new BlurPlaceholderOptions();
        var second = new BlurPlaceholderOptions
        {
            Enabled = false,
            BackfillExisting = false,
            RetryInterval = TimeSpan.FromMinutes(5),
        };

        Assert.Equal(BlurPlaceholderFingerprint.Create(first), BlurPlaceholderFingerprint.Create(second));
    }

    [Fact]
    public void Invalid_options_are_rejected()
    {
        var options = new BlurPlaceholderOptions
        {
            RetryInterval = TimeSpan.FromSeconds(30),
        };
        options.Webp.MaximumDimension = 8;
        options.BlurHash.ComponentsX = 10;

        var validation = new BlurPlaceholderOptionsValidator().Validate(Options.DefaultName, options);

        Assert.False(validation.Succeeded);
        var failures = string.Join(" ", validation.Failures ?? []);
        Assert.Contains("RetryInterval", failures);
        Assert.Contains("Webp.MaximumDimension", failures);
    }

    [Fact]
    public void Generation_results_make_placeholder_mutation_explicit()
    {
        Assert.Equal(PlaceholderMutation.Set("value"), PlaceholderGenerationResult.Generated("value").Mutation);
        Assert.Equal(PlaceholderMutation.Clear, PlaceholderGenerationResult.Unsupported("unsupported").Mutation);
        Assert.Equal(PlaceholderMutation.Clear, PlaceholderGenerationResult.RetryableFailure("retry").Mutation);
        Assert.Equal(PlaceholderMutation.Keep, PlaceholderGenerationResult.Disabled.Mutation);
    }
}
