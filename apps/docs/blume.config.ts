import { docsTheme } from "@thebuilder/umbraco-docs";
import { defineConfig } from "blume";
import { vercel } from "blume/analytics";
import { custom, filesystem } from "blume/sources";

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
      filesystem({ root: "content" }),
      custom(
        githubReleaseChangelogSource({
          owner: "thebuilder",
          repo: "blur-placeholder",
        }),
      ),
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
    site: "https://blur.thebuilder.dk",
  },
  seo: {
    // Package-owned cards are generated into public/og before Blume builds.
    // Disable Blume's generated /og routes so it cannot overwrite them.
    og: { enabled: false },
  },
  analytics: [vercel()],
  // The docs family's shared accent and fonts, so every package site looks alike.
  theme: { ...docsTheme },
  agents: {
    llmsTxt: true,
  },
});
