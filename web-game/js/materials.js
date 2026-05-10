// Procedural PBR material factory — no external textures needed.
// All maps are generated on <canvas> with layered value-noise, then fed as
// albedo/normal/roughness/metalness/AO to THREE.MeshStandardMaterial.

import * as THREE from 'three';

// ---------- noise helpers ----------
function hash(x, y, s = 0) {
  const n = Math.sin(x * 127.1 + y * 311.7 + s * 74.7) * 43758.5453;
  return n - Math.floor(n);
}
function smooth(t) { return t * t * (3 - 2 * t); }
function vnoise(x, y, s = 0) {
  const xi = Math.floor(x), yi = Math.floor(y);
  const xf = x - xi, yf = y - yi;
  const a = hash(xi, yi, s), b = hash(xi + 1, yi, s);
  const c = hash(xi, yi + 1, s), d = hash(xi + 1, yi + 1, s);
  const u = smooth(xf), v = smooth(yf);
  return a * (1 - u) * (1 - v) + b * u * (1 - v) + c * (1 - u) * v + d * u * v;
}
function fbm(x, y, oct = 5, s = 0) {
  let amp = 0.5, freq = 1, sum = 0, norm = 0;
  for (let i = 0; i < oct; i++) {
    sum += amp * vnoise(x * freq, y * freq, s + i);
    norm += amp; amp *= 0.5; freq *= 2;
  }
  return sum / norm;
}

function makeCanvas(size) {
  const c = document.createElement('canvas');
  c.width = c.height = size;
  return c;
}

// normal map generated from a height function
function heightToNormal(ctx, size, heightFn, strength = 2) {
  const img = ctx.createImageData(size, size);
  const d = img.data;
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const hL = heightFn((x - 1 + size) % size, y);
      const hR = heightFn((x + 1) % size, y);
      const hD = heightFn(x, (y - 1 + size) % size);
      const hU = heightFn(x, (y + 1) % size);
      let nx = (hL - hR) * strength;
      let ny = (hD - hU) * strength;
      let nz = 1;
      const len = Math.hypot(nx, ny, nz);
      nx /= len; ny /= len; nz /= len;
      const i = (y * size + x) * 4;
      d[i] = (nx * 0.5 + 0.5) * 255;
      d[i + 1] = (ny * 0.5 + 0.5) * 255;
      d[i + 2] = (nz * 0.5 + 0.5) * 255;
      d[i + 3] = 255;
    }
  }
  ctx.putImageData(img, 0, 0);
}

function makeTex(canvas, repeat = 4) {
  const t = new THREE.CanvasTexture(canvas);
  t.wrapS = t.wrapT = THREE.RepeatWrapping;
  t.repeat.set(repeat, repeat);
  t.anisotropy = 8;
  return t;
}

// ---------- generators ----------
function genHeight(size, fn) {
  const c = makeCanvas(size);
  const ctx = c.getContext('2d');
  const img = ctx.createImageData(size, size);
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const v = Math.max(0, Math.min(1, fn(x, y))) * 255;
    const i = (y * size + x) * 4;
    img.data[i] = img.data[i + 1] = img.data[i + 2] = v; img.data[i + 3] = 255;
  }
  ctx.putImageData(img, 0, 0);
  return c;
}
function genColor(size, fn) {
  const c = makeCanvas(size);
  const ctx = c.getContext('2d');
  const img = ctx.createImageData(size, size);
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const [r, g, b] = fn(x, y);
    const i = (y * size + x) * 4;
    img.data[i] = r; img.data[i + 1] = g; img.data[i + 2] = b; img.data[i + 3] = 255;
  }
  ctx.putImageData(img, 0, 0);
  return c;
}
function genNormal(size, heightFn, strength) {
  const c = makeCanvas(size);
  heightToNormal(c.getContext('2d'), size, heightFn, strength);
  return c;
}

// -------- material presets --------
const S = 256;   // texture res (fast, reusable)

function rustyMetal(repeat = 3) {
  const hFn = (x, y) => fbm(x / 14, y / 14, 6, 1) * 0.7 + fbm(x / 3, y / 3, 4, 2) * 0.3;
  const color = genColor(S, (x, y) => {
    const n = fbm(x / 22, y / 22, 5, 3);
    const rust = fbm(x / 10, y / 10, 5, 4);
    const r = rust > 0.55;
    const base = [70 + n * 40, 78 + n * 40, 86 + n * 40];
    const rustC = [110 + n * 50, 55 + n * 25, 22 + n * 10];
    return r ? rustC : base;
  });
  const rough = genHeight(S, (x, y) => 0.55 + fbm(x / 8, y / 8, 4, 5) * 0.4);
  const normal = genNormal(S, hFn, 3);
  const m = new THREE.MeshStandardMaterial({
    map: makeTex(color, repeat),
    normalMap: makeTex(normal, repeat),
    roughnessMap: makeTex(rough, repeat),
    metalness: 0.85, roughness: 0.7,
    color: 0x8a8a8a,
  });
  return m;
}

