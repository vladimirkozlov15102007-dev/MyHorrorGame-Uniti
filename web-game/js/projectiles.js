import * as THREE from 'three';
import { V3 } from './util.js';
import { rayAABB } from './weapon.js';
import { materials } from './materials.js';
import { audio } from './audio.js';

// ---------- ARROW ----------
export class Arrow {
  constructor(scene, origin, dir, speed, damage = 25, ownerTag = 'enemy') {
    this.scene = scene;
    const mats = materials();
    const g = new THREE.Group();
    const shaft = new THREE.Mesh(new THREE.CylinderGeometry(0.015, 0.015, 0.9, 6), mats.wood);
    shaft.rotation.x = Math.PI / 2; g.add(shaft);
    const head = new THREE.Mesh(new THREE.ConeGeometry(0.03, 0.12, 6), mats.dark);
    head.rotation.x = Math.PI / 2; head.position.z = 0.5; g.add(head);
    const feather = new THREE.Mesh(new THREE.PlaneGeometry(0.05, 0.12), mats.cloth);
    feather.position.z = -0.38; feather.rotation.y = Math.PI / 4; g.add(feather);

    this.mesh = g;
    scene.add(this.mesh);

    this.pos = origin.clone();
    this.vel = dir.clone().normalize().multiplyScalar(speed);
    this.life = 4.5;
    this.dead = false;
    this.damage = damage;
    this.tag = ownerTag;
    this.stuck = false;
  }

  update(dt, walls, target) {
    if (this.dead) return;
    if (this.stuck) {
      this.life -= dt;
      if (this.life <= 0) { this.dead = true; this.scene.remove(this.mesh); }
      return;
    }

    this.vel.y -= 9.8 * dt;
    const step = this.vel.clone().multiplyScalar(dt);
    const stepLen = step.length();
    const dir = step.clone().normalize();

    // 1) check target (player) if we're enemy-owned
    if (target && this.tag === 'enemy') {
      const dx = target.pos.x - this.pos.x;
      const dy = target.pos.y - this.pos.y;
      const dz = target.pos.z - this.pos.z;
      // very rough capsule test (cylinder)
      const proj = dx * dir.x + dy * dir.y + dz * dir.z;
      if (proj > 0 && proj < stepLen) {
        const cx = this.pos.x + dir.x * proj;
        const cy = this.pos.y + dir.y * proj;
        const cz = this.pos.z + dir.z * proj;
        const dpx = target.pos.x - cx;
        const dpy = target.pos.y - cy;
        const dpz = target.pos.z - cz;
        const distSq = dpx * dpx + dpz * dpz;
        if (distSq < 0.22 * 0.22 && Math.abs(dpy) < target.height * 0.6) {
          // determine zone by relative height
          const rel = (this.pos.y + dir.y * proj) - (target.pos.y);
          let dmg = 20;
          if (rel > 0.5) dmg = 30;          // head
          else if (rel > -0.4) dmg = 20;    // torso
          else dmg = 10;                    // legs
          target.damage(dmg, this.pos);
          audio.arrowImpact(target.pos, 'flesh');
          this._stickAt(this.pos.clone().add(dir.clone().multiplyScalar(proj)));
          return;
        }
      }
    }

    // 2) wall / ground hits
    let hitT = stepLen;
    let hit = null;
    for (const w of walls) {
      if (w.tag === 'trigger' || w.tag === 'floor') continue;
      const t = rayAABB(this.pos, dir, w.min, w.max);
      if (t !== null && t > 0 && t < hitT) { hitT = t; hit = { p: this.pos.clone().add(dir.clone().multiplyScalar(t)) }; }
    }
    if (this.pos.y + step.y < 0) {
      const tg = (0 - this.pos.y) / this.vel.y;
      const xt = this.pos.x + this.vel.x * tg, zt = this.pos.z + this.vel.z * tg;
      hit = { p: V3(xt, 0, zt) }; hitT = tg * this.vel.length() / stepLen * stepLen; // approximate
    }

    if (hit) {
      this._stickAt(hit.p);
      audio.arrowImpact(hit.p, 'wood');
      return;
    }

    // advance
    this.pos.add(step);
    this.mesh.position.copy(this.pos);
    this.mesh.lookAt(this.pos.clone().add(this.vel));
    this.life -= dt;
    if (this.life <= 0) { this.dead = true; this.scene.remove(this.mesh); }
  }

  _stickAt(p) {
    this.pos.copy(p);
    this.mesh.position.copy(this.pos);
    this.mesh.lookAt(this.pos.clone().add(this.vel));
    this.vel.set(0, 0, 0);
    this.stuck = true;
    this.life = 8;
  }
}

// ---------- THROWABLE (pickupable prop) ----------
export class Throwable {
  constructor(scene, type = 'bottle', pos = V3()) {
    this.scene = scene;
    this.type = type;
    this.mesh = this._makeMesh(type);
    this.mesh.position.copy(pos);
    scene.add(this.mesh);
    this.vel = V3(0, 0, 0);
    this.onGround = true;
    this.pickedUp = false;
  }
  _makeMesh(type) {
    const mats = materials();
    let m;
    if (type === 'bottle') {
      const g = new THREE.Group();
      const body = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.06, 0.22, 10), mats.glass);
      g.add(body);
      const neck = new THREE.Mesh(new THREE.CylinderGeometry(0.025, 0.035, 0.08, 8), mats.glass);
      neck.position.y = 0.15; g.add(neck);
      m = g;
    } else if (type === 'pipe') {
      const g = new THREE.Group();
      const body = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.04, 0.6, 8), mats.rustyMetal);
      body.rotation.z = Math.PI / 2; g.add(body);
      m = g;
    } else { // can / rock
      const g = new THREE.Group();
      const body = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.05, 0.12, 10), mats.rustyMetal);
      g.add(body);
      m = g;
    }
    return m;
  }

  throwIt(origin, dir, force) {
    this.pickedUp = false;
    this.mesh.position.copy(origin);
    this.vel = dir.clone().multiplyScalar(force);
    this.onGround = false;
    this.scene.add(this.mesh);
  }

  pickup() { this.pickedUp = true; this.scene.remove(this.mesh); }

  update(dt, walls, onNoise) {
    if (this.pickedUp) return;
    if (this.onGround) return;

    this.vel.y -= 9.8 * dt;
    const step = this.vel.clone().multiplyScalar(dt);
    this.mesh.position.add(step);
    this.mesh.rotation.x += dt * 8;
    this.mesh.rotation.z += dt * 6;

    // ground
    if (this.mesh.position.y <= 0.1) {
      this.mesh.position.y = 0.1;
      this.vel.set(0, 0, 0);
      this.onGround = true;
      const loud = this.type === 'bottle' ? 1.5 : this.type === 'pipe' ? 1.2 : 1;
      audio.glassBreak(this.mesh.position);
      if (onNoise) onNoise(this.mesh.position, 15 * loud);
      if (this.type === 'bottle') {
        // destroy bottle after break
        setTimeout(() => this.scene.remove(this.mesh), 200);
      }
      return;
    }
    // wall
    for (const w of walls) {
      if (w.tag === 'trigger' || w.tag === 'floor') continue;
      const p = this.mesh.position;
      if (p.x > w.min.x && p.x < w.max.x && p.z > w.min.z && p.z < w.max.z && p.y > w.min.y && p.y < w.max.y) {
        this.vel.x *= -0.3; this.vel.z *= -0.3; this.vel.y *= -0.3;
        audio.thud(p);
        if (onNoise) onNoise(p, 12);
        break;
      }
    }
  }
}
