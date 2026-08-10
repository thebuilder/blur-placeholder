import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import {
  css,
  html,
  nothing,
  customElement,
  property,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { decode as decodeBlurHash } from "blurhash";
import { thumbHashToRGBA } from "thumbhash";

type PlaceholderKind = "webp" | "blurhash" | "thumbhash" | "unknown";

interface PlaceholderInfo {
  kind: PlaceholderKind;
  label: string;
  raw: string;
  preview?: string;
  width?: number;
  height?: number;
}

interface PlaceholderDecodeResult {
  info: PlaceholderInfo;
  error?: string;
}

@customElement("thebuilder-blur-placeholder-property-editor")
export class TheBuilderBlurPlaceholderPropertyEditorElement extends UmbLitElement {
  @property({ type: String }) value = "";
  @property({ type: Boolean, reflect: true }) readonly = false;

  @state() private _info?: PlaceholderInfo;
  @state() private _error?: string;
  @state() private _copied = false;
  @state() private _blurred = true;

  protected override willUpdate(changedProperties: Map<string, unknown>) {
    if (changedProperties.has("value")) void this._decodeValue();
  }

  private async _decodeValue() {
    const raw = this.value.trim();
    this._copied = false;
    this._blurred = true;
    this._error = undefined;

    if (!raw) {
      this._info = undefined;
      return;
    }

    try {
      const decoded = await decodePlaceholder(raw);
      if (this.value.trim() !== raw) return;
      this._info = decoded.info;
      this._error = decoded.error;
    } catch (error) {
      if (this.value.trim() !== raw) return;
      this._info = { kind: "unknown", label: "Malformed value", raw };
      this._error = error instanceof Error ? error.message : "The placeholder could not be decoded.";
    }
  }

  private async _copyValue() {
    if (!this.value) return;

    try {
      await navigator.clipboard.writeText(this.value);
      this._copied = true;
    } catch {
      this._error = "Clipboard access was not granted.";
    }
  }

  private _toggleBlur(event: Event) {
    this._blurred = (event.currentTarget as HTMLElement & { checked: boolean }).checked;
  }

  override render() {
    const info = this._info;
    const aspectRatio = info?.width && info.height ? `${info.width} / ${info.height}` : "4 / 3";
    return html`
      <div class="editor">
        <div class="preview" style="aspect-ratio: ${aspectRatio}">
          ${info?.preview
            ? html`<img
                class=${this._blurred ? "blurred" : nothing}
                src="${info.preview}"
                alt="Generated image placeholder"
              />`
            : html`<div class="empty">
                ${this.value
                  ? "No preview"
                  : html`<em>Blur placeholder will be generated when the image is saved.</em>`}
              </div>`}
        </div>
        <div class="details">
          <div class="metadata">
            <span>${info?.label ?? "Empty"}</span>
            ${info?.width && info?.height ? html`<span>${info.width} × ${info.height}</span>` : nothing}
            <span>${this.value.length} characters</span>
          </div>
          <code title="${this.value}">${truncate(this.value)}</code>
          <div class="actions">
            <uui-button look="secondary" label="Copy placeholder" @click=${this._copyValue} ?disabled=${!this.value}>
              ${this._copied ? "Copied" : "Copy"}
            </uui-button>
            <uui-toggle
              label="Blur preview"
              .checked=${this._blurred}
              ?disabled=${!info?.preview}
              @change=${this._toggleBlur}
            ></uui-toggle>
          </div>
          ${this._error ? html`<p class="error" role="alert">${this._error}</p>` : nothing}
        </div>
      </div>
    `;
  }

  static override styles = css`
      :host { display: block; }
      .editor { display: grid; grid-template-columns: minmax(9rem, 14rem) 1fr; gap: var(--uui-size-space-4); }
      .preview { align-self: start; overflow: hidden; border-radius: var(--uui-border-radius); background: var(--uui-color-surface-alt); }
      img { display: block; width: 100%; height: 100%; object-fit: cover; }
      img.blurred { filter: blur(18px); transform: scale(1.12); }
      .empty { display: grid; place-items: center; height: 100%; padding: var(--uui-size-space-4); color: var(--uui-color-text-alt); text-align: center; }
      .empty em { font-size: var(--uui-type-small-size); }
      .details { display: grid; align-content: start; gap: var(--uui-size-space-3); min-width: 0; }
      .metadata { display: flex; flex-wrap: wrap; gap: var(--uui-size-space-3); color: var(--uui-color-text-alt); font-size: var(--uui-type-small-size); }
      code { display: block; overflow: hidden; color: var(--uui-color-text); text-overflow: ellipsis; white-space: nowrap; }
      .actions { display: grid; justify-items: start; gap: var(--uui-size-space-3); }
      .error { margin: 0; color: var(--uui-color-danger); }
      @media (max-width: 42rem) { .editor { grid-template-columns: 1fr; } }
  `;
}

function truncate(value: string, length = 96) {
  return value.length > length ? `${value.slice(0, length)}…` : value;
}

async function decodePlaceholder(raw: string): Promise<PlaceholderDecodeResult> {
  if (raw.startsWith("data:image/webp;base64,")) {
    const { width, height } = await getImageDimensions(raw);
    return {
      info: { kind: "webp", label: "WebP data URL", raw, preview: raw, width, height },
    };
  }

  if (raw.startsWith("blurhash:")) {
    return decodeBlurHashValue(raw.slice("blurhash:".length), raw);
  }

  if (raw.startsWith("thumbhash:")) {
    return decodeThumbHashValue(raw.slice("thumbhash:".length), raw);
  }

  if (looksLikeBlurHash(raw)) {
    return decodeBlurHashValue(raw, raw, "BlurHash (unprefixed)");
  }

  try {
    return decodeThumbHashValue(raw, raw, "ThumbHash (unprefixed)");
  } catch {
    return {
      info: { kind: "unknown", label: "Unknown value", raw },
      error: "The value is not a recognized WebP, BlurHash, or ThumbHash placeholder.",
    };
  }
}

function decodeBlurHashValue(
  value: string,
  raw: string,
  label = "BlurHash",
): PlaceholderDecodeResult {
  const width = 32;
  const height = 24;
  const rgba = decodeBlurHash(value, width, height);
  return {
    info: {
      kind: "blurhash",
      label,
      raw,
      preview: rgbaToDataUrl(rgba, width, height),
      width,
      height,
    },
  };
}

function decodeThumbHashValue(
  value: string,
  raw: string,
  label = "ThumbHash",
): PlaceholderDecodeResult {
  const bytes = base64ToBytes(value);
  if (bytes.length < 5) throw new Error("ThumbHash is too short.");
  const decoded = thumbHashToRGBA(bytes);
  return {
    info: {
      kind: "thumbhash",
      label,
      raw,
      preview: rgbaToDataUrl(decoded.rgba, decoded.w, decoded.h),
      width: decoded.w,
      height: decoded.h,
    },
  };
}

function looksLikeBlurHash(value: string) {
  const alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$%*+,-.:;=?@[]^_{|}~";
  if (value.length < 6 || [...value].some((character) => !alphabet.includes(character))) return false;

  const sizeFlag = alphabet.indexOf(value[0]);
  const componentsX = (sizeFlag % 9) + 1;
  const componentsY = Math.floor(sizeFlag / 9) + 1;
  return value.length === 4 + 2 * componentsX * componentsY;
}

function base64ToBytes(value: string) {
  const binary = atob(value);
  return Uint8Array.from(binary, (character) => character.charCodeAt(0));
}

function rgbaToDataUrl(rgba: Uint8Array | Uint8ClampedArray, width: number, height: number) {
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  const context = canvas.getContext("2d");
  if (!context) throw new Error("Canvas rendering is unavailable.");
  context.putImageData(new ImageData(new Uint8ClampedArray(rgba), width, height), 0, 0);
  return canvas.toDataURL("image/webp", 0.6);
}

function getImageDimensions(source: string): Promise<{ width: number; height: number }> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve({ width: image.naturalWidth, height: image.naturalHeight });
    image.onerror = () => reject(new Error("The WebP data URL could not be decoded."));
    image.src = source;
  });
}

export default TheBuilderBlurPlaceholderPropertyEditorElement;

declare global {
  interface HTMLElementTagNameMap {
    "thebuilder-blur-placeholder-property-editor": TheBuilderBlurPlaceholderPropertyEditorElement;
  }
}
