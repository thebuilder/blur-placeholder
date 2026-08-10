---
title: Operations
description: Backfill and retry behavior.
seo:
  image: /og/operations.png
---

The save handler only reacts to the default Image media type when `umbracoFile` is dirty. It clears the placeholder when the source is cleared, invalid, or temporarily unavailable. The media save remains atomic: no separate retry state is changed before the media itself is persisted.

Backfill is a fingerprinted operation. The last completed fingerprint is stored separately from the media property. A new configuration fingerprint permits one regeneration pass, while normal maintenance scans for empty placeholders left by transient failures.

Maintenance checkpoints the next media-library page after each completed page. A cancelled or interrupted pass resumes from that checkpoint instead of restarting at page zero. The cursor is discarded when its fingerprint or regeneration mode no longer matches the active configuration.

`Enabled: false` stops save-time generation and maintenance without deleting existing placeholders. Clearing or replacing a source file while generation is enabled updates the placeholder in the same media save.

## Schema ownership

The package migration owns the generated data type and property. An existing property is reused only when its data type already uses the package's plain-string schema and read-only preview UI. An incompatible same-alias property is reported by the health check and is never overwritten.

Package migrations are the schema source of truth. Sites that disable unattended migrations must apply the package migration during deployment. Deploy or uSync projects should exclude project-authored copies of this package-owned data type instead of maintaining two competing definitions.

## Troubleshooting

### No property appears

Restart the host after installing the package and check the migration log and health check. A conflicting `blurPlaceholder` property or incompatible editor is reported as a schema compatibility error.

### A value is empty

Confirm that the item uses the default `Image` media type and that `umbracoFile` contains a supported image. Unsupported or corrupt inputs clear stale output and are logged once. Transient file-system or blob-storage failures leave the property empty for the next maintenance pass.

### A native hash does not render

Strip `blurhash:` before passing the text to a BlurHash decoder. For `thumbhash:`, strip the prefix and base64-decode the remaining bytes before calling ThumbHash. The Backoffice preview rejects unprefixed or malformed native values instead of guessing which algorithm produced them.

### Package restore fails

The extension targets `net10.0` and declares an Umbraco dependency range of `[17.1.0,19.0.0)`. Use a compatible .NET SDK and ensure the configured NuGet source can reach `api.nuget.org`.
