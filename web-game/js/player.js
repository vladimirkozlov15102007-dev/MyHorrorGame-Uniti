import * as THREE from 'three';
import { V3, clamp, resolveXZ, lerp } from './util.js';
import { audio } from './audio.js';

export class Player {
  constructor(camera, walls) {
    this.cam = camera;
    this.walls = walls;
    this.pos = V3(-38, 1.6, 12);
    this.vel = V3(0, 0, 0);
    this.yaw = 0;
    this.pitch = 0;
    this.height = 1.65;
    this.crouchH = 1.0;
    this.radius = 0.35;
    this.onGround = true;

    this.hp = 100;
    this.stamina = 100;
    this.sprint = false;
    this.crouch = false;
    this.moving = false;

    this.footTimer = 0;
    this.bobT = 0;
    this.bobOffset = 0;

    this.keys = {};
    this.mouse = { x: 0, y: 0 };
    this.sens = 0.0022;

    // damage
    this.hurtTimer = 0;
    this.lastHitDir = V3(0, 0, 0);

    this._bindInput();

    // flashlight (off by default)
    this.flashlight = new THREE.SpotLight(0xfff0d0, 0, 22, Math.PI / 5, 0.4, 1.5);
    this.flashlight.position.set(0, 0, 0);
    this.flashlight.target.position.set(0, 0, -1);
    this.flashlightOn = false;
  }

  attachFlashlight(scene) {
    scene.add(this.flashlight);
    scene.add(this.flashlight.target);
  }

  _bindInput() {
    window.addEventListener('keydown', (e) => {
      this.keys[e.code] = true;
      if (e.code === 'KeyF') {
        this.flashlightOn = !this.flashlightOn;
        this.flashlight.intensity = this.flashlightOn ? 2.5 : 0;
      }
    });
    window.addEventListener('keyup', (e) => { this.keys[e.code] = false; });
    document.addEventListener('mousemove', (e) => {
      if (document.pointerLockElement) {
        this.yaw -= e.movementX * this.sens;
        this.pitch -= e.movementY * this.sens;
        this.pitch = clamp(this.pitch, -Math.PI / 2 + 0.05, Math.PI / 2 - 0.05);
      }
    });
  }

  damage(amount, fromPos) {
    if (this.hp <= 0) return;
    this.hp = Math.max(0, this.hp - amount);
    this.hurtTimer = 0.5;
    if (fromPos) this.lastHitDir.copy(fromPos).sub(this.pos).setY(0).normalize();
    audio.hurt();
  }

  get forward() {
    return V3(-Math.sin(this.yaw), 0, -Math.cos(this.yaw));
  }
  get right() {
    return V3(Math.cos(this.yaw), 0, -Math.sin(this.yaw));
  }

  update(dt, onNoise) {
    // ----- input -> desired velocity -----
    const f = this.forward, r = this.right;
    let ix = 0, iz = 0;
    if (this.keys.KeyW) iz += 1;
    if (this.keys.KeyS) iz -= 1;
    if (this.keys.KeyD) ix += 1;
    if (this.keys.KeyA) ix -= 1;
    const inputLen = Math.hypot(ix, iz);
    if (inputLen > 0) { ix /= inputLen; iz /= inputLen; }
    this.moving = inputLen > 0;

    this.sprint = !!this.keys.ShiftLeft && this.stamina > 0 && !this.crouch && this.moving && iz > 0.1;
    this.crouch = !!this.keys.ControlLeft;

    const baseSpd = this.crouch ? 1.6 : this.sprint ? 5.4 : 3.2;
    const desired = V3(
      r.x * ix + f.x * iz,
      0,
      r.z * ix + f.z * iz,
    ).multiplyScalar(baseSpd);

    // gravity
    this.vel.x = lerp(this.vel.x, desired.x, 0.25);
    this.vel.z = lerp(this.vel.z, desired.z, 0.25);
    this.vel.y -= 20 * dt;

    // jump
    if (this.keys.Space && this.onGround && this.stamina > 10) {
      this.vel.y = 6.5;
      this.stamina = Math.max(0, this.stamina - 10);
      this.onGround = false;
    }

    // integrate
    this.pos.x += this.vel.x * dt;
    this.pos.z += this.vel.z * dt;
    this.pos.y += this.vel.y * dt;

    // ground collision: floor is y=0 outdoors and per zone, we keep it at 0
    if (this.pos.y < this.height * (this.crouch ? 0.6 : 1)) {
      this.pos.y = this.height * (this.crouch ? 0.6 : 1);
      this.vel.y = 0; this.onGround = true;
    } else {
      this.onGround = false;
    }

    resolveXZ(this.pos, this.radius, this.walls);

    // stamina regen/drain
    if (this.sprint) this.stamina = Math.max(0, this.stamina - 20 * dt);
    else this.stamina = Math.min(100, this.stamina + 12 * dt);

    // footsteps & noise
    if (this.moving && this.onGround) {
      this.footTimer -= dt * (this.sprint ? 2.2 : this.crouch ? 0.9 : 1.5);
      if (this.footTimer <= 0) {
        this.footTimer = 0.35;
        audio.footstep(this.pos);
        // emit noise event for AI (range based on state)
        const noiseR = this.sprint ? 22 : this.crouch ? 4 : 12;
        if (onNoise) onNoise(this.pos, noiseR);
      }
    }

    // head bob
    if (this.moving && this.onGround) {
      this.bobT += dt * (this.sprint ? 12 : this.crouch ? 5 : 8);
      this.bobOffset = Math.sin(this.bobT) * (this.sprint ? 0.08 : 0.04);
    } else {
      this.bobT = 0;
      this.bobOffset = lerp(this.bobOffset, 0, 0.1);
    }

    // camera
    this.cam.position.copy(this.pos).y += this.bobOffset;
    this.cam.rotation.order = 'YXZ';
    this.cam.rotation.y = this.yaw;
    this.cam.rotation.x = this.pitch;

    // flashlight follows camera
    if (this.flashlight) {
      this.flashlight.position.copy(this.cam.position);
      const fwd = V3(-Math.sin(this.yaw) * Math.cos(this.pitch), Math.sin(this.pitch), -Math.cos(this.yaw) * Math.cos(this.pitch));
      this.flashlight.target.position.copy(this.cam.position).add(fwd);
    }

    if (this.hurtTimer > 0) this.hurtTimer -= dt;
  }
}
