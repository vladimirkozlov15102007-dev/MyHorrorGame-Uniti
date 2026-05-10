import * as THREE from 'three';
import { V3, rand, clamp, resolveXZ, hasLOS, yawTo } from './util.js';
import { materials } from './materials.js';
import { audio } from './audio.js';
import { Arrow } from './projectiles.js';

// Procedurally assembled skeleton archer.
// Not a rigged GLTF — we drive bones directly: pelvis, spine, head, arms, legs, bow.
// Animations are phase-based (walk, aim, shoot, stagger, death-ragdoll).

export class Skeleton {
  constructor(id, scene, pos, walls, opts = {}) {
    this.id = id;
    this.scene = scene;
    this.walls = walls;
    this.alive = true;
    this.hp = 100;
    this.pos = pos.clone();
    this.vel = V3();
    this.yaw = rand(0, Math.PI * 2);

    // variety
    this.scale = rand(0.95, 1.08);
    this.tilt = rand(-0.04, 0.04);
    this.limp = Math.random() < 0.3;
    this.color = 0xdcd0b6;

    // AI state
    this.state = 'patrol';
    this.stateT = 0;
    this.target = null;           // reference to player
    this.lastSeen = null;
    this.investigatePt = null;
    this.patrolDir = rand(0, Math.PI * 2);
    this.patrolT = 0;
    this.suppress = false;        // suppression fire mode
    this.ambushUntil = 0;
    this.flankDir = 0;            // -1 left, +1 right, 0 none
    this.groupCommand = null;     // overriding state from coordinator

    // combat
    this.aimT = 0;
    this.fireCooldown = rand(2, 4);
    this.speed = opts.speed || 2.2;
    this.sightRange = 28;
    this.sightFov = Math.PI * 0.75; // ~135deg
    this.hearingRange = 14;

    this.group = new THREE.Group();
    this.group.position.copy(this.pos);
    scene.add(this.group);
    this._buildMesh();

    // procedural phases
    this.walkPhase = Math.random() * Math.PI * 2;
    this.breath = Math.random() * Math.PI * 2;

    // ragdoll
    this.ragdollParts = null;
  }

