import { defineOgConfig } from "@thebuilder/umbraco-docs/og";
import { fileURLToPath } from "node:url";

export default defineOgConfig({
  contentDir: fileURLToPath(new URL("./content", import.meta.url)),
  publicDir: fileURLToPath(new URL("./public", import.meta.url)),
  prefix: "/og",
  brand: "TheBuilder · Blur Placeholder",
  accent: "#2563eb",
  logo: "/logo-mark.svg",
  root: {
    title: "Blur Placeholder",
    description: "Compact image placeholders for Umbraco.",
  },
});
