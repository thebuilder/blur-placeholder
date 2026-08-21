import { UmbLitElement as le } from "@umbraco-cms/backoffice/lit-element";
import { nothing as C, html as g, css as se, property as ee, state as V, customElement as ce } from "@umbraco-cms/backoffice/external/lit";
var de = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "#", "$", "%", "*", "+", ",", "-", ".", ":", ";", "=", "?", "@", "[", "]", "^", "_", "{", "|", "}", "~"], R = (e) => {
  let r = 0;
  for (let t = 0; t < e.length; t++) {
    let a = e[t], o = de.indexOf(a);
    r = r * 83 + o;
  }
  return r;
}, G = (e) => {
  let r = e / 255;
  return r <= 0.04045 ? r / 12.92 : Math.pow((r + 0.055) / 1.055, 2.4);
}, F = (e) => {
  let r = Math.max(0, Math.min(1, e));
  return r <= 31308e-7 ? Math.trunc(r * 12.92 * 255 + 0.5) : Math.trunc((1.055 * Math.pow(r, 0.4166666666666667) - 0.055) * 255 + 0.5);
}, ne = (e) => e < 0 ? -1 : 1, N = (e, r) => ne(e) * Math.pow(Math.abs(e), r), Z = class extends Error {
  constructor(e) {
    super(e), this.name = "ValidationError", this.message = e;
  }
}, ue = (e) => {
  if (!e || e.length < 6) throw new Z("The blurhash string must be at least 6 characters");
  let r = R(e[0]), t = Math.floor(r / 9) + 1, a = r % 9 + 1;
  if (e.length !== 4 + 2 * a * t) throw new Z(`blurhash length mismatch: length is ${e.length} but it should be ${4 + 2 * a * t}`);
}, he = (e) => {
  let r = e >> 16, t = e >> 8 & 255, a = e & 255;
  return [G(r), G(t), G(a)];
}, pe = (e, r) => {
  let t = Math.floor(e / 361), a = Math.floor(e / 19) % 19, o = e % 19;
  return [N((t - 9) / 9, 2) * r, N((a - 9) / 9, 2) * r, N((o - 9) / 9, 2) * r];
}, be = (e, r, t, a) => {
  ue(e), a = a | 1;
  let o = R(e[0]), d = Math.floor(o / 9) + 1, c = o % 9 + 1, p = (R(e[1]) + 1) / 166, b = new Array(c * d);
  for (let s = 0; s < b.length; s++) if (s === 0) {
    let l = R(e.substring(2, 6));
    b[s] = he(l);
  } else {
    let l = R(e.substring(4 + s * 2, 6 + s * 2));
    b[s] = pe(l, p * a);
  }
  let v = r * 4, y = new Uint8ClampedArray(v * t);
  for (let s = 0; s < t; s++) for (let l = 0; l < r; l++) {
    let q = 0, H = 0, S = 0;
    for (let $ = 0; $ < d; $++) for (let M = 0; M < c; M++) {
      let E = Math.cos(Math.PI * l * M / r) * Math.cos(Math.PI * s * $ / t), m = b[M + $ * c];
      q += m[0] * E, H += m[1] * E, S += m[2] * E;
    }
    let z = F(q), w = F(H), O = F(S);
    y[4 * l + 0 + s * v] = z, y[4 * l + 1 + s * v] = w, y[4 * l + 2 + s * v] = O, y[4 * l + 3 + s * v] = 255;
  }
  return y;
}, fe = be;
function me(e) {
  let { PI: r, min: t, max: a, cos: o, round: d } = Math, c = e[0] | e[1] << 8 | e[2] << 16, p = e[3] | e[4] << 8, b = (c & 63) / 63, v = (c >> 6 & 63) / 31.5 - 1, y = (c >> 12 & 63) / 31.5 - 1, s = (c >> 18 & 31) / 31, l = c >> 23, q = (p >> 3 & 63) / 63, H = (p >> 9 & 63) / 63, S = p >> 15, z = a(3, S ? l ? 5 : 7 : p & 7), w = a(3, S ? p & 7 : l ? 5 : 7), O = l ? (e[5] & 15) / 15 : 1, $ = (e[5] >> 4) / 15, M = l ? 6 : 5, E = 0, m = (U, f, A) => {
    let P = [];
    for (let _ = 0; _ < f; _++)
      for (let T = _ ? 0 : 1; T * f < U * (f - _); T++)
        P.push(((e[M + (E >> 1)] >> ((E++ & 1) << 2) & 15) / 7.5 - 1) * A);
    return P;
  }, te = m(z, w, s), re = m(3, 3, q * 1.25), ae = m(3, 3, H * 1.25), oe = l && m(5, 5, $), L = ge(e), D = d(L > 1 ? 32 : 32 * L), I = d(L > 1 ? 32 / L : 32), B = new Uint8Array(D * I * 4), j = [], W = [];
  for (let U = 0, f = 0; U < I; U++)
    for (let A = 0; A < D; A++, f += 4) {
      let P = b, _ = v, T = y, K = O;
      for (let i = 0, n = a(z, l ? 5 : 3); i < n; i++)
        j[i] = o(r / D * (A + 0.5) * i);
      for (let i = 0, n = a(w, l ? 5 : 3); i < n; i++)
        W[i] = o(r / I * (U + 0.5) * i);
      for (let i = 0, n = 0; i < w; i++)
        for (let u = i ? 0 : 1, k = W[i] * 2; u * w < z * (w - i); u++, n++)
          P += te[n] * j[u] * k;
      for (let i = 0, n = 0; i < 3; i++)
        for (let u = i ? 0 : 1, k = W[i] * 2; u < 3 - i; u++, n++) {
          let Y = j[u] * k;
          _ += re[n] * Y, T += ae[n] * Y;
        }
      if (l)
        for (let i = 0, n = 0; i < 5; i++)
          for (let u = i ? 0 : 1, k = W[i] * 2; u < 5 - i; u++, n++)
            K += oe[n] * j[u] * k;
      let Q = P - 2 / 3 * _, X = (3 * P - Q + T) / 2, ie = X - T;
      B[f] = a(0, 255 * t(1, X)), B[f + 1] = a(0, 255 * t(1, ie)), B[f + 2] = a(0, 255 * t(1, Q)), B[f + 3] = a(0, 255 * t(1, K));
    }
  return { w: D, h: I, rgba: B };
}
function ge(e) {
  let r = e[3], t = e[2] & 128, a = e[4] & 128, o = a ? t ? 5 : 7 : r & 7, d = a ? r & 7 : t ? 5 : 7;
  return o / d;
}
function ve(e) {
  if (e.startsWith("data:image/webp;base64,"))
    return { kind: "webp", label: "WebP data URL", dataUrl: e };
  if (e.startsWith("blurhash:"))
    return {
      kind: "blurhash",
      label: "BlurHash",
      rgba: fe(e.slice(9), 32, 24),
      width: 32,
      height: 24
    };
  if (e.startsWith("thumbhash:")) {
    const r = ye(e.slice(10));
    we(r);
    const t = me(r);
    return {
      kind: "thumbhash",
      label: "ThumbHash",
      rgba: t.rgba,
      width: t.w,
      height: t.h
    };
  }
  throw new Error("The value is not a prefixed WebP, BlurHash, or ThumbHash placeholder.");
}
function ye(e) {
  const r = atob(e);
  return Uint8Array.from(r, (t) => t.charCodeAt(0));
}
function we(e) {
  if (e.length < 5) throw new Error("ThumbHash is too short.");
  const r = e[0] | e[1] << 8 | e[2] << 16, t = e[3] | e[4] << 8, a = r >> 23 === 1, o = t >> 15 === 1, d = Math.max(3, o ? a ? 5 : 7 : t & 7), c = Math.max(3, o ? t & 7 : a ? 5 : 7), p = J(d, c) + 2 * J(3, 3) + (a ? J(5, 5) : 0), b = (a ? 6 : 5) + Math.ceil(p / 2);
  if (e.length !== b)
    throw new Error(`ThumbHash has ${e.length} bytes; expected ${b}.`);
}
function J(e, r) {
  let t = 0;
  for (let a = 0; a < r; a++)
    for (let o = a ? 0 : 1; o * r < e * (r - a); o++) t++;
  return t;
}
var _e = Object.defineProperty, xe = Object.getOwnPropertyDescriptor, x = (e, r, t, a) => {
  for (var o = a > 1 ? void 0 : a ? xe(r, t) : r, d = e.length - 1, c; d >= 0; d--)
    (c = e[d]) && (o = (a ? c(r, t, o) : c(o)) || o);
  return a && o && _e(r, t, o), o;
};
let h = class extends le {
  constructor() {
    super(...arguments), this.value = "", this.readonly = !1, this._decodeState = { status: "empty" }, this._copied = !1, this._blurred = !0, this._decodeRequest = 0;
  }
  willUpdate(e) {
    e.has("value") && this._decodeValue();
  }
  async _decodeValue() {
    const e = (this.value ?? "").trim(), r = ++this._decodeRequest;
    if (this._copied = !1, this._blurred = !0, this._copyError = void 0, !e) {
      this._decodeState = { status: "empty" };
      return;
    }
    this._decodeState = { status: "pending", raw: e };
    try {
      const t = await ve(e), a = t.kind === "webp" ? t.dataUrl : Me(t.rgba, t.width, t.height), o = t.kind === "webp" ? await Ee(a) : { width: t.width, height: t.height };
      if (r !== this._decodeRequest) return;
      this._decodeState = {
        status: "ready",
        raw: e,
        label: t.label,
        preview: a,
        ...o
      };
    } catch (t) {
      if (r !== this._decodeRequest) return;
      this._decodeState = {
        status: "invalid",
        raw: e,
        error: t instanceof Error ? t.message : "The placeholder could not be decoded."
      };
    }
  }
  async _copyValue() {
    const e = this.value ?? "";
    if (e)
      try {
        await navigator.clipboard.writeText(e), this._copied = !0;
      } catch {
        this._copyError = "Clipboard access was not granted.";
      }
  }
  _toggleBlur() {
    this._blurred = !this._blurred;
  }
  render() {
    const e = this.value ?? "", r = this._decodeState, t = r.status === "ready" ? r : void 0, a = t ? `${t.width} / ${t.height}` : "4 / 3";
    return g`
      <div class="editor">
        <button
          class="preview"
          type="button"
          style="aspect-ratio: ${a}"
          aria-label=${this._blurred ? "Show preview without CSS blur" : "Show preview with CSS blur"}
          aria-pressed=${this._blurred}
          title=${t ? "Toggle CSS blur preview" : C}
          ?disabled=${!t}
          @click=${this._toggleBlur}
        >
          ${t ? g`<img
                class=${this._blurred ? "blurred" : C}
                src="${t.preview}"
                alt="Generated image placeholder"
              />` : g`<div class="empty">
                ${r.status === "empty" ? g`<em>Blur placeholder will be generated when the image is saved.</em>` : r.status === "pending" ? "Decoding preview…" : "No preview"}
              </div>`}
        </button>
        ${r.status !== "empty" ? g`<div class="details">
              <div class="metadata">
                <span>${t?.label ?? (r.status === "invalid" ? "Malformed value" : "Loading")}</span>
                ${t ? g`<span>${t.width} × ${t.height}</span>` : C}
                <span>${e.length} characters</span>
              </div>
              <div class="value">
                <code title="${e}">${$e(e)}</code>
                <button
                  class="copy"
                  type="button"
                  aria-label=${this._copied ? "Placeholder copied" : "Copy placeholder"}
                  title=${this._copied ? "Copied" : "Copy placeholder"}
                  @click=${this._copyValue}
                >
                  <uui-icon name=${this._copied ? "icon-check" : "icon-documents"}></uui-icon>
                </button>
              </div>
              ${r.status === "invalid" ? g`<p class="error" role="alert">${r.error}</p>` : C}
              ${this._copyError ? g`<p class="error" role="alert">${this._copyError}</p>` : C}
            </div>` : C}
      </div>
    `;
  }
};
h.styles = se`
      :host { display: block; }
      .editor { display: grid; grid-template-columns: minmax(9rem, 14rem) minmax(0, 40rem); gap: var(--uui-size-space-6); }
      .preview { display: block; align-self: start; overflow: hidden; width: 100%; padding: 0; border: 0; border-radius: var(--uui-border-radius); background: var(--uui-color-surface-alt); color: inherit; }
      .preview:not(:disabled) { cursor: pointer; }
      .preview:focus-visible, .copy:focus-visible { outline: 2px solid var(--uui-color-focus); outline-offset: 2px; }
      img { display: block; width: 100%; height: 100%; object-fit: cover; }
      img.blurred { filter: blur(18px); transform: scale(1.12); }
      .empty { box-sizing: border-box; display: grid; place-items: center; width: 100%; height: 100%; padding: var(--uui-size-space-4); color: var(--uui-color-text-alt); text-align: center; }
      .empty em { font-size: var(--uui-type-small-size); }
      .details { display: grid; align-content: start; gap: var(--uui-size-space-3); min-width: 0; }
      .metadata { display: flex; flex-wrap: wrap; gap: var(--uui-size-space-3); color: var(--uui-color-text-alt); font-size: var(--uui-type-small-size); }
      .value { display: grid; grid-template-columns: minmax(0, 1fr) auto; align-items: stretch; overflow: hidden; border: 1px solid var(--uui-color-border); border-radius: var(--uui-border-radius); background: var(--uui-color-surface-alt); }
      code { display: block; overflow: hidden; padding: var(--uui-size-space-3) var(--uui-size-space-4); color: var(--uui-color-text); text-overflow: ellipsis; white-space: nowrap; }
      .copy { display: grid; place-items: center; min-width: var(--uui-size-11); padding: 0 var(--uui-size-space-3); border: 0; border-inline-start: 1px solid var(--uui-color-border); background: transparent; color: var(--uui-color-interactive); cursor: pointer; }
      .copy:hover:not(:disabled) { background: var(--uui-color-surface-emphasis); }
      .copy:disabled { color: var(--uui-color-disabled-contrast); cursor: default; }
      .error { margin: 0; color: var(--uui-color-danger); }
      @media (max-width: 42rem) { .editor { grid-template-columns: 1fr; } }
  `;
