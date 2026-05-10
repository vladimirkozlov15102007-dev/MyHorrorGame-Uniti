import * as THREE from 'three';
import { V3, lerp, clamp } from './util.js';
import { materials } from './materials.js';
import { audio } from './audio.js';

// A procedural pistol held in the camera's local space.
// Raycast firing against enemies (by their hitboxes) + world AABB walls.

export class Pistol {
  constructor(camera, scene) {
    this.cam = camera;
    this.scene = scene;
    this.ammoInMag = 7;
    this.magSize = 7;
    this.reserve = 35;
    this.reloading = false;
    this.reloadT = 0;

    this.cooldown = 0;
    this.fireRate = 0.18;         // sec between shots
    this.recoilY = 0;
    this.swayX = 0;
    this.swayY = 0;
    this.aim = 0;                 // 0 hip, 1 aimed

    this.muzzleLight = new THREE.PointLight(0xffc066, 0, 6, 2);
    this.muzzleLight.position.set(0, 0, 0);
    scene.add(this.muzzleLight);

    this.shells = [];             // { mesh, vel, life }
    this.sparks = [];             // ricochet sparks

    this._buildModel();
  }

  _buildModel() {
    const mats = materials();
    const g = new THREE.Group();

    // frame
    const frame = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.11, 0.24), mats.dark);
    frame.position.set(0.18, -0.18, -0.3);
    g.add(frame);
    // slide
    const slide = new THREE.Mesh(new THREE.BoxGeometry(0.055, 0.06, 0.22), mats.rustyMetal);
    slide.position.set(0.18, -0.12, -0.3);
    g.add(slide);
    // grip
    const grip = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.14, 0.08), mats.wood);
    grip.position.set(0.18, -0.28, -0.24);
    grip.rotation.x = 0.2;
    g.add(grip);
    // barrel tip
    const barrel = new THREE.Mesh(new THREE.CylinderGeometry(0.02, 0.02, 0.04, 8), mats.dark);
    barrel.rotation.x = Math.PI / 2;
    barrel.position.set(0.18, -0.12, -0.43);
    g.add(barrel);
    this.muzzlePos = V3(0.18, -0.12, -0.43);

    this.group = g;
    this.cam.add(g);
  }

  tryReload() {
    if (this.reloading || this.ammoInMag === this.magSize || this.reserve === 0) return;
    this.reloading = true;
    this.reloadT = 1.6;
  }

  fire(worldWalls, enemies, onShot) {
    if (this.reloading || this.cooldown > 0) return false;
    if (this.ammoInMag <= 0) { this.tryReload(); return false; }
    this.ammoInMag--;
    this.cooldown = this.fireRate;
    this.recoilY += this.aim > 0.5 ? 0.012 : 0.03;
    this.swayX += (Math.random() - 0.5) * 0.02;

    // muzzle flash
    this.muzzleLight.intensity = 6;
    this.muzzleLight.position.copy(this.cam.position);
    const fwd = this._camFwd();
    this.muzzleLight.position.add(fwd.clone().multiplyScalar(0.4));
    audio.gunshot(this.cam.position);

    // shell ejection
    this._ejectShell();

    // ---- raycast: world + enemies ----
    const origin = this.cam.position.clone();
    const dir = fwd.clone();
    // minor spread (bigger when hipfire)
    const spread = this.aim > 0.5 ? 0.0015 : 0.012;
    dir.x += (Math.random() - 0.5) * spread;
    dir.y += (Math.random() - 0.5) * spread;
    dir.z += (Math.random() - 0.5) * spread;
    dir.normalize();

    let hit = null;
    let hitT = Infinity;

    // enemies first (hitboxes)
    for (const e of enemies) {
      if (!e.alive) continue;
      const h = e.raycast(origin, dir);
      if (h && h.t < hitT) { hitT = h.t; hit = { type: 'enemy', enemy: e, zone: h.zone, point: h.point }; }
    }

    // worldWalls (AABB)
    for (const w of worldWalls) {
      if (w.tag === 'trigger' || w.tag === 'floor') continue;
      const t = rayAABB(origin, dir, w.min, w.max);
      if (t !== null && t > 0 && t < hitT) {
        hitT = t; hit = { type: 'world', point: origin.clone().add(dir.clone().multiplyScalar(t)) };
      }
    }

    // ground y=0
    const tg = (0 - origin.y) / dir.y;
    if (tg > 0 && tg < hitT) {
      hitT = tg; hit = { type: 'world', point: origin.clone().add(dir.clone().multiplyScalar(tg)) };
    }

    if (hit) {
      if (hit.type === 'enemy') {
        const dmg = hit.zone === 'head' ? 100 : hit.zone === 'torso' ? 40 : 20;
        hit.enemy.damage(dmg, hit.point, hit.zone);
      } else {
        this._spark(hit.point);
        audio.arrowImpact(hit.point, 'metal');
      }
    }
    if (onShot) onShot(hit, this.cam.position);
    return true;
  }

  _camFwd() {
    const q = this.cam.quaternion;
    return new THREE.Vector3(0, 0, -1).applyQuaternion(q).normalize();
  }

  _ejectShell() {
    const mat = materials().amber;
    const shell = new THREE.Mesh(new THREE.CylinderGeometry(0.015, 0.015, 0.04, 6), mat);
    shell.position.copy(this.cam.position);
    const right = new THREE.Vector3(1, 0, 0).applyQuaternion(this.cam.quaternion);
    shell.position.add(right.clone().multiplyScalar(0.1));
    shell.position.y -= 0.05;
    const vel = right.clone().multiplyScalar(2 + Math.random())
      .add(new THREE.Vector3(0, 2.4 + Math.random(), 0))
      .add(this._camFwd().multiplyScalar(-0.5));
    this.scene.add(shell);
    this.shells.push({ mesh: shell, vel, life: 2, angV: (Math.random() - 0.5) * 10 });
  }

  _spark(p) {
    const mat = new THREE.PointsMaterial({ color: 0xffaa44, size: 0.06, transparent: true, opacity: 1 });
    const g = new THREE.BufferGeometry();
    const N = 8;
    const pos = new Float32Array(N * 3);
    for (let i = 0; i < N; i++) { pos[i * 3] = p.x; pos[i * 3 + 1] = p.y; pos[i * 3 + 2] = p.z; }
    g.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    const points = new THREE.Points(g, mat);
    points.userData.vels = new Array(N).fill(0).map(() => V3(
      (Math.random() - 0.5) * 4, Math.random() * 3, (Math.random() - 0.5) * 4,
    ));
    points.userData.life = 0.5;
    this.scene.add(points);
    this.sparks.push(points);
  }

  update(dt, aiming) {
    this.cooldown = Math.max(0, this.cooldown - dt);
    if (this.muzzleLight.intensity > 0) {
      this.muzzleLight.intensity = Math.max(0, this.muzzleLight.intensity - dt * 40);
    }

    // aim lerp
    const aimTarget = aiming ? 1 : 0;
    this.aim = lerp(this.aim, aimTarget, 0.18);

    // sway decay
    this.recoilY = lerp(this.recoilY, 0, 0.12);
    this.swayX = lerp(this.swayX, 0, 0.1);
    this.swayY = lerp(this.swayY, 0, 0.1);

    // position gun: hip vs aim
    const hipPos = V3(0.25, -0.22, -0.45);
    const aimPos = V3(0, -0.12, -0.38);
    const p = hipPos.lerp(aimPos, this.aim);
    this.group.position.copy(p);
    this.group.rotation.x = -this.recoilY + this.swayY;
    this.group.rotation.y = this.swayX;

    // reload
    if (this.reloading) {
      this.reloadT -= dt;
      this.group.rotation.z = Math.sin(this.reloadT * 4) * 0.3 - 0.2;
      if (this.reloadT <= 0) {
        const need = this.magSize - this.ammoInMag;
        const take = Math.min(need, this.reserve);
        this.ammoInMag += take; this.reserve -= take;
        this.reloading = false;
      }
    } else {
      this.group.rotation.z = lerp(this.group.rotation.z, 0, 0.15);
    }

    // shells physics
    for (let i = this.shells.length - 1; i >= 0; i--) {
      const s = this.shells[i];
      s.vel.y -= 9.8 * dt;
      s.mesh.position.addScaledVector(s.vel, dt);
      s.mesh.rotation.x += s.angV * dt;
      s.mesh.rotation.z += s.angV * 0.7 * dt;
      s.life -= dt;
      if (s.mesh.position.y < 0) s.mesh.position.y = 0;
      if (s.life <= 0) { this.scene.remove(s.mesh); this.shells.splice(i, 1); }
    }
    // sparks
    for (let i = this.sparks.length - 1; i >= 0; i--) {
      const sp = this.sparks[i];
      sp.userData.life -= dt;
      const pos = sp.geometry.attributes.position.array;
      const vels = sp.userData.vels;
      for (let k = 0; k < vels.length; k++) {
        vels[k].y -= 9.8 * dt;
        pos[k * 3] += vels[k].x * dt;
        pos[k * 3 + 1] += vels[k].y * dt;
        pos[k * 3 + 2] += vels[k].z * dt;
      }
      sp.geometry.attributes.position.needsUpdate = true;
      sp.material.opacity = Math.max(0, sp.userData.life / 0.5);
      if (sp.userData.life <= 0) { this.scene.remove(sp); this.sparks.splice(i, 1); }
    }
  }
}

// Ray vs AABB (slab method). Returns t or null.
export function rayAABB(o, d, min, max) {
  let tmin = (min.x - o.x) / d.x, tmax = (max.x - o.x) / d.x;
  if (tmin > tmax) [tmin, tmax] = [tmax, tmin];
  let tymin = (min.y - o.y) / d.y, tymax = (max.y - o.y) / d.y;
  if (tymin > tymax) [tymin, tymax] = [tymax, tymin];
  if (tmin > tymax || tymin > tmax) return null;
  tmin = Math.max(tmin, tymin); tmax = Math.min(tmax, tymax);
  let tzmin = (min.z - o.z) / d.z, tzmax = (max.z - o.z) / d.z;
  if (tzmin > tzmax) [tzmin, tzmax] = [tzmax, tzmin];
  if (tmin > tzmax || tzmin > tmax) return null;
  tmin = Math.max(tmin, tzmin); tmax = Math.min(tmax, tzmax);
  if (tmax < 0) return null;
  return tmin >= 0 ? tmin : tmax;
}
