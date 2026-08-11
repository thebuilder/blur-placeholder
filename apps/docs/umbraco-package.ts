import { defineUmbracoPackage } from "@thebuilder/umbraco-docs";

export const blurPlaceholderPackage = defineUmbracoPackage({
  id: "thebuilder.blurplaceholder",
  name: "Blur Placeholder",
  summary: "Generate WebP, BlurHash, or ThumbHash placeholders for Umbraco Image media and deliver one frontend-ready string.",
  links: {
    docs: "https://blur-placeholder.vercel.app/",
    nuget: "https://www.nuget.org/packages/TheBuilder.BlurPlaceholder",
    marketplace: "https://marketplace.umbraco.com/package/thebuilder.blurplaceholder",
    github: "https://github.com/thebuilder/blur-placeholder",
  },
  logo: "/logo-mark.svg",
  compatibility: { umbraco: ">=17.1 <19", dotnet: ">=10" },
  status: "stable",
  categories: ["Developer Tools", "Media"],
});