x([
  ee({ type: String })
], h.prototype, "value", 2);
x([
  ee({ type: Boolean, reflect: !0 })
], h.prototype, "readonly", 2);
x([
  V()
], h.prototype, "_decodeState", 2);
x([
  V()
], h.prototype, "_copyError", 2);
x([
  V()
], h.prototype, "_copied", 2);
x([
  V()
], h.prototype, "_blurred", 2);
h = x([
  ce("thebuilder-blur-placeholder-property-editor")
], h);
function $e(e, r = 96) {
  return e.length > r ? `${e.slice(0, r)}…` : e;
}
function Me(e, r, t) {
  const a = document.createElement("canvas");
  a.width = r, a.height = t;
  const o = a.getContext("2d");
  if (!o) throw new Error("Canvas rendering is unavailable.");
  return o.putImageData(new ImageData(new Uint8ClampedArray(e), r, t), 0, 0), a.toDataURL("image/webp", 0.6);
}
function Ee(e) {
  return new Promise((r, t) => {
    const a = new Image();
    a.onload = () => r({ width: a.naturalWidth, height: a.naturalHeight }), a.onerror = () => t(new Error("The WebP data URL could not be decoded.")), a.src = e;
  });
}
const Ce = h;
export {
  h as TheBuilderBlurPlaceholderPropertyEditorElement,
  Ce as default
};
//# sourceMappingURL=blur-placeholder.element-Cp_wfwFl.js.map
