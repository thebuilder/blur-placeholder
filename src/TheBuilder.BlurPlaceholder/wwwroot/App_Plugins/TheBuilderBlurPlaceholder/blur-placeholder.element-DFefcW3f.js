import { UmbLitElement as e } from "@umbraco-cms/backoffice/lit-element";
import { css as t, customElement as n, html as r, nothing as i, property as a, state as o } from "@umbraco-cms/backoffice/external/lit";
//#region ../../../node_modules/.pnpm/blurhash@2.0.5/node_modules/blurhash/dist/esm/index.js
var s = /* @__PURE__ */ "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$%*+,-.:;=?@[]^_{|}~".split(""), c = (e) => {
	let t = 0;
	for (let n = 0; n < e.length; n++) {
		let r = e[n], i = s.indexOf(r);
		t = t * 83 + i;
	}
	return t;
}, l = (e) => {
	let t = e / 255;
	return t <= .04045 ? t / 12.92 : ((t + .055) / 1.055) ** 2.4;
}, u = (e) => {
	let t = Math.max(0, Math.min(1, e));
	return Math.trunc(t <= .0031308 ? t * 12.92 * 255 + .5 : (1.055 * t ** .4166666666666667 - .055) * 255 + .5);
}, d = (e) => e < 0 ? -1 : 1, f = (e, t) => d(e) * Math.abs(e) ** +t, p = class extends Error {
	constructor(e) {
		super(e), this.name = "ValidationError", this.message = e;
	}
}, m = (e) => {
	if (!e || e.length < 6) throw new p("The blurhash string must be at least 6 characters");
	let t = c(e[0]), n = Math.floor(t / 9) + 1, r = t % 9 + 1;
	if (e.length !== 4 + 2 * r * n) throw new p(`blurhash length mismatch: length is ${e.length} but it should be ${4 + 2 * r * n}`);
}, h = (e) => {
	let t = e >> 16, n = e >> 8 & 255, r = e & 255;
	return [
		l(t),
		l(n),
		l(r)
	];
}, g = (e, t) => {
	let n = Math.floor(e / 361), r = Math.floor(e / 19) % 19, i = e % 19;
	return [
		f((n - 9) / 9, 2) * t,
		f((r - 9) / 9, 2) * t,
		f((i - 9) / 9, 2) * t
	];
}, _ = (e, t, n, r) => {
	m(e), r |= 1;
	let i = c(e[0]), a = Math.floor(i / 9) + 1, o = i % 9 + 1, s = (c(e[1]) + 1) / 166, l = Array(o * a);
	for (let t = 0; t < l.length; t++) if (t === 0) {
		let n = c(e.substring(2, 6));
		l[t] = h(n);
	} else {
		let n = c(e.substring(4 + t * 2, 6 + t * 2));
		l[t] = g(n, s * r);
	}
	let d = t * 4, f = new Uint8ClampedArray(d * n);
	for (let e = 0; e < n; e++) for (let r = 0; r < t; r++) {
		let i = 0, s = 0, c = 0;
		for (let u = 0; u < a; u++) for (let a = 0; a < o; a++) {
			let d = Math.cos(Math.PI * r * a / t) * Math.cos(Math.PI * e * u / n), f = l[a + u * o];
			i += f[0] * d, s += f[1] * d, c += f[2] * d;
		}
		let p = u(i), m = u(s), h = u(c);
		f[4 * r + 0 + e * d] = p, f[4 * r + 1 + e * d] = m, f[4 * r + 2 + e * d] = h, f[4 * r + 3 + e * d] = 255;
	}
	return f;
};
//#endregion
//#region ../../../node_modules/.pnpm/thumbhash@0.1.1/node_modules/thumbhash/thumbhash.js
function v(e) {
	let { PI: t, min: n, max: r, cos: i, round: a } = Math, o = e[0] | e[1] << 8 | e[2] << 16, s = e[3] | e[4] << 8, c = (o & 63) / 63, l = (o >> 6 & 63) / 31.5 - 1, u = (o >> 12 & 63) / 31.5 - 1, d = (o >> 18 & 31) / 31, f = o >> 23, p = (s >> 3 & 63) / 63, m = (s >> 9 & 63) / 63, h = s >> 15, g = r(3, h ? f ? 5 : 7 : s & 7), _ = r(3, h ? s & 7 : f ? 5 : 7), v = f ? (e[5] & 15) / 15 : 1, b = (e[5] >> 4) / 15, x = f ? 6 : 5, S = 0, C = (t, n, r) => {
		let i = [];
		for (let a = 0; a < n; a++) for (let o = +!a; o * n < t * (n - a); o++) i.push(((e[x + (S >> 1)] >> ((S++ & 1) << 2) & 15) / 7.5 - 1) * r);
		return i;
	}, w = C(g, _, d), T = C(3, 3, p * 1.25), E = C(3, 3, m * 1.25), D = f && C(5, 5, b), O = y(e), k = a(O > 1 ? 32 : 32 * O), A = a(O > 1 ? 32 / O : 32), j = new Uint8Array(k * A * 4), M = [], N = [];
	for (let e = 0, a = 0; e < A; e++) for (let o = 0; o < k; o++, a += 4) {
		let s = c, d = l, p = u, m = v;
		for (let e = 0, n = r(g, f ? 5 : 3); e < n; e++) M[e] = i(t / k * (o + .5) * e);
		for (let n = 0, a = r(_, f ? 5 : 3); n < a; n++) N[n] = i(t / A * (e + .5) * n);
		for (let e = 0, t = 0; e < _; e++) for (let n = +!e, r = N[e] * 2; n * _ < g * (_ - e); n++, t++) s += w[t] * M[n] * r;
		for (let e = 0, t = 0; e < 3; e++) for (let n = +!e, r = N[e] * 2; n < 3 - e; n++, t++) {
			let e = M[n] * r;
			d += T[t] * e, p += E[t] * e;
		}
		if (f) for (let e = 0, t = 0; e < 5; e++) for (let n = +!e, r = N[e] * 2; n < 5 - e; n++, t++) m += D[t] * M[n] * r;
		let h = s - 2 / 3 * d, y = (3 * s - h + p) / 2, b = y - p;
		j[a] = r(0, 255 * n(1, y)), j[a + 1] = r(0, 255 * n(1, b)), j[a + 2] = r(0, 255 * n(1, h)), j[a + 3] = r(0, 255 * n(1, m));
	}
	return {
		w: k,
		h: A,
		rgba: j
	};
}
function y(e) {
	let t = e[3], n = e[2] & 128, r = e[4] & 128;
	return (r ? n ? 5 : 7 : t & 7) / (r ? t & 7 : n ? 5 : 7);
}
//#endregion
//#region src/property-editor/decode-placeholder.ts
function b(e) {
	if (e.startsWith("data:image/webp;base64,")) return {
		kind: "webp",
		label: "WebP data URL",
		dataUrl: e
	};
	if (e.startsWith("blurhash:")) return {
		kind: "blurhash",
		label: "BlurHash",
		rgba: _(e.slice(9), 32, 24),
		width: 32,
		height: 24
	};
	if (e.startsWith("thumbhash:")) {
		let t = x(e.slice(10));
		S(t);
		let n = v(t);
		return {
			kind: "thumbhash",
			label: "ThumbHash",
			rgba: n.rgba,
			width: n.w,
			height: n.h
		};
	}
	throw Error("The value is not a prefixed WebP, BlurHash, or ThumbHash placeholder.");
}
function x(e) {
	let t = atob(e);
	return Uint8Array.from(t, (e) => e.charCodeAt(0));
}
function S(e) {
	if (e.length < 5) throw Error("ThumbHash is too short.");
	let t = e[0] | e[1] << 8 | e[2] << 16, n = e[3] | e[4] << 8, r = t >> 23 == 1, i = n >> 15 == 1, a = C(Math.max(3, i ? r ? 5 : 7 : n & 7), Math.max(3, i ? n & 7 : r ? 5 : 7)) + 2 * C(3, 3) + (r ? C(5, 5) : 0), o = (r ? 6 : 5) + Math.ceil(a / 2);
	if (e.length !== o) throw Error(`ThumbHash has ${e.length} bytes; expected ${o}.`);
}
function C(e, t) {
	let n = 0;
	for (let r = 0; r < t; r++) for (let i = +!r; i * t < e * (t - r); i++) n++;
	return n;
}
//#endregion
//#region \0@oxc-project+runtime@0.149.0/helpers/esm/decorate.js
function w(e, t, n, r) {
	var i = arguments.length, a = i < 3 ? t : r === null ? r = Object.getOwnPropertyDescriptor(t, n) : r, o;
	if (typeof Reflect == "object" && typeof Reflect.decorate == "function") a = Reflect.decorate(e, t, n, r);
	else for (var s = e.length - 1; s >= 0; s--) (o = e[s]) && (a = (i < 3 ? o(a) : i > 3 ? o(t, n, a) : o(t, n)) || a);
	return i > 3 && a && Object.defineProperty(t, n, a), a;
}
//#endregion
//#region src/property-editor/blur-placeholder.element.ts
var T = class extends e {
	constructor(...e) {
		super(...e), this.value = "", this.readonly = !1, this._decodeState = { status: "empty" }, this._copied = !1, this._blurred = !0, this._decodeRequest = 0;
	}
	willUpdate(e) {
		e.has("value") && this._decodeValue();
	}
	async _decodeValue() {
		let e = (this.value ?? "").trim(), t = ++this._decodeRequest;
		if (this._copied = !1, this._blurred = !0, this._copyError = void 0, !e) {
			this._decodeState = { status: "empty" };
			return;
		}
		this._decodeState = {
			status: "pending",
			raw: e
		};
		try {
			let n = await b(e), r = n.kind === "webp" ? n.dataUrl : D(n.rgba, n.width, n.height), i = n.kind === "webp" ? await O(r) : {
				width: n.width,
				height: n.height
			};
			if (t !== this._decodeRequest) return;
			this._decodeState = {
				status: "ready",
				raw: e,
				label: n.label,
				preview: r,
				...i
			};
		} catch (n) {
			if (t !== this._decodeRequest) return;
			this._decodeState = {
				status: "invalid",
				raw: e,
				error: n instanceof Error ? n.message : "The placeholder could not be decoded."
			};
		}
	}
	async _copyValue() {
		let e = this.value ?? "";
		if (e) try {
			await navigator.clipboard.writeText(e), this._copied = !0;
		} catch {
			this._copyError = "Clipboard access was not granted.";
		}
	}
	_toggleBlur() {
		this._blurred = !this._blurred;
	}
	render() {
		let e = this.value ?? "", t = this._decodeState, n = t.status === "ready" ? t : void 0, a = n ? `${n.width} / ${n.height}` : "4 / 3";
		return r`
      <div class="editor">
        <button
          class="preview"
          type="button"
          style="aspect-ratio: ${a}"
          aria-label=${this._blurred ? "Show preview without CSS blur" : "Show preview with CSS blur"}
          aria-pressed=${this._blurred}
          title=${n ? "Toggle CSS blur preview" : i}
          ?disabled=${!n}
          @click=${this._toggleBlur}
        >
          ${n ? r`<img
                class=${this._blurred ? "blurred" : i}
                src="${n.preview}"
                alt="Generated image placeholder"
              />` : r`<div class="empty">
                ${t.status === "empty" ? r`<em>Blur placeholder will be generated when the image is saved.</em>` : t.status === "pending" ? "Decoding preview…" : "No preview"}
              </div>`}
        </button>
        ${t.status === "empty" ? i : r`<div class="details">
              <div class="metadata">
                <span>${n?.label ?? (t.status === "invalid" ? "Malformed value" : "Loading")}</span>
                ${n ? r`<span>${n.width} × ${n.height}</span>` : i}
                <span>${e.length} characters</span>
              </div>
              <div class="value">
                <code title="${e}">${E(e)}</code>
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
              ${t.status === "invalid" ? r`<p class="error" role="alert">${t.error}</p>` : i}
              ${this._copyError ? r`<p class="error" role="alert">${this._copyError}</p>` : i}
            </div>`}
      </div>
    `;
	}
	static {
		this.styles = t`
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
	}
};
w([a({ type: String })], T.prototype, "value", void 0), w([a({
	type: Boolean,
	reflect: !0
})], T.prototype, "readonly", void 0), w([o()], T.prototype, "_decodeState", void 0), w([o()], T.prototype, "_copyError", void 0), w([o()], T.prototype, "_copied", void 0), w([o()], T.prototype, "_blurred", void 0), T = w([n("thebuilder-blur-placeholder-property-editor")], T);
function E(e, t = 96) {
	return e.length > t ? `${e.slice(0, t)}…` : e;
}
function D(e, t, n) {
	let r = document.createElement("canvas");
	r.width = t, r.height = n;
	let i = r.getContext("2d");
	if (!i) throw Error("Canvas rendering is unavailable.");
	return i.putImageData(new ImageData(new Uint8ClampedArray(e), t, n), 0, 0), r.toDataURL("image/webp", .6);
}
function O(e) {
	return new Promise((t, n) => {
		let r = new Image();
		r.onload = () => t({
			width: r.naturalWidth,
			height: r.naturalHeight
		}), r.onerror = () => n(/* @__PURE__ */ Error("The WebP data URL could not be decoded.")), r.src = e;
	});
}
var k = T;
//#endregion
export { T as TheBuilderBlurPlaceholderPropertyEditorElement, k as default };

//# sourceMappingURL=blur-placeholder.element-DFefcW3f.js.map