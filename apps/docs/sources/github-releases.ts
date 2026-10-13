import { resolve } from "node:path";

import type { ContentSource, SourceEntry } from "blume/core/sources/types.ts";
import { githubReleasesSource } from "blume/sources/github-releases.ts";

type ReleaseSourceOptions = {
  owner: string;
  repo: string;
};

const excludeSearchFromRaw = (raw: string): string =>
  raw.replace(/^---\n/u, "---\nsearch:\n  exclude: true\n");

const excludeFromSearch = (entry: SourceEntry): SourceEntry => ({
  ...entry,
  data: {
    ...entry.data,
    search: { exclude: true },
  },
  raw: entry.raw ? excludeSearchFromRaw(entry.raw) : undefined,
});

// Blume 2 rebuilds an engine-backed custom() source on each scan's context
// through withContext, so the wrapper rewraps the rebuilt source to keep
// release pages out of search.
const excludeSourceFromSearch = (source: ContentSource): ContentSource => {
  const { read, withContext } = source;

  return {
    ...source,
    load: async () => {
      const result = await source.load();

      return {
        ...result,
        entries: result.entries.map(excludeFromSearch),
      };
    },
    read: read
      ? async (ref: string) => excludeSearchFromRaw(await read(ref))
      : undefined,
    withContext: withContext
      ? (ctx) => excludeSourceFromSearch(withContext(ctx))
      : undefined,
  };
};

export const githubReleaseChangelogSource = ({
  owner,
  repo,
}: ReleaseSourceOptions): ContentSource => {
  const isDev = process.argv.includes("dev");

  return excludeSourceFromSearch(
    githubReleasesSource(
      {
        name: "changelog",
        owner,
        prefix: "changelog",
        repo,
      },
      {
        cacheDir: resolve(".blume/cache/changelog"),
        mode: isDev ? "dev" : "build",
        projectRoot: process.cwd(),
        refresh: !isDev,
      },
    ),
  );
};
