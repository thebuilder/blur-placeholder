using Umbraco.Cms.Core.Packaging;

namespace TheBuilder.BlurPlaceholder.Migrations;

internal sealed class BlurPlaceholderPackageMigrationPlan : PackageMigrationPlan
{
    public BlurPlaceholderPackageMigrationPlan() : base(Constants.PackageName)
    {
    }

    protected override void DefinePlan()
    {
        To<InstallBlurPlaceholderSchema>(new Guid("cb1ef2f3-3ad4-4f4a-8a3a-8b0b2a68a1b9"));
        To<MoveBlurPlaceholderIntoImageGroup>(new Guid("709c54eb-6c7a-4b92-afa2-cbe531f370a7"));
    }
}
