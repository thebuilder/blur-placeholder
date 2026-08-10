import assert from "node:assert/strict";
import { decodePlaceholder } from "../src/property-editor/decode-placeholder.ts";
import fixtures from "./fixtures.json" with { type: "json" };

const blurHash = decodePlaceholder(fixtures.BlurHash);
assert.equal(blurHash.kind, "blurhash");
assert.equal(blurHash.rgba.length, blurHash.width * blurHash.height * 4);
assert.ok(blurHash.rgba.some((channel) => channel !== 0));

const thumbHash = decodePlaceholder(fixtures.ThumbHash);
assert.equal(thumbHash.kind, "thumbhash");
assert.ok(thumbHash.width > 0);
assert.ok(thumbHash.height > 0);
assert.equal(thumbHash.rgba.length, thumbHash.width * thumbHash.height * 4);

const webp = decodePlaceholder("data:image/webp;base64,AAAA");
assert.deepEqual(webp, {
  kind: "webp",
  label: "WebP data URL",
  dataUrl: "data:image/webp;base64,AAAA",
});

assert.throws(() => decodePlaceholder(fixtures.BlurHash.slice("blurhash:".length)), /prefixed/);
assert.throws(() => decodePlaceholder("thumbhash:YWJjZGVm"), /expected/);
assert.throws(() => decodePlaceholder("not-a-placeholder"), /prefixed/);

console.log(`Decoded production fixtures: BlurHash ${blurHash.rgba.length / 4} pixels; ThumbHash ${thumbHash.width}x${thumbHash.height}.`);
