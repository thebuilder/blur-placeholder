import { mkdir, readFile, readdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import matter from "gray-matter";
import sharp from "sharp";

const here = dirname(fileURLToPath(import.meta.url));
const docsRoot = resolve(here, "..");
const logo = await readFile(resolve(docsRoot, "public/logo-mark.svg"));

const width = 1200;
const height = 630;
const font = "Inter, 'Helvetica Neue', Arial, sans-serif";
const escapeXml = (value) =>
  value.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;");

const findContentFiles = async (directory) => {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(
    entries.map((entry) => {
      const path = resolve(directory, entry.name);
      return entry.isDirectory()
        ? findContentFiles(path)
        : entry.isFile() && /\.mdx?$/.test(entry.name)
          ? [path]
          : [];
    }),
  );
  return nested.flat();
};

const wrapText = (value, maxCharacters, maxLines) => {
  const words = value.trim().split(/\s+/);
  const lines = [];
  for (const word of words) {
    const line = lines.at(-1);
    if (!line || `${line} ${word}`.length > maxCharacters) {
      lines.push(word);
    } else {
      lines[lines.length - 1] = `${line} ${word}`;
    }
  }
  if (lines.length <= maxLines) return lines;
  const visible = lines.slice(0, maxLines);
  visible[maxLines - 1] = `${visible[maxLines - 1].replace(/[.,;:]?$/, "")}…`;
  return visible;
};

const ogRoot = resolve(docsRoot, "public/og");
const pages = [
  {
    output: resolve(docsRoot, "public/opengraph-image.png"),
    title: "Blur Placeholder",
    description: "Compact image placeholders for Umbraco.",
  },
];
for (const source of await findContentFiles(resolve(docsRoot, "content"))) {
  const { data } = matter(await readFile(source, "utf8"));
  const image = data.seo?.image;
  if (!image) continue;
  if (typeof data.title !== "string" || typeof data.description !== "string") {
    throw new Error(`${source} needs string title and description frontmatter`);
  }
  if (typeof image !== "string" || !image.startsWith("/og/") || !image.endsWith(".png")) {
    throw new Error(`${source} has unsupported seo.image: ${String(image)}`);
  }
  const output = resolve(docsRoot, "public", image.slice(1));
  if (!output.startsWith(`${ogRoot}/`)) {
    throw new Error(`${source} resolves outside public/og`);
  }
  pages.push({ title: data.title, description: data.description, output });
}
pages.sort((a, b) => a.output.localeCompare(b.output));

const card = ({ title, description }) => {
  const titleSize = title.length > 23 ? 72 : 82;
  const descriptionLines = wrapText(description, 58, 3);
  const titleY = 372 - (descriptionLines.length - 1) * 16;
  const descriptionY = titleY + 60;
  const descriptionSvg = descriptionLines
    .map(
      (line, index) =>
        `<tspan x="82" y="${descriptionY + index * 40}">${escapeXml(line)}</tspan>`,
    )
    .join("");
  return `<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="background" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#111a35"/>
      <stop offset="0.56" stop-color="#090e1d"/>
      <stop offset="1" stop-color="#05060c"/>
    </linearGradient>
    <radialGradient id="glow" cx="0.2" cy="-0.08" r="0.9">
      <stop offset="0" stop-color="#527fe7" stop-opacity="0.45"/>
      <stop offset="0.46" stop-color="#6366f1" stop-opacity="0.12"/>
      <stop offset="1" stop-color="#6366f1" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="accent" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#60a5fa"/>
      <stop offset="0.5" stop-color="#9b83ec"/>
      <stop offset="1" stop-color="#ed63ad"/>
    </linearGradient>
  </defs>
  <rect width="${width}" height="${height}" fill="url(#background)"/>
  <rect width="${width}" height="${height}" fill="url(#glow)"/>
  <rect width="${width}" height="6" fill="url(#accent)"/>
  <text x="80" y="${titleY}" font-family="${font}" font-size="${titleSize}" font-weight="800" letter-spacing="-2" fill="#f8fafc">${escapeXml(title)}</text>
  <text font-family="${font}" font-size="32" font-weight="400" fill="#aab2c5">${descriptionSvg}</text>
  <text x="82" y="566" font-family="${font}" font-size="26" font-weight="600" fill="#6f7a91">blur-placeholder.vercel.app</text>
</svg>`;
};

const logoSize = 136;
const renderedLogo = await sharp(logo)
  .resize(logoSize, logoSize, { fit: "contain" })
  .png()
  .toBuffer();

await mkdir(ogRoot, { recursive: true });

for (const page of pages) {
  await mkdir(dirname(page.output), { recursive: true });
  await sharp(Buffer.from(card(page)))
    .composite([{ input: renderedLogo, left: 78, top: 92 }])
    .png()
    .toFile(page.output);
}

console.log(`generated ${pages.length} Open Graph images`);
