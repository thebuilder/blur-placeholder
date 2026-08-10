---
title: Operations
description: Backfill and retry behavior.
---

The save handler only reacts to the default Image media type when `umbracoFile` is dirty. It clears the placeholder when the source is cleared or invalid, and it queues transient storage failures for a targeted retry.

Backfill is a fingerprinted one-off operation. The last completed fingerprint is stored separately from the media property. A new configuration fingerprint permits one new pass; normal maintenance only drains the retry queue and does not scan the whole library.

The retry queue uses one durable aggregate record with compare-and-set updates. Pending failures remain inspectable and are retried at the configured interval; completed entries are compacted out of the record.

`Enabled: false` stops save-time generation and maintenance without deleting existing placeholders. Clearing or replacing a source file while generation is enabled updates the placeholder in the same media save.

## Schema ownership

The package migration owns the generated data type and property. An existing property is reused only when its data type already uses the package's plain-string schema and read-only preview UI. An incompatible same-alias property is reported by the health check and is never overwritten.

Package migrations are the schema source of truth. Sites that disable unattended migrations must apply the package migration during deployment. Deploy or uSync projects should exclude project-authored copies of this package-owned data type instead of maintaining two competing definitions.

## Troubleshooting

### No property appears

Restart the host after installing the package and check the migration log and health check. A conflicting `blurPlaceholder` property or incompatible editor is reported as a schema compatibility error.

### A value is empty

Confirm that the item uses the default `Image` media type and that `umbracoFile` contains a supported image. Unsupported or corrupt inputs clear stale output and are logged once. Transient file-system or blob-storage failures are queued by media key for retry.

### A native hash does not render

Strip `blurhash:` before passing the text to a BlurHash decoder. For `thumbhash:`, strip the prefix and base64-decode the remaining bytes before calling ThumbHash. When `IncludeAlgorithmPrefix` is disabled, use the configured `Algorithm` to select the decoder. The Backoffice preview also recognizes unprefixed native values and reports malformed values.

### Package restore fails

The extension targets `net10.0` and declares an Umbraco dependency range of `[17.1.0,19.0.0)`. Use a compatible .NET SDK and ensure the configured NuGet source can reach `api.nuget.org`.
