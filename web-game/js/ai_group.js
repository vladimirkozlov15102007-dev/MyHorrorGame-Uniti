// Group-level AI: analyzes player behavior and issues tactical orders
// (flank, ambush, suppress) to individual skeletons.

import { V3 } from './util.js';

export class PlayerBehaviorAnalyzer {
  constructor() {
    this.samplePositions = [];     // recent player positions
    this.shotsByZone = {};         // how often player shoots from same spot
    this.totalShots = 0;
    this.totalMoves = 0;
    this.lastPos = null;
    this.movedDist = 0;
    this.timeStationary = 0;

    // derived flags (read by skeletons)
    this.playerAggressive = false;   // shoots a lot
    this.playerStaysInCover = false; // stationary + behind walls
    this.playerRushes = false;       // moves fast
    this.playerHotspot = null;       // V3 of frequent shooting spot

    this.tick = 0;
  }

  sample(player, dt) {
    this.tick += dt;
    if (this.lastPos) {
      const d = Math.hypot(player.pos.x - this.lastPos.x, player.pos.z - this.lastPos.z);
      this.movedDist += d;
      if (d < 0.2 * dt) this.timeStationary += dt;
      else this.timeStationary = 0;
    }
    this.lastPos = player.pos.clone();

    this.samplePositions.push({ t: this.tick, p: player.pos.clone() });
    while (this.samplePositions.length > 0 && this.tick - this.samplePositions[0].t > 10) {
      this.samplePositions.shift();
    }

    const recent = this.samplePositions;
    let avg = V3(0, 0, 0);
    for (const s of recent) avg.add(s.p);
    avg.multiplyScalar(1 / Math.max(1, recent.length));
    let variance = 0;
    for (const s of recent) variance += s.p.distanceToSquared(avg);
    variance /= Math.max(1, recent.length);

    // decide
    this.playerStaysInCover = variance < 4 && this.timeStationary > 2.5;
    this.playerRushes = player.sprint && this.movedDist > 15;
    this.playerAggressive = this.totalShots > 4 && this.tick % 6 < 1;

    // hotspot = avg shooting position weighted
    if (this.totalShots > 3) {
      let best = null, bestN = 0;
      for (const k in this.shotsByZone) {
        if (this.shotsByZone[k].n > bestN) { bestN = this.shotsByZone[k].n; best = this.shotsByZone[k].p; }
      }
      this.playerHotspot = best;
    }
  }

  onPlayerShot(player) {
    this.totalShots++;
    const key = `${Math.round(player.pos.x / 4)}_${Math.round(player.pos.z / 4)}`;
    if (!this.shotsByZone[key]) this.shotsByZone[key] = { p: player.pos.clone(), n: 0 };
    this.shotsByZone[key].n++;
  }
}

export class GroupCoordinator {
  constructor(skeletons) {
    this.skeletons = skeletons;
    this.lastSighting = null;       // { pos, time }
    this.time = 0;
    this.nextFlankAt = 0;
    this.nextAmbushAt = 0;
  }

  reportSighting(id, pos) {
    this.lastSighting = { pos: pos.clone(), time: this.time, id };
    // broadcast to nearby idle skeletons
    for (const s of this.skeletons) {
      if (!s.alive || s.id === id) continue;
      const d = Math.hypot(pos.x - s.pos.x, pos.z - s.pos.z);
      if (d < 30 && (s.state === 'patrol' || s.state === 'search')) {
        s.lastSeen = pos.clone();
        s.state = 'alert';
        s.stateT = 0;
      }
    }
  }

  update(dt, player, analyzer) {
    this.time += dt;
    if (!this.lastSighting) return;
    if (this.time - this.lastSighting.time > 8) return; // sighting stale

    const alive = this.skeletons.filter(s => s.alive);
    if (alive.length === 0) return;

    const inCombat = alive.filter(s => s.state === 'combat' || s.state === 'flank' || s.state === 'ambush');

    // ---- FLANK: if player stays in cover, order 1-2 to flank
    if (analyzer.playerStaysInCover && this.time > this.nextFlankAt && inCombat.length > 0) {
      this.nextFlankAt = this.time + 10;
      const toFlank = inCombat.filter(s => s.state === 'combat').slice(0, 2);
      let dir = -1;
      for (const s of toFlank) {
        s.state = 'flank';
        s.stateT = 0;
        s.flankDir = dir;
        dir = -dir;
      }
    }

    // ---- AMBUSH: if player rushes, send one ahead to ambush
    if (analyzer.playerRushes && this.time > this.nextAmbushAt) {
      this.nextAmbushAt = this.time + 12;
      const patrollers = alive.filter(s => s.state === 'patrol' || s.state === 'search');
      if (patrollers.length > 0) {
        const s = patrollers[0];
        s.state = 'ambush';
        s.stateT = 0;
        s.lastSeen = player.pos.clone();
      }
    }

    // ---- SUPPRESS: if player often shoots from same hotspot
    if (analyzer.playerHotspot) {
      for (const s of inCombat) s.suppress = true;
    } else {
      for (const s of inCombat) s.suppress = false;
    }

    // ---- GROUP ATTACK: if player aggressive, keep distance and coordinate shots
    if (analyzer.playerAggressive && inCombat.length >= 2) {
      for (const s of inCombat) s.state = 'combat'; // already, but forces keep_distance logic
    }
  }
}