function concrete(repeat = 4, tint = 0x6e6c67) {
  const hFn = (x, y) => fbm(x / 20, y / 20, 5, 6) * 0.6 + fbm(x / 4, y / 4, 3, 7) * 0.4;
  const color = genColor(S, (x, y) => {
    const n = fbm(x / 24, y / 24, 5, 8);
    const crack = fbm(x / 6, y / 6, 3, 9) < 0.18 ? -30 : 0;
    const v = 110 + n * 45 + crack;
    return [v, v * 0.98, v * 0.95];
  });
  const rough = genHeight(S, (x, y) => 0.75 + fbm(x / 10, y / 10, 4, 10) * 0.2);
  const normal = genNormal(S, hFn, 2.2);
  return new THREE.MeshStandardMaterial({
    map: makeTex(color, repeat),
    normalMap: makeTex(normal, repeat),
    roughnessMap: makeTex(rough, repeat),
    metalness: 0.05, roughness: 0.95, color: tint,
  });
}

function wood(repeat = 3) {
  const hFn = (x, y) => {
    const ring = Math.abs(Math.sin(x / 4 + fbm(x / 30, y / 30, 4, 11) * 6)) * 0.6;
    return ring + fbm(x / 6, y / 6, 3, 12) * 0.4;
  };
  const color = genColor(S, (x, y) => {
    const ring = Math.abs(Math.sin(x / 4 + fbm(x / 30, y / 30, 4, 11) * 6));
    const n = fbm(x / 10, y / 10, 4, 13);
    const r = 80 + ring * 60 + n * 25;
    return [r, r * 0.6, r * 0.3];
  });
  const rough = genHeight(S, () => 0.8);
  const normal = genNormal(S, hFn, 1.2);
  return new THREE.MeshStandardMaterial({
    map: makeTex(color, repeat),
    normalMap: makeTex(normal, repeat),
    roughnessMap: makeTex(rough, repeat),
    metalness: 0.0, roughness: 0.9,
  });
}

function dirtAsphalt(repeat = 6) {
  const hFn = (x, y) => fbm(x / 3, y / 3, 6, 14);
  const color = genColor(S, (x, y) => {
    const n = fbm(x / 12, y / 12, 5, 15);
    const mud = fbm(x / 8, y / 8, 3, 16);
    const base = 40 + n * 30;
    const m = mud > 0.55 ? 20 : 0;
    return [base + m, base * 0.95 + m * 0.7, base * 0.85 + m * 0.5];
  });
  const rough = genHeight(S, () => 0.95);
  const normal = genNormal(S, hFn, 2);
  return new THREE.MeshStandardMaterial({
    map: makeTex(color, repeat),
    normalMap: makeTex(normal, repeat),
    roughnessMap: makeTex(rough, repeat),
    metalness: 0.02, roughness: 1,
  });
}

function boneMaterial() {
  const hFn = (x, y) => fbm(x / 8, y / 8, 5, 17) * 0.7 + fbm(x / 2, y / 2, 3, 18) * 0.3;
  const color = genColor(S, (x, y) => {
    const n = fbm(x / 14, y / 14, 4, 19);
    const grime = fbm(x / 6, y / 6, 3, 20);
    const crack = fbm(x / 3, y / 3, 2, 21) < 0.18 ? -25 : 0;
    const r = 205 + n * 30 - grime * 40 + crack;
    const g = 190 + n * 25 - grime * 50 + crack;
    const b = 160 + n * 20 - grime * 55 + crack;
    return [Math.max(80, r), Math.max(70, g), Math.max(60, b)];
  });
  const rough = genHeight(S, (x, y) => 0.75 + fbm(x / 6, y / 6, 4, 22) * 0.2);
  const normal = genNormal(S, hFn, 1.6);
  return new THREE.MeshStandardMaterial({
    map: makeTex(color, 1),
    normalMap: makeTex(normal, 1),
    roughnessMap: makeTex(rough, 1),
    metalness: 0, roughness: 0.85,
  });
}

function yellowTruckPaint() {
  const c = genColor(S, (x, y) => {
    const n = fbm(x / 14, y / 14, 4, 23);
    const scratch = fbm(x / 3, y / 3, 2, 24) < 0.15 ? -60 : 0;
    const rust = fbm(x / 20, y / 20, 4, 25) > 0.62 ? -80 : 0;
    return [210 + n * 20 + scratch + rust * 0.4, 165 + n * 15 + scratch + rust * 0.2, 40 + n * 15 + scratch + rust];
  });
  const hFn = (x, y) => fbm(x / 6, y / 6, 4, 26);
  const n = genNormal(S, hFn, 1.2);
  return new THREE.MeshStandardMaterial({
    map: makeTex(c, 1), normalMap: makeTex(n, 1),
    metalness: 0.5, roughness: 0.55, color: 0xffffff,
  });
}

