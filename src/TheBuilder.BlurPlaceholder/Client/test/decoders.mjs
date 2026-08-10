import assert from "node:assert/strict";
import { decode } from "blurhash";
import { thumbHashToRGBA } from "thumbhash";
import fixtures from "./fixtures.json" with { type: "json" };

const blurHash = fixtures.BlurHash.slice("blurhash:".length);
const blurHashPixels = decode(blurHash, 32, 24);
assert.equal(blurHashPixels.length, 32 * 24 * 4);
assert.ok(blurHashPixels.some((channel) => channel !== 0));

const thumbHash = fixtures.ThumbHash.slice("thumbhash:".length);
const thumbHashBytes = Uint8Array.from(atob(thumbHash), (character) => character.charCodeAt(0));
const thumbHashPixels = thumbHashToRGBA(thumbHashBytes);
assert.ok(thumbHashPixels.w > 0);
assert.ok(thumbHashPixels.h > 0);
assert.equal(thumbHashPixels.rgba.length, thumbHashPixels.w * thumbHashPixels.h * 4);

console.log(`Decoded C# fixtures: BlurHash ${blurHashPixels.length / 4} pixels; ThumbHash ${thumbHashPixels.w}x${thumbHashPixels.h}.`);
