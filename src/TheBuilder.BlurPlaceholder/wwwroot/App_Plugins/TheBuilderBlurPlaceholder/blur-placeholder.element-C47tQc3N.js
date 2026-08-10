import { UmbLitElement as ne } from "@umbraco-cms/backoffice/lit-element";
import { nothing as G, html as P, css as ce, property as te, state as O, customElement as he } from "@umbraco-cms/backoffice/external/lit";
var de = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "#", "$", "%", "*", "+", ",", "-", ".", ":", ";", "=", "?", "@", "[", "]", "^", "_", "{", "|", "}", "~"], A = (e) => {
  let t = 0;
  for (let l = 0; l < e.length; l++) {
    let r = e[l], a = de.indexOf(r);
    t = t * 83 + a;
  }
  return t;
}, F = (e) => {
  let t = e / 255;
  return t <= 0.04045 ? t / 12.92 : Math.pow((t + 0.055) / 1.055, 2.4);
}, N = (e) => {
  let t = Math.max(0, Math.min(1, e));
  return t <= 31308e-7 ? Math.trunc(t * 12.92 * 255 + 0.5) : Math.trunc((1.055 * Math.pow(t, 0.4166666666666667) - 0.055) * 255 + 0.5);
}, ue = (e) => e < 0 ? -1 : 1, S = (e, t) => ue(e) * Math.pow(Math.abs(e), t), Q = class extends Error {
  constructor(e) {
    super(e), this.name = "ValidationError", this.message = e;
  }
}, pe = (e) => {
  if (!e || e.length < 6) throw new Q("The blurhash string must be at least 6 characters");
  let t = A(e[0]), l = Math.floor(t / 9) + 1, r = t % 9 + 1;
  if (e.length !== 4 + 2 * r * l) throw new Q(`blurhash length mismatch: length is ${e.length} but it should be ${4 + 2 * r * l}`);
}, fe = (e) => {
  let t = e >> 16, l = e >> 8 & 255, r = e & 255;
  return [F(t), F(l), F(r)];
}, ge = (e, t) => {
  let l = Math.floor(e / 361), r = Math.floor(e / 19) % 19, a = e % 19;
  return [S((l - 9) / 9, 2) * t, S((r - 9) / 9, 2) * t, S((a - 9) / 9, 2) * t];
}, me = (e, t, l, r) => {
  pe(e), r = r | 1;
  let a = A(e[0]), n = Math.floor(a / 9) + 1, c = a % 9 + 1, g = (A(e[1]) + 1) / 166, _ = new Array(c * n);
  for (let s = 0; s < _.length; s++) if (s === 0) {
    let o = A(e.substring(2, 6));
    _[s] = fe(o);
  } else {
    let o = A(e.substring(4 + s * 2, 6 + s * 2));
    _[s] = ge(o, g * r);
  }
  let m = t * 4, b = new Uint8ClampedArray(m * l);
  for (let s = 0; s < l; s++) for (let o = 0; o < t; o++) {
    let j = 0, I = 0, E = 0;
    for (let x = 0; x < n; x++) for (let $ = 0; $ < c; $++) {
      let M = Math.cos(Math.PI * o * $ / t) * Math.cos(Math.PI * s * x / l), f = _[$ + x * c];
      j += f[0] * M, I += f[1] * M, E += f[2] * M;
    }
    let k = N(j), v = N(I), q = N(E);
    b[4 * o + 0 + s * m] = k, b[4 * o + 1 + s * m] = v, b[4 * o + 2 + s * m] = q, b[4 * o + 3 + s * m] = 255;
  }
  return b;
}, be = me;
function ve(e) {
  let { PI: t, min: l, max: r, cos: a, round: n } = Math, c = e[0] | e[1] << 8 | e[2] << 16, g = e[3] | e[4] << 8, _ = (c & 63) / 63, m = (c >> 6 & 63) / 31.5 - 1, b = (c >> 12 & 63) / 31.5 - 1, s = (c >> 18 & 31) / 31, o = c >> 23, j = (g >> 3 & 63) / 63, I = (g >> 9 & 63) / 63, E = g >> 15, k = r(3, E ? o ? 5 : 7 : g & 7), v = r(3, E ? g & 7 : o ? 5 : 7), q = o ? (e[5] & 15) / 15 : 1, x = (e[5] >> 4) / 15, $ = o ? 6 : 5, M = 0, f = (z, p, C) => {
    let T = [];
    for (let y = 0; y < p; y++)
      for (let B = y ? 0 : 1; B * p < z * (p - y); B++)
        T.push(((e[$ + (M >> 1)] >> ((M++ & 1) << 2) & 15) / 7.5 - 1) * C);
    return T;
  }, le = f(k, v, s), ae = f(3, 3, j * 1.25), ie = f(3, 3, I * 1.25), oe = o && f(5, 5, x), L = ye(e), V = n(L > 1 ? 32 : 32 * L), D = n(L > 1 ? 32 / L : 32), H = new Uint8Array(V * D * 4), R = [], W = [];
  for (let z = 0, p = 0; z < D; z++)
    for (let C = 0; C < V; C++, p += 4) {
      let T = _, y = m, B = b, X = q;
      for (let i = 0, h = r(k, o ? 5 : 3); i < h; i++)
        R[i] = a(t / V * (C + 0.5) * i);
      for (let i = 0, h = r(v, o ? 5 : 3); i < h; i++)
        W[i] = a(t / D * (z + 0.5) * i);
      for (let i = 0, h = 0; i < v; i++)
        for (let d = i ? 0 : 1, U = W[i] * 2; d * v < k * (v - i); d++, h++)
          T += le[h] * R[d] * U;
      for (let i = 0, h = 0; i < 3; i++)
        for (let d = i ? 0 : 1, U = W[i] * 2; d < 3 - i; d++, h++) {
          let K = R[d] * U;
          y += ae[h] * K, B += ie[h] * K;
        }
      if (o)
        for (let i = 0, h = 0; i < 5; i++)
          for (let d = i ? 0 : 1, U = W[i] * 2; d < 5 - i; d++, h++)
            X += oe[h] * R[d] * U;
      let Y = T - 2 / 3 * y, J = (3 * T - Y + B) / 2, se = J - B;
      H[p] = r(0, 255 * l(1, J)), H[p + 1] = r(0, 255 * l(1, se)), H[p + 2] = r(0, 255 * l(1, Y)), H[p + 3] = r(0, 255 * l(1, X));
    }
  return { w: V, h: D, rgba: H };
}
function ye(e) {
  let t = e[3], l = e[2] & 128, r = e[4] & 128, a = r ? l ? 5 : 7 : t & 7, n = r ? t & 7 : l ? 5 : 7;
  return a / n;
}
var we = Object.defineProperty, _e = Object.getOwnPropertyDescriptor, w = (e, t, l, r) => {
  for (var a = r > 1 ? void 0 : r ? _e(t, l) : t, n = e.length - 1, c; n >= 0; n--)
    (c = e[n]) && (a = (r ? c(t, l, a) : c(a)) || a);
  return r && a && we(t, l, a), a;
};
let u = class extends ne {
  constructor() {
    super(...arguments), this.value = "", this.readonly = !1, this._copied = !1, this._blurred = !0;
  }
  willUpdate(e) {
    e.has("value") && this._decodeValue();
  }
  async _decodeValue() {
    const e = this.value.trim();
    if (this._copied = !1, this._blurred = !0, this._error = void 0, !e) {
      this._info = void 0;
      return;
    }
    try {
      const t = await $e(e);
      if (this.value.trim() !== e) return;
      this._info = t.info, this._error = t.error;
    } catch (t) {
      if (this.value.trim() !== e) return;
      this._info = { kind: "unknown", label: "Malformed value", raw: e }, this._error = t instanceof Error ? t.message : "The placeholder could not be decoded.";
    }
  }
  async _copyValue() {
    if (this.value)
      try {
        await navigator.clipboard.writeText(this.value), this._copied = !0;
      } catch {
        this._error = "Clipboard access was not granted.";
      }
  }
  _toggleBlur(e) {
    this._blurred = e.currentTarget.checked;
  }
  render() {
    const e = this._info, t = e?.width && e.height ? `${e.width} / ${e.height}` : "4 / 3";
    return P`
      <div class="editor">
        <div class="preview" style="aspect-ratio: ${t}">
          ${e?.preview ? P`<img
                class=${this._blurred ? "blurred" : G}
                src="${e.preview}"
                alt="Generated image placeholder"
              />` : P`<div class="empty">
                ${this.value ? "No preview" : P`<em>Blur placeholder will be generated when the image is saved.</em>`}
              </div>`}
        </div>
        <div class="details">
          <div class="metadata">
            <span>${e?.label ?? "Empty"}</span>
            ${e?.width && e?.height ? P`<span>${e.width} × ${e.height}</span>` : G}
            <span>${this.value.length} characters</span>
          </div>
          <code title="${this.value}">${xe(this.value)}</code>
          <div class="actions">
            <uui-button look="secondary" label="Copy placeholder" @click=${this._copyValue} ?disabled=${!this.value}>
              ${this._copied ? "Copied" : "Copy"}
            </uui-button>
            <uui-toggle
              label="Blur preview"
              .checked=${this._blurred}
              ?disabled=${!e?.preview}
              @change=${this._toggleBlur}
            ></uui-toggle>
          </div>
          ${this._error ? P`<p class="error" role="alert">${this._error}</p>` : G}
        </div>
      </div>
    `;
  }
};
u.styles = ce`
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
w([
  te({ type: String })
], u.prototype, "value", 2);
w([
  te({ type: Boolean, reflect: !0 })
], u.prototype, "readonly", 2);
w([
  O()
], u.prototype, "_info", 2);
w([
  O()
], u.prototype, "_error", 2);
w([
  O()
], u.prototype, "_copied", 2);
w([
  O()
], u.prototype, "_blurred", 2);
u = w([
  he("thebuilder-blur-placeholder-property-editor")
], u);
function xe(e, t = 96) {
  return e.length > t ? `${e.slice(0, t)}…` : e;
}
async function $e(e) {
  if (e.startsWith("data:image/webp;base64,")) {
    const { width: t, height: l } = await Be(e);
    return {
      info: { kind: "webp", label: "WebP data URL", raw: e, preview: e, width: t, height: l }
    };
  }
  if (e.startsWith("blurhash:"))
    return Z(e.slice(9), e);
  if (e.startsWith("thumbhash:"))
    return ee(e.slice(10), e);
  if (Me(e))
    return Z(e, e, "BlurHash (unprefixed)");
  try {
    return ee(e, e, "ThumbHash (unprefixed)");
  } catch {
    return {
      info: { kind: "unknown", label: "Unknown value", raw: e },
      error: "The value is not a recognized WebP, BlurHash, or ThumbHash placeholder."
    };
  }
}
function Z(e, t, l = "BlurHash") {
  const n = be(e, 32, 24);
  return {
    info: {
      kind: "blurhash",
      label: l,
      raw: t,
      preview: re(n, 32, 24),
      width: 32,
      height: 24
    }
  };
}
function ee(e, t, l = "ThumbHash") {
  const r = Te(e);
  if (r.length < 5) throw new Error("ThumbHash is too short.");
  const a = ve(r);
  return {
    info: {
      kind: "thumbhash",
      label: l,
      raw: t,
      preview: re(a.rgba, a.w, a.h),
      width: a.w,
      height: a.h
    }
  };
}
function Me(e) {
  const t = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$%*+,-.:;=?@[]^_{|}~";
  if (e.length < 6 || [...e].some((n) => !t.includes(n))) return !1;
  const l = t.indexOf(e[0]), r = l % 9 + 1, a = Math.floor(l / 9) + 1;
  return e.length === 4 + 2 * r * a;
}
function Te(e) {
  const t = atob(e);
  return Uint8Array.from(t, (l) => l.charCodeAt(0));
}
function re(e, t, l) {
  const r = document.createElement("canvas");
  r.width = t, r.height = l;
  const a = r.getContext("2d");
  if (!a) throw new Error("Canvas rendering is unavailable.");
  return a.putImageData(new ImageData(new Uint8ClampedArray(e), t, l), 0, 0), r.toDataURL("image/webp", 0.6);
}
function Be(e) {
  return new Promise((t, l) => {
    const r = new Image();
    r.onload = () => t({ width: r.naturalWidth, height: r.naturalHeight }), r.onerror = () => l(new Error("The WebP data URL could not be decoded.")), r.src = e;
  });
}
const ke = u;
export {
  u as TheBuilderBlurPlaceholderPropertyEditorElement,
  ke as default
};
//# sourceMappingURL=blur-placeholder.element-C47tQc3N.js.map