function glowAmber() {
  return new THREE.MeshStandardMaterial({
    color: 0xffcc66, emissive: 0xd9a441, emissiveIntensity: 2.5,
    metalness: 0, roughness: 0.4,
  });
}

function crtScreen() {
  const c = makeCanvas(256);
  const ctx = c.getContext('2d');
  ctx.fillStyle = '#0a0e0a'; ctx.fillRect(0, 0, 256, 256);
  // scanlines
  ctx.fillStyle = 'rgba(0,0,0,0.25)';
  for (let y = 0; y < 256; y += 2) ctx.fillRect(0, y, 256, 1);
  // noise
  const img = ctx.getImageData(0, 0, 256, 256); const d = img.data;
  for (let i = 0; i < d.length; i += 4) {
    const n = (Math.random() - 0.5) * 40;
    d[i] = Math.max(0, d[i] + n);
    d[i + 1] = Math.max(0, d[i + 1] + n * 1.6);
    d[i + 2] = Math.max(0, d[i + 2] + n);
  }
  ctx.putImageData(img, 0, 0);
  const t = new THREE.CanvasTexture(c);
  return new THREE.MeshStandardMaterial({ map: t, emissive: 0x114422, emissiveMap: t, emissiveIntensity: 1.8, roughness: 0.3, metalness: 0.1 });
}

function brick(repeat = 4) {
  const hFn = (x, y) => {
    const bw = 32, bh = 12;
    const row = Math.floor(y / bh);
    const xo = (row % 2) * bw / 2;
    const ix = ((x + xo) % bw), iy = y % bh;
    const mortar = (ix < 2 || ix > bw - 2 || iy < 2 || iy > bh - 2) ? 0 : 1;
    return mortar * 0.8 + fbm(x / 4, y / 4, 3, 27) * 0.2;
  };
  const color = genColor(S, (x, y) => {
    const bw = 32, bh = 12;
    const row = Math.floor(y / bh);
    const xo = (row % 2) * bw / 2;
    const ix = ((x + xo) % bw), iy = y % bh;
    const mortar = (ix < 2 || ix > bw - 2 || iy < 2 || iy > bh - 2);
    const n = fbm(x / 20, y / 20, 4, 28);
    if (mortar) return [70 + n * 20, 68 + n * 20, 62 + n * 18];
    return [110 + n * 40, 60 + n * 25, 45 + n * 20];
  });
  const rough = genHeight(S, () => 0.92);
  const normal = genNormal(S, hFn, 2.5);
  return new THREE.MeshStandardMaterial({
    map: makeTex(color, repeat),
    normalMap: makeTex(normal, repeat),
    roughnessMap: makeTex(rough, repeat),
    metalness: 0, roughness: 1,
  });
}

function glass() {
  return new THREE.MeshStandardMaterial({
    color: 0x223033, metalness: 0.2, roughness: 0.05, transparent: true, opacity: 0.35,
  });
}

// cached singletons
let _cache = null;
export function materials() {
  if (_cache) return _cache;
  _cache = {
    rustyMetal: rustyMetal(3),
    rustyMetalFine: rustyMetal(6),
    concrete: concrete(4),
    concreteFloor: concrete(8, 0x5c5a55),
    wall: brick(3),
    wood: wood(3),
    asphalt: dirtAsphalt(8),
    bone: boneMaterial(),
    truck: yellowTruckPaint(),
    amber: glowAmber(),
    crt: crtScreen(),
    glass: glass(),
    dark: new THREE.MeshStandardMaterial({ color: 0x15181c, roughness: 0.9, metalness: 0.1 }),
    rubber: new THREE.MeshStandardMaterial({ color: 0x1a1a1e, roughness: 0.95, metalness: 0 }),
    lampOff: new THREE.MeshStandardMaterial({ color: 0x222222, roughness: 0.3, metalness: 0.7 }),
    lampOn: new THREE.MeshStandardMaterial({ color: 0xffeecc, emissive: 0xffddaa, emissiveIntensity: 3 }),
    blood: new THREE.MeshStandardMaterial({ color: 0x4a0e0e, roughness: 0.6, metalness: 0, transparent: true, opacity: 0.8 }),
    cloth: new THREE.MeshStandardMaterial({ color: 0x2a241d, roughness: 1, metalness: 0, side: THREE.DoubleSide }),
  };
  return _cache;
}
