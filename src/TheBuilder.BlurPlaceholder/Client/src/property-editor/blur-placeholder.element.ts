import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import {
  css,
  html,
  nothing,
  customElement,
  property,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { decodePlaceholder } from "./decode-placeholder.js";

type DecodeState =
  | { status: "empty" }
  | { status: "pending"; raw: string }
  | { status: "ready"; raw: string; label: string; preview: string; width: number; height: number }
  | { status: "invalid"; raw: string; error: string };

@customElement("thebuilder-blur-placeholder-property-editor")
export class TheBuilderBlurPlaceholderPropertyEditorElement extends UmbLitElement {
  @property({ type: String }) value = "";
  @property({ type: Boolean, reflect: true }) readonly = false;

  @state() private _decodeState: DecodeState = { status: "empty" };
  @state() private _copyError?: string;
  @state() private _copied = false;
  @state() private _blurred = true;
  private _decodeRequest = 0;

  protected override willUpdate(changedProperties: Map<string, unknown>) {
    if (changedProperties.has("value")) void this._decodeValue();
  }

  private async _decodeValue() {
    const raw = (this.value ?? "").trim();
    const request = ++this._decodeRequest;
    this._copied = false;
    this._blurred = true;
    this._copyError = undefined;

    if (!raw) {
      this._decodeState = { status: "empty" };
      return;
    }

    this._decodeState = { status: "pending", raw };
    try {
      const decoded = await decodePlaceholder(raw);
      const preview = decoded.kind === "webp"
        ? decoded.dataUrl
        : rgbaToDataUrl(decoded.rgba, decoded.width, decoded.height);
      const dimensions = decoded.kind === "webp"
        ? await getImageDimensions(preview)
        : { width: decoded.width, height: decoded.height };
      if (request !== this._decodeRequest) return;
      this._decodeState = {
        status: "ready",
        raw,
        label: decoded.label,
        preview,
        ...dimensions,
      };
    } catch (error) {
      if (request !== this._decodeRequest) return;
      this._decodeState = {
        status: "invalid",
        raw,
        error: error instanceof Error ? error.message : "The placeholder could not be decoded.",
      };
    }
  }

  private async _copyValue() {
    const value = this.value ?? "";
    if (!value) return;

    try {
      await navigator.clipboard.writeText(value);
      this._copied = true;
    } catch {
      this._copyError = "Clipboard access was not granted.";
    }
  }

  private _toggleBlur(event: Event) {
    this._blurred = (event.currentTarget as HTMLElement & { checked: boolean }).checked;
  }

  override render() {
    const value = this.value ?? "";
    const state = this._decodeState;
    const ready = state.status === "ready" ? state : undefined;
    const aspectRatio = ready ? `${ready.width} / ${ready.height}` : "4 / 3";
    return html`
      <div class="editor">
        <div class="preview" style="aspect-ratio: ${aspectRatio}">
          ${ready
            ? html`<img
                class=${this._blurred ? "blurred" : nothing}
                src="${ready.preview}"
                alt="Generated image placeholder"
              />`
            : html`<div class="empty">
                ${state.status === "empty"
                  ? html`<em>Blur placeholder will be generated when the image is saved.</em>`
                  : state.status === "pending" ? "Decoding preview…" : "No preview"}
              </div>`}
        </div>
        <div class="details">
          <div class="metadata">
            <span>${ready?.label ?? (state.status === "invalid" ? "Malformed value" : state.status === "pending" ? "Loading" : "Empty")}</span>
            ${ready ? html`<span>${ready.width} × ${ready.height}</span>` : nothing}
            <span>${value.length} characters</span>
          </div>
          <code title="${value}">${truncate(value)}</code>
          <div class="actions">
            <uui-button look="secondary" label="Copy placeholder" @click=${this._copyValue} ?disabled=${!value}>
              ${this._copied ? "Copied" : "Copy"}
            </uui-button>
            <uui-toggle
              label="Blur preview"
              .checked=${this._blurred}
              ?disabled=${!ready}
              @change=${this._toggleBlur}
            ></uui-toggle>
          </div>
          ${state.status === "invalid" ? html`<p class="error" role="alert">${state.error}</p>` : nothing}
          ${this._copyError ? html`<p class="error" role="alert">${this._copyError}</p>` : nothing}
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
