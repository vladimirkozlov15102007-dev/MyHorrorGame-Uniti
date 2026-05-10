import * as THREE from 'three';
import { materials } from './materials.js';
import { buildLevel } from './level.js';
import { Player } from './player.js';
import { Pistol } from './weapon.js';
import { Throwable } from './projectiles.js';
import { Skeleton } from './skeleton.js';
import { PlayerBehaviorAnalyzer, GroupCoordinator } from './ai_group.js';
import { createComposer, createDustField, createGodRays } from './postfx.js';
import { audio } from './audio.js';
import { V3, clamp, rand } from './util.js';

export class Game {
  constructor() {
    this.canvas = document.getElementById('gl');
    this.renderer = new THREE.WebGLRenderer({ canvas: this.canvas, antialias: true, powerPreference: 'high-performance' });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5));
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 0.9;
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x05080c);

    this.camera = new THREE.PerspectiveCamera(72, window.innerWidth / window.innerHeight, 0.05, 260);
    this.secCam = new THREE.PerspectiveCamera(55, window.innerWidth / window.innerHeight, 0.1, 80);

    // build world
    this.level = buildLevel(this.scene, this.renderer);
    this.walls = this.level.walls;

    // player
    this.player = new Player(this.camera, this.walls);
    this.player.attachFlashlight(this.scene);
    this.scene.add(this.camera);

    // pistol
    this.pistol = new Pistol(this.camera, this.scene);

    // throwables scattered near spawn
    this.throwables = [];
    for (let i = 0; i < 8; i++) {
      const t = new Throwable(
        this.scene,
        ['bottle', 'bottle', 'pipe', 'can'][Math.floor(Math.random() * 4)],
        V3(rand(-36, -10), 0.1, rand(-12, 12)),
      );
      this.throwables.push(t);
    }
    for (let i = 0; i < 6; i++) {
      const t = new Throwable(
        this.scene,
        ['bottle', 'pipe', 'can'][Math.floor(Math.random() * 3)],
        V3(rand(-2, 20), 0.1, rand(-14, 18)),
      );
      this.throwables.push(t);
    }
    this.heldThrowable = null;
    this.throwCharge = 0;

    // skeletons
    this.skeletons = [];
    for (let i = 0; i < 10; i++) {
      const sp = this.level.spawnPoints[i % this.level.spawnPoints.length];
      const s = new Skeleton(i, this.scene, sp, this.walls);
      this.skeletons.push(s);
    }
    this.analyzer = new PlayerBehaviorAnalyzer();
    this.coordinator = new GroupCoordinator(this.skeletons);

    // arrows
    this.arrows = [];

    // post-fx
    const { composer, bloom, grade, fxaa } = createComposer(this.renderer, this.scene, this.camera);
    this.composer = composer;
    this.bloom = bloom;
    this.grade = grade;
    this.fxaa = fxaa;
    this.dust = createDustField(this.scene, 600, { x: 60, y: 10, z: 60, cx: 5, cy: 4, cz: 0 });
    this.rays = createGodRays(this.scene);

    // state
    this.time = 0;
    this.running = false;

    // game progress
    this.powerOn = false;
    this.hasKey = false;
    this.gameState = 'playing';    // playing, dead, won
    this.viewingCam = -1;
    this.camCooldown = 0;

    // hud refs
    this.$ = {
      hp: document.getElementById('hp'),
      hpnum: document.getElementById('hpnum'),
      sta: document.getElementById('sta'),
      ammo: document.getElementById('ammo'),
      enemies: document.getElementById('enemies'),
      zone: document.getElementById('zone'),
      prompt: document.getElementById('prompt'),
      subtitle: document.getElementById('subtitle'),
      flash: document.getElementById('flash'),
      blood: document.getElementById('blood'),
      objective: document.getElementById('objective'),
      dds: {
        n: document.querySelector('.dd-n'),
        s: document.querySelector('.dd-s'),
        e: document.querySelector('.dd-e'),
        w: document.querySelector('.dd-w'),
      },
    };

    // input: firing / interact
    this.mouseDown = false;
    this.rmbDown = false;
    window.addEventListener('mousedown', (e) => {
      if (!document.pointerLockElement) return;
      if (e.button === 0) this.mouseDown = true;
      if (e.button === 2) this.rmbDown = true;
    });
    window.addEventListener('mouseup', (e) => {
      if (e.button === 0) {
        if (this.heldThrowable && this.throwCharge > 0.2) this._throwHeld();
        this.mouseDown = false;
      }
      if (e.button === 2) this.rmbDown = false;
    });
    document.addEventListener('contextmenu', (e) => e.preventDefault());
    window.addEventListener('keydown', (e) => {
      if (e.code === 'KeyR') this.pistol.tryReload();
      if (e.code === 'KeyE') this._tryInteract();
      if (e.code === 'KeyG' && this.heldThrowable) { this._dropHeld(); }
      if (e.code === 'Escape' && this.viewingCam >= 0) this._exitCamera();
    });

    window.addEventListener('resize', () => this._onResize());
  }

  _onResize() {
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.camera.aspect = window.innerWidth / window.innerHeight;
    this.camera.updateProjectionMatrix();
    this.secCam.aspect = this.camera.aspect;
    this.secCam.updateProjectionMatrix();
    this.composer.setSize(window.innerWidth, window.innerHeight);
    this.fxaa.material.uniforms['resolution'].value.set(1 / window.innerWidth, 1 / window.innerHeight);
  }

  start() {
    this.running = true;
    this.canvas.requestPointerLock();
    this._subtitle('Find the yellow truck. Survive the night.', 4);
    this._loop(performance.now());
  }

  _subtitle(txt, sec = 3) {
    this.$.subtitle.textContent = txt;
    clearTimeout(this._subT);
    this._subT = setTimeout(() => { this.$.subtitle.textContent = ''; }, sec * 1000);
  }

  _nearestInteractable() {
    const p = this.player.pos;
    let best = null, bestD = 3;
    for (const it of this.level.interactables) {
      const d = p.distanceTo(it.mesh.position);
      if (d < bestD) { bestD = d; best = it; }
    }
    // also pickupable throwables on ground
    if (!this.heldThrowable) {
      for (const t of this.throwables) {
        if (t.pickedUp || !t.onGround) continue;
        const d = p.distanceTo(t.mesh.position);
        if (d < bestD) { bestD = d; best = { type: 'throwable', ref: t, prompt: `[E] Pick up ${t.type}` }; }
      }
    }
    return best;
  }

  _tryInteract() {
    const it = this._nearestInteractable();
    if (!it) return;
    if (it.type === 'throwable') {
      it.ref.pickup();
      this.heldThrowable = it.ref;
      this._subtitle(`Picked up ${it.ref.type}. LMB hold to charge throw, release to throw, G to drop.`, 4);
    } else if (it.type === 'security') {
      if (this.camCooldown > 0) { this._subtitle('Cameras recharging...', 2); return; }
      this._enterCamera(0);
    } else if (it.type === 'power') {
      if (!it.data.active) {
        it.data.active = true;
        it.data.lever.rotation.x = -0.7;
        this.powerOn = true;
        this._subtitle('Power online. A truck key must be somewhere in the warehouse.', 5);
        // turn on warning lights to amber
        for (const l of this.level.lights) l.color.setHex(0xffcc88);
      }
    } else if (it.type === 'key') {
      this.hasKey = true;
      this.scene.remove(it.data.mesh);
      this.level.interactables = this.level.interactables.filter(x => x !== it);
      this._subtitle('Truck key acquired. Head to the yellow truck.', 5);
    } else if (it.type === 'ignition') {
      if (!this.powerOn) { this._subtitle('No power. Find the power panel in the yard.', 4); return; }
      if (!this.hasKey) { this._subtitle('Need the key — check the warehouse.', 4); return; }
      this._startEscape(it.data.truck);
    }
  }

  _enterCamera(idx) {
    this.viewingCam = idx;
    const node = this.level.cameraNodes[idx];
    this.secCam.position.copy(node.pos);
    this.secCam.lookAt(node.look);
    this._subtitle(`Viewing: ${node.name} — [1-${this.level.cameraNodes.length}] switch, [ESC] exit`, 5);
    this._camViewTime = 0;
  }
  _exitCamera() {
    this.viewingCam = -1;
    this.camCooldown = 20;
    this._subtitle('Back to body. Cameras recharging.', 3);
  }

  _throwHeld() {
    if (!this.heldThrowable) return;
    const force = 10 + this.throwCharge * 22;
    const fwd = new THREE.Vector3(-Math.sin(this.player.yaw) * Math.cos(this.player.pitch), Math.sin(this.player.pitch), -Math.cos(this.player.yaw) * Math.cos(this.player.pitch));
    const origin = this.player.pos.clone().add(fwd.clone().multiplyScalar(0.6)).add(V3(0, 0.3, 0));
    this.heldThrowable.throwIt(origin, fwd, force);
    this.heldThrowable = null;
    this.throwCharge = 0;
  }
  _dropHeld() {
    if (!this.heldThrowable) return;
    const origin = this.player.pos.clone().add(V3(0, 0.2, 0));
    const fwd = new THREE.Vector3(-Math.sin(this.player.yaw), 0, -Math.cos(this.player.yaw));
    this.heldThrowable.throwIt(origin, fwd, 1.5);
    this.heldThrowable = null;
    this.throwCharge = 0;
  }

  _onNoise(pos, radius) {
    for (const s of this.skeletons) s.hearNoise(pos, radius);
  }

  _startEscape(truck) {
    this.gameState = 'escaping';
    this._subtitle('Starting engine...', 2.5);
    audio.engineStart(truck.position);
    // turn on headlights
    for (const h of truck.userData.headlights) h.intensity = 4;
    // spawn final wave - bump all remaining enemies to combat / ambush
    for (const s of this.skeletons) {
      if (s.alive) {
        s.lastSeen = this.player.pos.clone();
        s.state = 'combat'; s.stateT = 0; s.fireCooldown = 0.5;
      }
    }
    // after 3s, drive away
    this._escapeStartT = this.time;
    this._escapeTruck = truck;
  }

  _updateEscape(dt) {
    const elapsed = this.time - this._escapeStartT;
    if (elapsed < 3) {
      // shake
      this.camera.position.x += (Math.random() - 0.5) * 0.03;
      this.camera.position.y += (Math.random() - 0.5) * 0.03;
    } else if (elapsed < 10) {
      // drive truck west
      this._escapeTruck.position.x -= dt * (3 + (elapsed - 3) * 1.2);
      // lock camera inside truck
      this.camera.position.set(this._escapeTruck.position.x - 1.2, this._escapeTruck.position.y + 2.1, this._escapeTruck.position.z);
      this.camera.lookAt(this._escapeTruck.position.x - 12, 1.8, this._escapeTruck.position.z);
    } else {
      this.gameState = 'won';
      this.running = false;
      document.getElementById('winScreen').classList.remove('hidden');
      document.exitPointerLock();
    }
  }

  _currentZone() {
    const p = this.player.pos;
    for (const z of this.level.zones) {
      if (p.x >= z.bounds.x1 && p.x <= z.bounds.x2 && p.z >= z.bounds.z1 && p.z <= z.bounds.z2) return z.name;
    }
    return '—';
  }

  _updateHUD(dt) {
    this.$.hp.style.width = this.player.hp + '%';
    this.$.hpnum.textContent = Math.round(this.player.hp);
    this.$.sta.style.width = this.player.stamina + '%';
    if (this.heldThrowable) {
      this.$.ammo.textContent = `${this.heldThrowable.type.toUpperCase()}`;
      document.getElementById('weapon').textContent = `THROW ${Math.round(this.throwCharge * 100)}%`;
    } else {
      this.$.ammo.textContent = `${this.pistol.ammoInMag} / ${this.pistol.reserve}`;
      document.getElementById('weapon').textContent = this.pistol.reloading ? 'RELOADING' : 'PISTOL';
    }
    const alive = this.skeletons.filter(s => s.alive).length;
    this.$.enemies.textContent = `ENEMIES: ${alive}`;
    this.$.zone.textContent = `ZONE: ${this._currentZone()}`;

    // interact prompt
    const it = this._nearestInteractable();
    if (it) { this.$.prompt.textContent = it.prompt; this.$.prompt.classList.remove('hidden'); }
    else this.$.prompt.classList.add('hidden');

    // blood vignette
    const want = 1 - this.player.hp / 100;
    this.$.blood.style.opacity = clamp(want * 0.9 + (this.player.hurtTimer > 0 ? 0.3 : 0), 0, 0.85);

    // directional damage
    if (this.player.hurtTimer > 0) {
      const d = this.player.lastHitDir;
      const camF = new THREE.Vector3(-Math.sin(this.player.yaw), 0, -Math.cos(this.player.yaw));
      const camR = new THREE.Vector3(Math.cos(this.player.yaw), 0, -Math.sin(this.player.yaw));
      const fDot = d.x * camF.x + d.z * camF.z;
      const rDot = d.x * camR.x + d.z * camR.z;
      this.$.dds.n.style.opacity = Math.max(0, fDot);
      this.$.dds.s.style.opacity = Math.max(0, -fDot);
      this.$.dds.e.style.opacity = Math.max(0, rDot);
      this.$.dds.w.style.opacity = Math.max(0, -rDot);
    } else {
      for (const k in this.$.dds) this.$.dds[k].style.opacity = 0;
    }

    // objective
    let obj = 'Neutralize the skeletons. Find the yellow truck.';
    if (alive === 0) obj = 'Silence. Reach the yellow truck.';
    if (this.powerOn && !this.hasKey) obj = 'Get the truck key from the warehouse.';
    if (this.hasKey) obj = 'Return to the truck and start the engine.';
    if (this.gameState === 'escaping') obj = 'HOLD ON!';
    this.$.objective.textContent = 'OBJECTIVE: ' + obj;
  }

  _updateMood() {
    const alive = this.skeletons.filter(s => s.alive);
    let minD = 200; let fighting = 0;
    for (const s of alive) {
      const d = Math.hypot(s.pos.x - this.player.pos.x, s.pos.z - this.player.pos.z);
      if (d < minD) minD = d;
      if (s.state === 'combat' || s.state === 'flank') fighting++;
    }
    const tension = clamp(1 - minD / 30, 0, 1);
    const combat = clamp(fighting / 3, 0, 1);
    audio.setMood({ tension, combat });
  }

  _flickerLamps(dt) {
    for (const l of this.level.flickerLamps) {
      l.next -= dt;
      if (l.next <= 0) {
        l.next = rand(0.05, 0.6);
        l.flicker = Math.random() < 0.25;
      }
      const k = l.flicker ? rand(0, 1.2) : 1;
      l.light.intensity = l.base * k;
      if (l.mesh && l.mesh.material) {
        l.mesh.material.emissiveIntensity = 1.2 * k;
      }
    }
  }

  _loop(nowMs) {
    if (!this.running) return;
    requestAnimationFrame((t) => this._loop(t));
    const now = nowMs / 1000;
    if (!this._lastT) this._lastT = now;
    const dt = Math.min(0.05, now - this._lastT);
    this._lastT = now;
    this.time += dt;

    this._tick(dt);
    this._render();
  }

  _tick(dt) {
    if (this.gameState === 'dead' || this.gameState === 'won') return;

    // handle camera view mode first
    if (this.viewingCam >= 0) {
      this._camViewTime += dt;
      if (this._camViewTime > 8) { this._exitCamera(); return; }
    }

    const escaping = this.gameState === 'escaping';

    // player (skip control if in escape cinematic)
    if (!escaping) this.player.update(dt, (p, r) => this._onNoise(p, r));

    // audio listener
    audio.setListener(this.player.pos, this.player.forward);

    // death
    if (this.player.hp <= 0) {
      this.gameState = 'dead'; this.running = false;
      document.getElementById('deathScreen').classList.remove('hidden');
      document.exitPointerLock();
      return;
    }

    // fire / throw charge
    if (this.mouseDown) {
      if (this.heldThrowable) {
        this.throwCharge = Math.min(1, this.throwCharge + dt * 0.9);
      } else {
        const fired = this.pistol.fire(this.walls, this.skeletons, (hit) => {
          this.analyzer.onPlayerShot(this.player);
          this._onNoise(this.player.pos, 28);  // gunshot noise heard far
          if (hit && hit.type === 'enemy') {
            this._subtitle(hit.enemy.alive ? 'HIT' : 'KILL', 1.2);
          }
          // muzzle flash DOM
          this.$.flash.style.opacity = 0.35;
          setTimeout(() => { this.$.flash.style.opacity = 0; }, 50);
        });
      }
    }

    // pistol animate
    this.pistol.update(dt, this.rmbDown && !this.heldThrowable);

    // throwables physics
    for (const t of this.throwables) t.update(dt, this.walls, (p, r) => this._onNoise(p, r));
    if (this.heldThrowable) {
      // move it to hand pos for visual in pov? We'll hide it since it's conceptually in hand.
    }

    // arrows
    for (const a of this.arrows) a.update(dt, this.walls, this.player);
    this.arrows = this.arrows.filter(a => !a.dead);

    // AI
    this.analyzer.sample(this.player, dt);
    this.coordinator.update(dt, this.player, this.analyzer);
    for (const s of this.skeletons) s.aiUpdate(dt, this.player, this.analyzer, this.coordinator, this.arrows);

    // mood / music
    this._updateMood();
    this._flickerLamps(dt);
    this.dust.update(dt, this.camera);

    // security room cooldown
    if (this.camCooldown > 0) this.camCooldown -= dt;

    // postfx uniforms
    this.grade.uniforms.uTime.value = this.time;
    this.grade.uniforms.uHurt.value = Math.max(0, this.player.hurtTimer);

    // escape sequence
    if (this.gameState === 'escaping') this._updateEscape(dt);

    this._updateHUD(dt);
  }

  _render() {
    if (this.viewingCam >= 0) {
      // render from security camera
      this.composer.passes[0].camera = this.secCam;
      this.composer.render();
    } else {
      this.composer.passes[0].camera = this.camera;
      this.composer.render();
    }
  }
}
