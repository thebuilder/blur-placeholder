import { defineConfig } from "blume";

export default defineConfig({
  title: "Blur Placeholder",
  description: "Compact image placeholders for Umbraco.",
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
    root: "content"
  },
  navigation: {
    tabs: [{ label: "Docs", path: "/", href: "/overview" }]
  },
  deployment: {
    output: "static",
    site: "https://blur-placeholder.vercel.app"
  },
  seo: {
    og: { enabled: false }
  },
  theme: {
    accent: "#2563eb"
  }
});