  _buildMesh() {
    const mats = materials();
    const bone = mats.bone.clone();
    bone.color = new THREE.Color(this.color).multiplyScalar(rand(0.9, 1.05));
    this.bone = bone;

    this.root = new THREE.Group();
    this.root.scale.setScalar(this.scale);
    this.group.add(this.root);

    // Pelvis at ~0.9, head at ~1.7
    this.pelvis = new THREE.Mesh(new THREE.BoxGeometry(0.32, 0.18, 0.22), bone);
    this.pelvis.position.y = 0.9; this.pelvis.castShadow = true;
    this.root.add(this.pelvis);

    this.spine = new THREE.Group(); this.spine.position.y = 0.15; this.pelvis.add(this.spine);
    // ribs - stack of slightly bent boxes
    for (let i = 0; i < 6; i++) {
      const r = new THREE.Mesh(new THREE.BoxGeometry(0.28 - i * 0.01, 0.05, 0.18 - i * 0.006), bone);
      r.position.y = 0.06 + i * 0.07;
      r.rotation.x = -i * 0.02 + this.tilt;
      this.spine.add(r);
    }
    // shoulder block
    const shBlock = new THREE.Mesh(new THREE.BoxGeometry(0.38, 0.08, 0.12), bone);
    shBlock.position.y = 0.52; this.spine.add(shBlock);

    // neck + skull
    this.neck = new THREE.Group(); this.neck.position.y = 0.58; this.spine.add(this.neck);
    const neckMesh = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.05, 0.1, 8), bone);
    neckMesh.position.y = 0.05; this.neck.add(neckMesh);
    const skull = new THREE.Mesh(new THREE.SphereGeometry(0.12, 12, 10), bone);
    skull.position.y = 0.2; skull.scale.set(0.9, 1.05, 1.1); this.neck.add(skull);
    // jaw
    const jaw = new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.05, 0.14), bone);
    jaw.position.set(0, 0.13, 0.04); this.neck.add(jaw);
    // glowing eyes
    const eyeMat = new THREE.MeshStandardMaterial({ color: 0xffaa33, emissive: 0xff8800, emissiveIntensity: 3 });
    const eyeGeo = new THREE.SphereGeometry(0.022, 8, 8);
    for (const x of [-0.045, 0.045]) {
      const e = new THREE.Mesh(eyeGeo, eyeMat);
      e.position.set(x, 0.22, 0.1); this.neck.add(e);
    }
    // cloth rags
    const ragMat = new THREE.MeshStandardMaterial({ color: 0x3a2e24, roughness: 1, side: THREE.DoubleSide });
    for (let i = 0; i < 3; i++) {
      const rag = new THREE.Mesh(new THREE.PlaneGeometry(rand(0.3, 0.45), rand(0.3, 0.5)), ragMat);
      rag.position.set(rand(-0.1, 0.1), rand(0.15, 0.4), rand(-0.1, 0.1));
      rag.rotation.set(rand(-0.3, 0.3), rand(0, Math.PI), 0);
      this.spine.add(rag);
    }

    // arms: L/R groups anchored at shoulders
    this.shoulderL = new THREE.Group(); this.shoulderL.position.set(-0.2, 0.52, 0); this.spine.add(this.shoulderL);
    this.shoulderR = new THREE.Group(); this.shoulderR.position.set(0.2, 0.52, 0); this.spine.add(this.shoulderR);
    for (const [sh, side] of [[this.shoulderL, -1], [this.shoulderR, 1]]) {
      const upper = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.035, 0.28, 8), bone);
      upper.position.y = -0.14; sh.add(upper);
      const elbow = new THREE.Group(); elbow.position.y = -0.28; sh.add(elbow);
      const lower = new THREE.Mesh(new THREE.CylinderGeometry(0.035, 0.03, 0.28, 8), bone);
      lower.position.y = -0.14; elbow.add(lower);
      const hand = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.08, 0.05), bone);
      hand.position.y = -0.32; elbow.add(hand);
      sh.userData.elbow = elbow;
      sh.userData.hand = hand;
      sh.userData.side = side;
    }
    // legs
    this.hipL = new THREE.Group(); this.hipL.position.set(-0.12, -0.05, 0); this.pelvis.add(this.hipL);
    this.hipR = new THREE.Group(); this.hipR.position.set(0.12, -0.05, 0); this.pelvis.add(this.hipR);
    for (const h of [this.hipL, this.hipR]) {
      const thigh = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.045, 0.42, 8), bone);
      thigh.position.y = -0.22; h.add(thigh);
      const knee = new THREE.Group(); knee.position.y = -0.42; h.add(knee);
      const shin = new THREE.Mesh(new THREE.CylinderGeometry(0.045, 0.035, 0.42, 8), bone);
      shin.position.y = -0.22; knee.add(shin);
      const foot = new THREE.Mesh(new THREE.BoxGeometry(0.12, 0.05, 0.22), bone);
      foot.position.set(0, -0.44, 0.04); knee.add(foot);
      h.userData.knee = knee;
    }

    // bow in left hand
    const bow = new THREE.Group();
    const bowArc = new THREE.Mesh(new THREE.TorusGeometry(0.32, 0.015, 5, 20, Math.PI * 1.2), mats.wood);
    bowArc.rotation.z = Math.PI / 2; bow.add(bowArc);
    const string = new THREE.Mesh(new THREE.BoxGeometry(0.005, 0.56, 0.005), mats.cloth);
    bow.add(string);
    bow.rotation.x = Math.PI / 2;
    this.shoulderL.userData.elbow.userData.hand.add(bow);
    this.bow = bow;

    // a bigger collider/hitbox ring for raycast
    this.hbTorso = new THREE.Box3();
    this.hbHead = new THREE.Box3();
    this.hbLegs = new THREE.Box3();
    this._updateHitboxes();
  }

  _updateHitboxes() {
    const x = this.pos.x, y = this.pos.y, z = this.pos.z;
    this.hbTorso.set(new THREE.Vector3(x - 0.25, y + 0.85, z - 0.2), new THREE.Vector3(x + 0.25, y + 1.45, z + 0.2));
    this.hbHead.set(new THREE.Vector3(x - 0.15, y + 1.45, z - 0.15), new THREE.Vector3(x + 0.15, y + 1.72, z + 0.15));
    this.hbLegs.set(new THREE.Vector3(x - 0.2, y + 0.05, z - 0.15), new THREE.Vector3(x + 0.2, y + 0.85, z + 0.15));
  }

  // ray vs this skeleton's hitboxes (returns {t, zone, point}) or null
  raycast(origin, dir) {
    const boxes = [
      { b: this.hbHead, z: 'head' },
      { b: this.hbTorso, z: 'torso' },
      { b: this.hbLegs, z: 'legs' },
    ];
    let best = null;
    for (const { b, z } of boxes) {
      const t = rayBox(origin, dir, b.min, b.max);
      if (t !== null && t > 0 && (!best || t < best.t)) {
        best = { t, zone: z, point: origin.clone().add(dir.clone().multiplyScalar(t)) };
      }
    }
    return best;
  }

  damage(amount, point, zone) {
    if (!this.alive) return;
    this.hp -= amount;
    if (this.hp <= 0) {
      this.die(point);
      return;
    }
    this.stateT = 0;
    this.state = 'alert';
    this.aimT = 0;
    // stagger visually
    this.spine.rotation.x = -0.3;
    setTimeout(() => { if (this.spine) this.spine.rotation.x = 0; }, 180);
  }

  die(point) {
    this.alive = false;
    // simple "ragdoll-ish" — rotate & drop body
    this.root.rotation.set(rand(-1.2, -0.7), rand(-0.4, 0.4), rand(-0.3, 0.3));
    this.root.position.y = 0.1;
    // scatter cloth via spin
    audio.thud(point || this.pos);
    this.deadTime = 0;
  }

  _faceTarget(wpos, dt) {
    const targetYaw = yawTo(this.pos, wpos);
    let dy = targetYaw - this.yaw;
    while (dy > Math.PI) dy -= Math.PI * 2;
    while (dy < -Math.PI) dy += Math.PI * 2;
    this.yaw += clamp(dy, -3 * dt, 3 * dt);
  }

  canSee(player) {
    const dx = player.pos.x - this.pos.x, dz = player.pos.z - this.pos.z;
    const d = Math.hypot(dx, dz);
    if (d > this.sightRange) return false;
    const ang = Math.atan2(dx, dz);
    let da = ang - this.yaw;
    while (da > Math.PI) da -= Math.PI * 2;
    while (da < -Math.PI) da += Math.PI * 2;
    if (Math.abs(da) > this.sightFov / 2) return false;
    // LOS
    return hasLOS(
      V3(this.pos.x, 1.5, this.pos.z),
      V3(player.pos.x, 1.5, player.pos.z),
      this.walls, 0.6,
    );
  }

  hearNoise(pos, radius) {
    const d = Math.hypot(pos.x - this.pos.x, pos.z - this.pos.z);
    if (d < radius) {
      // go investigate if not already in combat
      if (this.state === 'patrol' || this.state === 'investigate') {
        this.investigatePt = pos.clone();
        this.state = 'investigate';
        this.stateT = 0;
      }
    }
  }

  // ----- AI tick -----
  aiUpdate(dt, player, analyzer, coordinator, projectiles) {
    if (!this.alive) return;
    this.stateT += dt;

    const canSee = this.canSee(player);
    if (canSee) {
      this.lastSeen = player.pos.clone();
      coordinator.reportSighting(this.id, player.pos);
    }

    // listen to coordinator orders
    if (this.groupCommand) {
      this.state = this.groupCommand;
      this.groupCommand = null;
    }

    // state machine
    switch (this.state) {
      case 'patrol':
        this._patrol(dt);
        if (canSee) { this.state = 'combat'; this.stateT = 0; audio.bowShot(this.pos); }
        break;
      case 'investigate':
        this._investigate(dt);
        if (canSee) { this.state = 'combat'; this.stateT = 0; }
        break;
      case 'alert':
        // turn towards last seen, scan
        if (this.lastSeen) this._faceTarget(this.lastSeen, dt);
        if (canSee) { this.state = 'combat'; this.stateT = 0; }
        if (this.stateT > 3) { this.state = 'search'; this.stateT = 0; }
        break;
      case 'combat':
        this._combat(dt, player, analyzer, projectiles);
        if (!canSee && this.stateT > 2.5) { this.state = 'search'; this.stateT = 0; }
        if (this.hp < 28 && Math.random() < 0.02) { this.state = 'retreat'; this.stateT = 0; }
        break;
      case 'flank':
        this._flank(dt, player, projectiles);
        if (this.stateT > 6) { this.state = 'combat'; this.stateT = 0; }
        break;
      case 'ambush':
        this._ambush(dt, player, projectiles);
        if (canSee && this.stateT > 1.5) { this.state = 'combat'; this.stateT = 0; }
        break;
      case 'search':
        this._search(dt);
        if (canSee) { this.state = 'combat'; this.stateT = 0; }
        if (this.stateT > 7) { this.state = 'patrol'; this.stateT = 0; this.lastSeen = null; }
        break;
      case 'retreat':
        this._retreat(dt, player);
        if (this.stateT > 4) { this.state = 'combat'; this.stateT = 0; }
        break;
      case 'group_attack':
        this._combat(dt, player, analyzer, projectiles);
        if (this.stateT > 5) { this.state = 'combat'; this.stateT = 0; }
        break;
    }

    // integrate motion
    const oldX = this.pos.x, oldZ = this.pos.z;
    this.pos.x += this.vel.x * dt;
    this.pos.z += this.vel.z * dt;
    this.pos.y = 0;       // grounded
    resolveXZ(this.pos, 0.32, this.walls);

    // friction
    this.vel.x *= 0.84;
    this.vel.z *= 0.84;

    // animate
    const moving = Math.hypot(this.pos.x - oldX, this.pos.z - oldZ) > 0.004;
    this._animate(dt, moving);

    // update hitboxes + root transform
    this.group.position.copy(this.pos);
    this.group.rotation.y = this.yaw;
    this._updateHitboxes();
  }

  // ---------- states ----------
  _patrol(dt) {
    this.patrolT -= dt;
    if (this.patrolT <= 0) {
      this.patrolT = rand(2, 5);
      this.patrolDir = rand(0, Math.PI * 2);
      if (Math.random() < 0.35) this.patrolDir = this.yaw + rand(-0.8, 0.8); // mostly stay similar direction
    }
    const spd = 1.0 * (this.limp ? 0.8 : 1);
    this.vel.x = -Math.sin(this.patrolDir) * spd;
    this.vel.z = -Math.cos(this.patrolDir) * spd;
    this.yaw = this.patrolDir;
  }
  _investigate(dt) {
    if (!this.investigatePt) { this.state = 'patrol'; return; }
    const d = Math.hypot(this.investigatePt.x - this.pos.x, this.investigatePt.z - this.pos.z);
    if (d < 1.2) { this.state = 'search'; this.stateT = 0; this.investigatePt = null; return; }
    this._faceTarget(this.investigatePt, dt);
    this.vel.x = -Math.sin(this.yaw) * 1.6;
    this.vel.z = -Math.cos(this.yaw) * 1.6;
  }
  _combat(dt, player, analyzer, projectiles) {
    this._faceTarget(player.pos, dt);
    const d = Math.hypot(player.pos.x - this.pos.x, player.pos.z - this.pos.z);

    // keep distance based on analyzer + suppress
    const desiredD = this.suppress ? 14 : analyzer.playerAggressive ? 12 : 8;
    const error = d - desiredD;
    const strafe = analyzer.playerStaysInCover ? Math.sin(this.stateT * 1.5) * 0.8 : 0;

    const fwd = error * 0.3;
    const fwdX = -Math.sin(this.yaw) * fwd;
    const fwdZ = -Math.cos(this.yaw) * fwd;
    const stX = -Math.sin(this.yaw + Math.PI / 2) * strafe;
    const stZ = -Math.cos(this.yaw + Math.PI / 2) * strafe;
    this.vel.x = clamp(fwdX + stX, -2.2, 2.2);
    this.vel.z = clamp(fwdZ + stZ, -2.2, 2.2);

    // aim & shoot
    this.aimT = Math.min(1, this.aimT + dt * 1.2);
    this.fireCooldown -= dt;
    if (this.aimT >= 1 && this.fireCooldown <= 0 && hasLOS(V3(this.pos.x, 1.4, this.pos.z), V3(player.pos.x, 1.4, player.pos.z), this.walls, 0.6)) {
      this._shootArrow(player, projectiles);
      this.fireCooldown = this.suppress ? rand(0.8, 1.4) : rand(2, 3.6);
    }
  }
  _flank(dt, player, projectiles) {
    // strafe around player toward desired flank side
    const toward = V3(player.pos.x - this.pos.x, 0, player.pos.z - this.pos.z);
    const d = toward.length();
    if (d < 0.01) return;
    toward.multiplyScalar(1 / d);
    const side = V3(-toward.z, 0, toward.x).multiplyScalar(this.flankDir || 1);
    const goal = V3(player.pos.x + side.x * 8, 0, player.pos.z + side.z * 8);
    this._faceTarget(goal, dt);
    this.vel.x = -Math.sin(this.yaw) * 2.0;
    this.vel.z = -Math.cos(this.yaw) * 2.0;
    // occasional shots
    this.fireCooldown -= dt;
    if (this.fireCooldown <= 0 && this.canSee(player)) {
      this._shootArrow(player, projectiles);
      this.fireCooldown = rand(2.4, 4);
    }
  }
  _ambush(dt, player, projectiles) {
    // stand still, face last known, fire when sighted
    if (this.lastSeen) this._faceTarget(this.lastSeen, dt);
    this.vel.set(0, 0, 0);
    if (this.canSee(player)) {
      this.fireCooldown -= dt;
      if (this.fireCooldown <= 0) { this._shootArrow(player, projectiles); this.fireCooldown = rand(1.4, 2.4); }
    }
  }
  _search(dt) {
    this.patrolT -= dt;
    if (!this.lastSeen) { this.state = 'patrol'; return; }
    if (this.patrolT <= 0) {
      this.patrolT = rand(1.2, 2.5);
      this.investigatePt = this.lastSeen.clone().add(V3(rand(-6, 6), 0, rand(-6, 6)));
    }
    if (this.investigatePt) {
      this._faceTarget(this.investigatePt, dt);
      this.vel.x = -Math.sin(this.yaw) * 1.3;
      this.vel.z = -Math.cos(this.yaw) * 1.3;
    }
  }
  _retreat(dt, player) {
    // move away + keep facing
    const toPlayer = yawTo(this.pos, player.pos);
    const away = toPlayer + Math.PI;
    this.vel.x = -Math.sin(away) * 2.6;
    this.vel.z = -Math.cos(away) * 2.6;
    this._faceTarget(player.pos, dt);
  }

  _shootArrow(player, projectiles) {
    const muzzle = V3(this.pos.x - Math.sin(this.yaw) * 0.4, 1.35, this.pos.z - Math.cos(this.yaw) * 0.4);
    const dir = V3(player.pos.x - muzzle.x, player.pos.y + 0.2 - muzzle.y, player.pos.z - muzzle.z);
    const dist = dir.length();
    dir.normalize();
    // ballistic lead + gravity compensation
    const speed = 28;
    const tof = dist / speed;
    dir.y += 9.8 * tof * 0.5 / speed;       // lift
    dir.normalize();
    const a = new Arrow(this.scene, muzzle, dir, speed, 25, 'enemy');
    projectiles.push(a);
    audio.bowShot(this.pos);
  }

  // ---------- animation ----------
  _animate(dt, moving) {
    if (!this.alive) {
      // keep on floor
      return;
    }
    this.breath += dt;
    const baseY = 0.9 + Math.sin(this.breath * 1.3) * 0.01;
    this.pelvis.position.y = baseY;

    if (moving) {
      this.walkPhase += dt * (this.state === 'combat' ? 6 : 5);
      const p = this.walkPhase;
      // legs
      this.hipL.rotation.x = Math.sin(p) * 0.6 * (this.limp ? 0.6 : 1);
      this.hipR.rotation.x = -Math.sin(p) * 0.6 * (this.limp ? 1.2 : 1);
      this.hipL.userData.knee.rotation.x = Math.max(0, -Math.sin(p + 0.7)) * 0.9;
      this.hipR.userData.knee.rotation.x = Math.max(0, Math.sin(p + 0.7)) * 0.9;
      // arm counter
      this.shoulderL.rotation.x = -Math.sin(p) * 0.3;
      this.shoulderR.rotation.x = Math.sin(p) * 0.3;
      // small bob
      this.pelvis.position.y = baseY + Math.abs(Math.sin(p)) * 0.03;
      // spine counter-twist
      this.spine.rotation.y = Math.sin(p) * 0.06;
    } else {
      // idle
      this.walkPhase = 0;
      this.hipL.rotation.x *= 0.85;
      this.hipR.rotation.x *= 0.85;
      this.hipL.userData.knee.rotation.x *= 0.85;
      this.hipR.userData.knee.rotation.x *= 0.85;
      this.shoulderL.rotation.x *= 0.85;
      this.shoulderR.rotation.x *= 0.85;
    }

    // aim pose for left arm holding bow + right arm drawing string
    if (this.state === 'combat' || this.state === 'ambush' || this.state === 'flank') {
      const a = this.aimT;
      // left arm forward, raised
      this.shoulderL.rotation.x = -1.3 * a;
      this.shoulderL.rotation.z = -0.3 * a;
      this.shoulderL.userData.elbow.rotation.x = -0.2 * a;
      // right arm pulled back
      this.shoulderR.rotation.x = -1.1 * a;
      this.shoulderR.rotation.z = 0.2 * a;
      this.shoulderR.userData.elbow.rotation.x = -1.3 * a;
      // spine slightly twisted toward target
      this.spine.rotation.y = 0.15 * a;
    } else {
      this.aimT = Math.max(0, this.aimT - dt * 2);
    }

    // head track target if known
    const tgt = this.lastSeen || null;
    if (tgt) {
      const dx = tgt.x - this.pos.x, dz = tgt.z - this.pos.z;
      const ang = Math.atan2(dx, dz) - this.yaw;
      this.neck.rotation.y = clamp(ang, -0.8, 0.8);
      this.neck.rotation.x = clamp((tgt.y ? tgt.y - 1.6 : 0) * 0.3, -0.4, 0.4);
    } else {
      this.neck.rotation.y *= 0.9;
      this.neck.rotation.x *= 0.9;
    }
  }
}

// rayAABB duplicate to avoid circular imports
function rayBox(o, d, min, max) {
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
