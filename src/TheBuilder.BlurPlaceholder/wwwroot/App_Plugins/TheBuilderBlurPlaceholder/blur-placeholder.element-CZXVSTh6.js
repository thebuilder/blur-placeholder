import { UmbLitElement as ie } from "@umbraco-cms/backoffice/lit-element";
import { nothing as W, html as _, css as se, property as ee, state as V, customElement as ne } from "@umbraco-cms/backoffice/external/lit";
var ce = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "#", "$", "%", "*", "+", ",", "-", ".", ":", ";", "=", "?", "@", "[", "]", "^", "_", "{", "|", "}", "~"], z = (e) => {
  let t = 0;
  for (let r = 0; r < e.length; r++) {
    let a = e[r], l = ce.indexOf(a);
    t = t * 83 + l;
  }
  return t;
}, G = (e) => {
  let t = e / 255;
  return t <= 0.04045 ? t / 12.92 : Math.pow((t + 0.055) / 1.055, 2.4);
}, F = (e) => {
  let t = Math.max(0, Math.min(1, e));
  return t <= 31308e-7 ? Math.trunc(t * 12.92 * 255 + 0.5) : Math.trunc((1.055 * Math.pow(t, 0.4166666666666667) - 0.055) * 255 + 0.5);
}, de = (e) => e < 0 ? -1 : 1, N = (e, t) => de(e) * Math.pow(Math.abs(e), t), Z = class extends Error {
  constructor(e) {
    super(e), this.name = "ValidationError", this.message = e;
  }
}, ue = (e) => {
  if (!e || e.length < 6) throw new Z("The blurhash string must be at least 6 characters");
  let t = z(e[0]), r = Math.floor(t / 9) + 1, a = t % 9 + 1;
  if (e.length !== 4 + 2 * a * r) throw new Z(`blurhash length mismatch: length is ${e.length} but it should be ${4 + 2 * a * r}`);
}, he = (e) => {
  let t = e >> 16, r = e >> 8 & 255, a = e & 255;
  return [G(t), G(r), G(a)];
}, pe = (e, t) => {
  let r = Math.floor(e / 361), a = Math.floor(e / 19) % 19, l = e % 19;
  return [N((r - 9) / 9, 2) * t, N((a - 9) / 9, 2) * t, N((l - 9) / 9, 2) * t];
}, fe = (e, t, r, a) => {
  ue(e), a = a | 1;
  let l = z(e[0]), c = Math.floor(l / 9) + 1, n = l % 9 + 1, p = (z(e[1]) + 1) / 166, f = new Array(n * c);
  for (let s = 0; s < f.length; s++) if (s === 0) {
    let i = z(e.substring(2, 6));
    f[s] = he(i);
  } else {
    let i = z(e.substring(4 + s * 2, 6 + s * 2));
    f[s] = pe(i, p * a);
  }
  let b = t * 4, y = new Uint8ClampedArray(b * r);
  for (let s = 0; s < r; s++) for (let i = 0; i < t; i++) {
    let H = 0, L = 0, B = 0;
    for (let $ = 0; $ < c; $++) for (let E = 0; E < n; E++) {
      let M = Math.cos(Math.PI * i * E / t) * Math.cos(Math.PI * s * $ / r), m = f[E + $ * n];
      H += m[0] * M, L += m[1] * M, B += m[2] * M;
    }
    let C = F(H), v = F(L), O = F(B);
    y[4 * i + 0 + s * b] = C, y[4 * i + 1 + s * b] = v, y[4 * i + 2 + s * b] = O, y[4 * i + 3 + s * b] = 255;
  }
  return y;
}, ge = fe;
function me(e) {
  let { PI: t, min: r, max: a, cos: l, round: c } = Math, n = e[0] | e[1] << 8 | e[2] << 16, p = e[3] | e[4] << 8, f = (n & 63) / 63, b = (n >> 6 & 63) / 31.5 - 1, y = (n >> 12 & 63) / 31.5 - 1, s = (n >> 18 & 31) / 31, i = n >> 23, H = (p >> 3 & 63) / 63, L = (p >> 9 & 63) / 63, B = p >> 15, C = a(3, B ? i ? 5 : 7 : p & 7), v = a(3, B ? p & 7 : i ? 5 : 7), O = i ? (e[5] & 15) / 15 : 1, $ = (e[5] >> 4) / 15, E = i ? 6 : 5, M = 0, m = (A, g, R) => {
    let T = [];
    for (let w = 0; w < g; w++)
      for (let P = w ? 0 : 1; P * g < A * (g - w); P++)
        T.push(((e[E + (M >> 1)] >> ((M++ & 1) << 2) & 15) / 7.5 - 1) * R);
    return T;
  }, te = m(C, v, s), re = m(3, 3, H * 1.25), ae = m(3, 3, L * 1.25), le = i && m(5, 5, $), j = be(e), k = c(j > 1 ? 32 : 32 * j), D = c(j > 1 ? 32 / j : 32), U = new Uint8Array(k * D * 4), I = [], S = [];
  for (let A = 0, g = 0; A < D; A++)
    for (let R = 0; R < k; R++, g += 4) {
      let T = f, w = b, P = y, K = O;
      for (let o = 0, d = a(C, i ? 5 : 3); o < d; o++)
        I[o] = l(t / k * (R + 0.5) * o);
      for (let o = 0, d = a(v, i ? 5 : 3); o < d; o++)
        S[o] = l(t / D * (A + 0.5) * o);
      for (let o = 0, d = 0; o < v; o++)
        for (let u = o ? 0 : 1, q = S[o] * 2; u * v < C * (v - o); u++, d++)
          T += te[d] * I[u] * q;
      for (let o = 0, d = 0; o < 3; o++)
        for (let u = o ? 0 : 1, q = S[o] * 2; u < 3 - o; u++, d++) {
          let Y = I[u] * q;
          w += re[d] * Y, P += ae[d] * Y;
        }
      if (i)
        for (let o = 0, d = 0; o < 5; o++)
          for (let u = o ? 0 : 1, q = S[o] * 2; u < 5 - o; u++, d++)
            K += le[d] * I[u] * q;
      let Q = T - 2 / 3 * w, X = (3 * T - Q + P) / 2, oe = X - P;
      U[g] = a(0, 255 * r(1, X)), U[g + 1] = a(0, 255 * r(1, oe)), U[g + 2] = a(0, 255 * r(1, Q)), U[g + 3] = a(0, 255 * r(1, K));
    }
  return { w: k, h: D, rgba: U };
}
function be(e) {
  let t = e[3], r = e[2] & 128, a = e[4] & 128, l = a ? r ? 5 : 7 : t & 7, c = a ? t & 7 : r ? 5 : 7;
  return l / c;
}
function ye(e) {
  if (e.startsWith("data:image/webp;base64,"))
    return { kind: "webp", label: "WebP data URL", dataUrl: e };
  if (e.startsWith("blurhash:"))
    return {
      kind: "blurhash",
      label: "BlurHash",
      rgba: ge(e.slice(9), 32, 24),
      width: 32,
      height: 24
    };
  if (e.startsWith("thumbhash:")) {
    const t = ve(e.slice(10));
    we(t);
    const r = me(t);
    return {
      kind: "thumbhash",
      label: "ThumbHash",
      rgba: r.rgba,
      width: r.w,
      height: r.h
    };
  }
  throw new Error("The value is not a prefixed WebP, BlurHash, or ThumbHash placeholder.");
}
function ve(e) {
  const t = atob(e);
  return Uint8Array.from(t, (r) => r.charCodeAt(0));
}
function we(e) {
  if (e.length < 5) throw new Error("ThumbHash is too short.");
  const t = e[0] | e[1] << 8 | e[2] << 16, r = e[3] | e[4] << 8, a = t >> 23 === 1, l = r >> 15 === 1, c = Math.max(3, l ? a ? 5 : 7 : r & 7), n = Math.max(3, l ? r & 7 : a ? 5 : 7), p = J(c, n) + 2 * J(3, 3) + (a ? J(5, 5) : 0), f = (a ? 6 : 5) + Math.ceil(p / 2);
  if (e.length !== f)
    throw new Error(`ThumbHash has ${e.length} bytes; expected ${f}.`);
}
function J(e, t) {
  let r = 0;
  for (let a = 0; a < t; a++)
    for (let l = a ? 0 : 1; l * t < e * (t - a); l++) r++;
  return r;
}
var _e = Object.defineProperty, xe = Object.getOwnPropertyDescriptor, x = (e, t, r, a) => {
  for (var l = a > 1 ? void 0 : a ? xe(t, r) : t, c = e.length - 1, n; c >= 0; c--)
    (n = e[c]) && (l = (a ? n(t, r, l) : n(l)) || l);
  return a && l && _e(t, r, l), l;
};
let h = class extends ie {
  constructor() {
    super(...arguments), this.value = "", this.readonly = !1, this._decodeState = { status: "empty" }, this._copied = !1, this._blurred = !0, this._decodeRequest = 0;
  }
  willUpdate(e) {
    e.has("value") && this._decodeValue();
  }
  async _decodeValue() {
    const e = this.value.trim(), t = ++this._decodeRequest;
    if (this._copied = !1, this._blurred = !0, this._copyError = void 0, !e) {
      this._decodeState = { status: "empty" };
      return;
    }
    this._decodeState = { status: "pending", raw: e };
    try {
      const r = await ye(e), a = r.kind === "webp" ? r.dataUrl : Ee(r.rgba, r.width, r.height), l = r.kind === "webp" ? await Me(a) : { width: r.width, height: r.height };
      if (t !== this._decodeRequest) return;
      this._decodeState = {
        status: "ready",
        raw: e,
        label: r.label,
        preview: a,
        ...l
      };
    } catch (r) {
      if (t !== this._decodeRequest) return;
      this._decodeState = {
        status: "invalid",
        raw: e,
        error: r instanceof Error ? r.message : "The placeholder could not be decoded."
      };
    }
  }
  async _copyValue() {
    if (this.value)
      try {
        await navigator.clipboard.writeText(this.value), this._copied = !0;
      } catch {
        this._copyError = "Clipboard access was not granted.";
      }
  }
  _toggleBlur(e) {
    this._blurred = e.currentTarget.checked;
  }
  render() {
    const e = this._decodeState, t = e.status === "ready" ? e : void 0, r = t ? `${t.width} / ${t.height}` : "4 / 3";
    return _`
      <div class="editor">
        <div class="preview" style="aspect-ratio: ${r}">
          ${t ? _`<img
                class=${this._blurred ? "blurred" : W}
                src="${t.preview}"
                alt="Generated image placeholder"
              />` : _`<div class="empty">
                ${e.status === "empty" ? _`<em>Blur placeholder will be generated when the image is saved.</em>` : e.status === "pending" ? "Decoding preview…" : "No preview"}
              </div>`}
        </div>
        <div class="details">
          <div class="metadata">
            <span>${t?.label ?? (e.status === "invalid" ? "Malformed value" : e.status === "pending" ? "Loading" : "Empty")}</span>
            ${t ? _`<span>${t.width} × ${t.height}</span>` : W}
            <span>${this.value.length} characters</span>
          </div>
          <code title="${this.value}">${$e(this.value)}</code>
          <div class="actions">
            <uui-button look="secondary" label="Copy placeholder" @click=${this._copyValue} ?disabled=${!this.value}>
              ${this._copied ? "Copied" : "Copy"}
            </uui-button>
            <uui-toggle
              label="Blur preview"
              .checked=${this._blurred}
              ?disabled=${!t}
              @change=${this._toggleBlur}
            ></uui-toggle>
          </div>
          ${e.status === "invalid" ? _`<p class="error" role="alert">${e.error}</p>` : W}
          ${this._copyError ? _`<p class="error" role="alert">${this._copyError}</p>` : W}
        </div>
      </div>
    `;
  }
};
h.styles = se`
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
  ne("thebuilder-blur-placeholder-property-editor")
], h);
function $e(e, t = 96) {
  return e.length > t ? `${e.slice(0, t)}…` : e;
}
function Ee(e, t, r) {
  const a = document.createElement("canvas");
  a.width = t, a.height = r;
  const l = a.getContext("2d");
  if (!l) throw new Error("Canvas rendering is unavailable.");
  return l.putImageData(new ImageData(new Uint8ClampedArray(e), t, r), 0, 0), a.toDataURL("image/webp", 0.6);
}
function Me(e) {
  return new Promise((t, r) => {
    const a = new Image();
    a.onload = () => t({ width: a.naturalWidth, height: a.naturalHeight }), a.onerror = () => r(new Error("The WebP data URL could not be decoded.")), a.src = e;
  });
}
const Be = h;
export {
  h as TheBuilderBlurPlaceholderPropertyEditorElement,
  Be as default
};
//# sourceMappingURL=blur-placeholder.element-CZXVSTh6.js.map
