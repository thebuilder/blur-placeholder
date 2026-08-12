import { defineConfig } from "blume";
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
    root: "content"
  },
  navigation: {
    tabs: [{ label: "Docs", path: "/", href: "/overview" }]
  },
  deployment: {
    output: "static",
    site: "https://blur-placeholder.thebuilder.dk"
  },
  seo: {
    og: { enabled: false }
  },
  theme: {
    accent: "#2563eb"
  }
});
