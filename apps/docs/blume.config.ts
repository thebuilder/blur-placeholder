import { defineConfig } from "blume";

import { githubReleaseChangelogSource } from "./sources/github-releases";
import { blurPlaceholderPackage } from "./umbraco-package";

export default defineConfig({
  title: blurPlaceholderPackage.name,
  description: blurPlaceholderPackage.summary,
  logo: {
    image: "/logo-mark.svg",
    text: "Blur Placeholder",
  },
  github: {
    owner: "thebuilder",
    repo: "blur-placeholder",
    dir: "apps/docs",
  },
  content: {
    sources: [
      { type: "filesystem", root: "content" },
      {
        type: "custom",
        source: githubReleaseChangelogSource({
          owner: "thebuilder",
          repo: "blur-placeholder",
        }),
      },
    ],
  },
  navigation: {
    tabs: [
      // A custom landing page owns "/", so the Docs tab links to the overview
      // while keeping path "/" to stay highlighted across every docs route.
      { label: "Docs", path: "/", href: "/overview" },
      { label: "Changelog", path: "/changelog", href: "/changelog" },
    ],
  },
  deployment: {
    output: "static",
    site: "https://blur.thebuilder.dk",
  },
  seo: {
    // Package-owned cards are generated into public/og before Blume builds.
    // Disable Blume's generated /og routes so it cannot overwrite them.
    og: { enabled: false },
  },
  analytics: {
    vercel: true,
  },
  ai: {
    llmsTxt: true,
  },
  theme: {
    accent: "#2563eb",
  },
});
