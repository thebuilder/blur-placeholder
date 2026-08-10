import { decode as decodeBlurHash } from "blurhash";
import { thumbHashToRGBA } from "thumbhash";

export type DecodedPlaceholder =
  | {
      kind: "webp";
      label: "WebP data URL";
      dataUrl: string;
    }
  | {
      kind: "blurhash" | "thumbhash";
      label: "BlurHash" | "ThumbHash";
      rgba: Uint8Array | Uint8ClampedArray;
      width: number;
      height: number;
    };

export function decodePlaceholder(raw: string): DecodedPlaceholder {
  if (raw.startsWith("data:image/webp;base64,")) {
    return { kind: "webp", label: "WebP data URL", dataUrl: raw };
  }

  if (raw.startsWith("blurhash:")) {
    const width = 32;
    const height = 24;
    return {
      kind: "blurhash",
      label: "BlurHash",
      rgba: decodeBlurHash(raw.slice("blurhash:".length), width, height),
      width,
      height,
    };
  }

  if (raw.startsWith("thumbhash:")) {
    const bytes = base64ToBytes(raw.slice("thumbhash:".length));
    validateThumbHash(bytes);
    const decoded = thumbHashToRGBA(bytes);
    return {
      kind: "thumbhash",
      label: "ThumbHash",
      rgba: decoded.rgba,
      width: decoded.w,
      height: decoded.h,
    };
  }

  throw new Error("The value is not a prefixed WebP, BlurHash, or ThumbHash placeholder.");
}

function base64ToBytes(value: string) {
  const binary = atob(value);
  return Uint8Array.from(binary, (character) => character.charCodeAt(0));
}

function validateThumbHash(hash: Uint8Array) {
  if (hash.length < 5) throw new Error("ThumbHash is too short.");

  const header24 = hash[0] | (hash[1] << 8) | (hash[2] << 16);
  const header16 = hash[3] | (hash[4] << 8);
  const hasAlpha = (header24 >> 23) === 1;
  const isLandscape = (header16 >> 15) === 1;
  const lx = Math.max(3, isLandscape ? (hasAlpha ? 5 : 7) : header16 & 7);
  const ly = Math.max(3, isLandscape ? header16 & 7 : hasAlpha ? 5 : 7);
  const coefficientCount = countCoefficients(lx, ly) + 2 * countCoefficients(3, 3)
    + (hasAlpha ? countCoefficients(5, 5) : 0);
  const expectedLength = (hasAlpha ? 6 : 5) + Math.ceil(coefficientCount / 2);

  if (hash.length !== expectedLength) {
    throw new Error(`ThumbHash has ${hash.length} bytes; expected ${expectedLength}.`);
  }
}

function countCoefficients(nx: number, ny: number) {
  let count = 0;
  for (let cy = 0; cy < ny; cy++) {
    for (let cx = cy ? 0 : 1; cx * ny < nx * (ny - cy); cx++) count++;
  }
  return count;
}
