import * as THREE from 'three';

export const V3 = (x = 0, y = 0, z = 0) => new THREE.Vector3(x, y, z);
export const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
export const lerp = (a, b, t) => a + (b - a) * t;
export const rand = (a, b) => a + Math.random() * (b - a);
export const randi = (a, b) => Math.floor(rand(a, b + 1));
export const pick = (arr) => arr[Math.floor(Math.random() * arr.length)];

// axis-aligned box collider stored as {min:Vector3,max:Vector3,tag?}
export function makeAABB(cx, cz, sx, sz, y = 0, h = 3, tag = 'wall') {
  return {
    min: V3(cx - sx / 2, y, cz - sz / 2),
    max: V3(cx + sx / 2, y + h, cz + sz / 2),
    tag,
  };
}
export function aabbContains(a, p) {
  return p.x >= a.min.x && p.x <= a.max.x && p.z >= a.min.z && p.z <= a.max.z && p.y >= a.min.y && p.y <= a.max.y;
}
export function aabbDistXZ(a, p) {
  const cx = (a.min.x + a.max.x) / 2, cz = (a.min.z + a.max.z) / 2;
  return Math.hypot(p.x - cx, p.z - cz);
}

// resolve a 2D (XZ) point vs a list of AABB walls, giving a new position
// that does not penetrate any wall. Simple push-out.
export function resolveXZ(pos, radius, walls) {
  for (const w of walls) {
    if (w.tag === 'trigger' || w.tag === 'floor') continue;
    const minX = w.min.x - radius, maxX = w.max.x + radius;
    const minZ = w.min.z - radius, maxZ = w.max.z + radius;
    if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ) continue;
    // vertical skip: if the wall is above player head or below feet
    if (w.max.y < pos.y - 1.0 || w.min.y > pos.y + 1.2) continue;
    // push along shortest axis
    const dl = pos.x - minX, dr = maxX - pos.x;
    const dt = pos.z - minZ, db = maxZ - pos.z;
    const m = Math.min(dl, dr, dt, db);
    if (m === dl) pos.x = minX;
    else if (m === dr) pos.x = maxX;
    else if (m === dt) pos.z = minZ;
    else pos.z = maxZ;
  }
  return pos;
}

// quick LOS via stepped raycast against list of walls (XZ only)
export function hasLOS(from, to, walls, step = 0.5) {
  const dx = to.x - from.x, dz = to.z - from.z;
  const d = Math.hypot(dx, dz);
  const n = Math.max(1, Math.floor(d / step));
  for (let i = 1; i < n; i++) {
    const t = i / n;
    const p = { x: from.x + dx * t, y: from.y, z: from.z + dz * t };
    for (const w of walls) {
      if (w.tag === 'trigger' || w.tag === 'floor') continue;
      if (w.max.y < p.y - 0.2 || w.min.y > p.y + 1.4) continue;
      if (p.x >= w.min.x && p.x <= w.max.x && p.z >= w.min.z && p.z <= w.max.z) return false;
    }
  }
  return true;
}

// directional angle from A to B (yaw, radians, 0 = +Z)
export function yawTo(from, to) {
  return Math.atan2(to.x - from.x, to.z - from.z);
}

// pseudo seed
export function seeded(seed) {
  let s = seed | 0 || 1;
  return () => {
    s = (s * 1664525 + 1013904223) | 0;
    return ((s >>> 0) % 100000) / 100000;
  };
}
