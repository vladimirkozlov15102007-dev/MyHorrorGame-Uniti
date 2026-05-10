// Procedurally build the full factory: 5 zones + yellow truck.
// Returns { group, walls (AABB list), spawnPoints, truck, zones, lights, flickerLamps, crtGroup, cameraNodes, interactables }

import * as THREE from 'three';
import { materials } from './materials.js';
import { makeAABB, V3, rand, randi } from './util.js';

// ---------- helpers ----------
function makeBox(w, h, d, mat) {
  return new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
}
function flat(w, d, mat) {
  return new THREE.Mesh(new THREE.PlaneGeometry(w, d), mat);
}

// ---------- props ----------
function barrel(mat) {
  const g = new THREE.Group();
  const body = new THREE.Mesh(new THREE.CylinderGeometry(0.45, 0.45, 1.1, 18, 1), mat);
  body.castShadow = body.receiveShadow = true;
  g.add(body);
  const ringGeo = new THREE.TorusGeometry(0.45, 0.03, 6, 20);
  for (const y of [-0.45, 0, 0.45]) {
    const r = new THREE.Mesh(ringGeo, mat);
    r.rotation.x = Math.PI / 2; r.position.y = y; g.add(r);
  }
  return g;
}
function crate(mat, size = 0.9) {
  const g = new THREE.Group();
  const body = new THREE.Mesh(new THREE.BoxGeometry(size, size * 0.9, size), mat);
  body.castShadow = body.receiveShadow = true;
  g.add(body);
  // plank lines (subtle geometry)
  const line = new THREE.Mesh(new THREE.BoxGeometry(size + 0.01, 0.05, 0.04), materials().dark);
  for (const y of [-size * 0.25, size * 0.25]) {
    const a = line.clone(); a.position.y = y; g.add(a);
    const b = line.clone(); b.position.y = y; b.rotation.y = Math.PI / 2; g.add(b);
  }
  return g;
}
function shelf(mats) {
  const g = new THREE.Group();
  const post = new THREE.BoxGeometry(0.1, 4.5, 0.1);
  for (const x of [-1.2, 1.2]) for (const z of [-0.5, 0.5]) {
    const p = new THREE.Mesh(post, mats.rustyMetal);
    p.position.set(x, 2.25, z); p.castShadow = true; g.add(p);
  }
  const shelfGeo = new THREE.BoxGeometry(2.6, 0.06, 1.1);
  for (let i = 0; i < 4; i++) {
    const s = new THREE.Mesh(shelfGeo, mats.wood);
    s.position.y = 0.4 + i * 1.1; s.receiveShadow = true; g.add(s);
    if (Math.random() < 0.7) {
      const box = crate(mats.wood, rand(0.4, 0.7));
      box.position.set(rand(-0.8, 0.8), s.position.y + 0.3, rand(-0.3, 0.3));
      g.add(box);
    }
  }
  return g;
}
function pipe(length, mat, radius = 0.1) {
  const g = new THREE.Mesh(new THREE.CylinderGeometry(radius, radius, length, 10), mat);
  g.castShadow = true;
  return g;
}
function chain(length, mat) {
  const g = new THREE.Group();
  const links = Math.floor(length / 0.15);
  for (let i = 0; i < links; i++) {
    const l = new THREE.Mesh(new THREE.TorusGeometry(0.06, 0.018, 4, 8), mat);
    l.position.y = -i * 0.12;
    l.rotation.x = (i % 2) * Math.PI / 2;
    g.add(l);
  }
  return g;
}
function flickerLampMesh(mats) {
  const g = new THREE.Group();
  const tube = new THREE.Mesh(new THREE.BoxGeometry(1.4, 0.08, 0.2), mats.lampOn);
  tube.castShadow = false;
  const cage = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.15, 0.25), mats.rustyMetal);
  cage.position.y = 0.05;
  g.add(cage); g.add(tube);
  g.userData.tube = tube;
  return g;
}
function crtMonitor(mats) {
  const g = new THREE.Group();
  const body = new THREE.Mesh(new THREE.BoxGeometry(0.8, 0.6, 0.7), mats.dark);
  body.castShadow = body.receiveShadow = true;
  g.add(body);
  const screen = new THREE.Mesh(new THREE.PlaneGeometry(0.6, 0.42), mats.crt);
  screen.position.z = 0.36; screen.position.y = 0.03;
  g.add(screen);
  g.userData.screen = screen;
  return g;
}
function conveyor(length, mats) {
  const g = new THREE.Group();
  const belt = new THREE.Mesh(new THREE.BoxGeometry(length, 0.1, 1.2), mats.rubber);
  belt.position.y = 0.95; belt.castShadow = belt.receiveShadow = true; g.add(belt);
  const frameGeo = new THREE.BoxGeometry(length + 0.2, 0.08, 0.15);
  for (const z of [-0.65, 0.65]) {
    const f = new THREE.Mesh(frameGeo, mats.rustyMetal); f.position.set(0, 0.9, z); g.add(f);
  }
  const legGeo = new THREE.BoxGeometry(0.1, 0.9, 0.1);
  for (let i = -length / 2 + 0.5; i <= length / 2 - 0.5; i += 2) {
    const l1 = new THREE.Mesh(legGeo, mats.rustyMetal); l1.position.set(i, 0.45, -0.6); g.add(l1);
    const l2 = l1.clone(); l2.position.z = 0.6; g.add(l2);
  }
  return g;
}
function pressMachine(mats) {
  const g = new THREE.Group();
  const base = new THREE.Mesh(new THREE.BoxGeometry(2.2, 1, 2), mats.rustyMetal);
  base.position.y = 0.5; base.castShadow = base.receiveShadow = true; g.add(base);
  const col1 = new THREE.Mesh(new THREE.BoxGeometry(0.3, 3, 0.3), mats.rustyMetal);
  col1.position.set(-0.9, 2.5, 0); g.add(col1);
  const col2 = col1.clone(); col2.position.x = 0.9; g.add(col2);
  const top = new THREE.Mesh(new THREE.BoxGeometry(2.2, 0.5, 1.2), mats.rustyMetal);
  top.position.y = 4; g.add(top);
  const piston = new THREE.Mesh(new THREE.BoxGeometry(1.2, 1.2, 1), mats.dark);
  piston.position.y = 2.5; g.add(piston);
  return g;
}

// ---------- main builder ----------
export function buildLevel(scene, renderer) {
  const mats = materials();
  const group = new THREE.Group();
  scene.add(group);

  const walls = [];           // collision AABBs
  const zones = [];           // { name, center, radius }
  const flickerLamps = [];    // lights that flicker
  const lights = [];          // all lights (for debug / mood)
  const interactables = [];   // { mesh, type, data }
  const cameraNodes = [];     // for security room

  // =====================================================================
  // GLOBAL GROUND: outdoor around & inside halls handled per zone
  // We keep a single large ground plane for the outdoor yard
  // and interior concrete floors per zone.
  // =====================================================================

  // ---- helper: add wall with collision ----
  function wall(cx, cz, sx, sz, h = 4, mat = mats.wall, y = 0, tag = 'wall') {
    const m = new THREE.Mesh(new THREE.BoxGeometry(sx, h, sz), mat);
    m.position.set(cx, y + h / 2, cz);
    m.castShadow = true; m.receiveShadow = true;
    group.add(m);
    walls.push(makeAABB(cx, cz, sx, sz, y, h, tag));
    return m;
  }
  function floor(cx, cz, sx, sz, mat = mats.concreteFloor, y = 0) {
    const m = new THREE.Mesh(new THREE.BoxGeometry(sx, 0.2, sz), mat);
    m.position.set(cx, y - 0.1, cz);
    m.receiveShadow = true;
    group.add(m);
  }
  function ceiling(cx, cz, sx, sz, mat = mats.concrete, y = 4) {
    const m = new THREE.Mesh(new THREE.BoxGeometry(sx, 0.2, sz), mat);
    m.position.set(cx, y, cz);
    m.receiveShadow = true;
    group.add(m);
  }

  // ------------------------------------------------------------------
  // ZONE 1 — ADMIN BUILDING  (x: -40..-10, z: -14..14)
  // Narrow corridors, security room, offices.
  // ------------------------------------------------------------------
  zones.push({ name: 'Admin', bounds: { x1: -42, x2: -8, z1: -16, z2: 16 } });
  floor(-25, 0, 34, 32, mats.wood);
  ceiling(-25, 0, 34, 32, mats.concrete, 3.2);
  // outer walls
  wall(-42, 0, 0.6, 32);                    // west
  // east wall (admin -> factory) with 3m doorway at z=0
  wall(-8, -9.5, 0.6, 13);
  wall(-8, 9.5, 0.6, 13);
  wall(-25, -16, 34, 0.6);                  // south
  wall(-25, 16, 34, 0.6);                   // north
  // corridor partitions at z=-6  (gap at x=-25 to -23)
  wall(-32.5, -6, 7, 0.4);
  wall(-18.5, -6, 9, 0.4);
  wall(-24, -12, 0.4, 8);
  // corridor partitions at z=+6  (gap at x=-25 to -23)
  wall(-32.5, 6, 7, 0.4);
  wall(-18.5, 6, 9, 0.4);
  // security room (the player's spawn) at approx (-38, 0, 12)
  wall(-33, 11.5, 0.4, 7);       // east wall of security room (gap at z=7 to z=8)
  // south wall of security room (x=-43..-33) with 2m doorway at x=-38
  wall(-41, 6, 4, 0.4);
  wall(-35, 6, 4, 0.4);
  // office rooms
  wall(-36, -12, 0.4, 7);
  wall(-18, -12, 0.4, 7);
  wall(-36, 12, 0.4, 6);
  // flicker fluorescent lamps along admin corridor
  const corridorYs = [-9, -3, 3, 9];
  for (const z of corridorYs) {
    const lamp = flickerLampMesh(mats);
    lamp.position.set(-25, 3.1, z);
    group.add(lamp);
    const pl = new THREE.PointLight(0xffeecc, 1.8, 9, 1.5);
    pl.position.set(-25, 3, z);
    pl.castShadow = true;
    pl.shadow.mapSize.set(256, 256);
    group.add(pl);
    flickerLamps.push({ light: pl, mesh: lamp.userData.tube, base: 1.8, next: 0, flicker: false });
  }
  // CRT monitor bank in security room
  const crtGroup = new THREE.Group();
  for (let i = 0; i < 4; i++) {
    const crt = crtMonitor(mats);
    crt.position.set(-39 + i * 0.85, 1.4, 14);
    crt.rotation.y = Math.PI;   // face south
    crtGroup.add(crt);
  }
  group.add(crtGroup);
  // interact with CRT bank
  const crtInteract = new THREE.Mesh(new THREE.BoxGeometry(3.5, 1.2, 0.6), new THREE.MeshBasicMaterial({ visible: false }));
  crtInteract.position.set(-37.5, 1.4, 13.5);
  group.add(crtInteract);
  interactables.push({ mesh: crtInteract, type: 'security', prompt: '[E] Use security cameras' });
  // desks
  for (let i = 0; i < 6; i++) {
    const desk = makeBox(1.2, 0.08, 0.7, mats.wood);
    desk.position.set(rand(-36, -12), 0.75, rand(-13, 13));
    // avoid corridor path
    if (Math.abs(desk.position.z) < 2) desk.position.z = rand(-13, -7) * Math.sign(desk.position.z || 1);
    desk.castShadow = desk.receiveShadow = true;
    group.add(desk);
    // legs
    const legGeo = new THREE.BoxGeometry(0.05, 0.75, 0.05);
    for (const dx of [-0.5, 0.5]) for (const dz of [-0.3, 0.3]) {
      const leg = new THREE.Mesh(legGeo, mats.wood);
      leg.position.set(desk.position.x + dx, 0.38, desk.position.z + dz);
      group.add(leg);
    }
    if (Math.random() < 0.6) {
      const mon = crtMonitor(mats);
      mon.position.set(desk.position.x, 1.1, desk.position.z);
      mon.rotation.y = rand(0, Math.PI * 2);
      group.add(mon);
    }
  }

  // ------------------------------------------------------------------
  // ZONE 2 — MAIN FACTORY HALL (x: -8..22, z: -18..20)  big open hall
  // ------------------------------------------------------------------
  zones.push({ name: 'Factory Hall', bounds: { x1: -8, x2: 22, z1: -18, z2: 20 } });
  floor(7, 1, 30, 38, mats.concreteFloor);
  ceiling(7, 1, 30, 38, mats.concrete, 12);
  // south wall with garage door opening handled below (outdoor zone)
  // north wall split with doorway (at x=14.5) so player can enter vents
  wall(0, 20, 16, 0.6, 12);
  wall(20, 20, 4, 0.6, 12);
  // east wall (to warehouse) split with doorway at z=1
  wall(22, -9.5, 0.6, 17, 12);
  wall(22, 11.5, 0.6, 17, 12);
  // broken window panels high up
  for (let i = 0; i < 8; i++) {
    const w = new THREE.Mesh(new THREE.PlaneGeometry(2.4, 1.4), mats.glass);
    w.position.set(7 - 14 + i * 4, 9, -17.7); w.rotation.y = 0;
    group.add(w);
  }
  // conveyors (long)
  const conv1 = conveyor(14, mats); conv1.position.set(4, 0, -8); group.add(conv1);
  const conv2 = conveyor(10, mats); conv2.position.set(12, 0, 6); conv2.rotation.y = Math.PI / 2; group.add(conv2);
  walls.push(makeAABB(4, -8, 14.5, 1.4, 0.8, 0.3, 'cover'));
  walls.push(makeAABB(12, 6, 1.4, 10.5, 0.8, 0.3, 'cover'));
  // press machines
  const press1 = pressMachine(mats); press1.position.set(-3, 0, 10); group.add(press1);
  const press2 = pressMachine(mats); press2.position.set(18, 0, -12); group.add(press2);
  walls.push(makeAABB(-3, 10, 2.2, 2, 0, 4.5, 'cover'));
  walls.push(makeAABB(18, -12, 2.2, 2, 0, 4.5, 'cover'));
  // overhead catwalk
  const catwalkMat = mats.rustyMetal;
  const cw = new THREE.Mesh(new THREE.BoxGeometry(28, 0.2, 2), catwalkMat);
  cw.position.set(7, 6, 14); cw.receiveShadow = cw.castShadow = true; group.add(cw);
  // catwalk handrails
  for (const side of [-1, 1]) {
    const rail = new THREE.Mesh(new THREE.BoxGeometry(28, 0.08, 0.05), catwalkMat);
    rail.position.set(7, 6.9, 14 + side * 0.95); group.add(rail);
    for (let x = -7; x <= 7; x += 2) {
      const post = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.9, 0.05), catwalkMat);
      post.position.set(7 + x, 6.45, 14 + side * 0.95); group.add(post);
    }
  }
  // columns
  for (let i = 0; i < 4; i++) {
    const col = new THREE.Mesh(new THREE.CylinderGeometry(0.4, 0.5, 12, 14), mats.concrete);
    col.position.set(-2 + i * 6, 6, -2);
    col.castShadow = col.receiveShadow = true;
    group.add(col);
    walls.push(makeAABB(-2 + i * 6, -2, 1, 1, 0, 12, 'wall'));
  }
  // hanging chains with small crates
  for (let i = 0; i < 5; i++) {
    const c = chain(3.5, mats.rustyMetal);
    c.position.set(rand(-3, 17), 10, rand(-6, 4));
    group.add(c);
    const hook = new THREE.Mesh(new THREE.TorusGeometry(0.12, 0.04, 6, 10), mats.rustyMetal);
    hook.position.copy(c.position); hook.position.y -= 3.7;
    group.add(hook);
  }
  // main hall lights (high sodium)
  for (let i = 0; i < 4; i++) {
    const pl = new THREE.PointLight(0xffc48a, 2.4, 28, 1.4);
    pl.position.set(-2 + i * 6, 10, 2);
    pl.castShadow = true;
    pl.shadow.mapSize.set(512, 512);
    group.add(pl);
    lights.push(pl);
    // lamp geom
    const sphere = new THREE.Mesh(new THREE.SphereGeometry(0.22, 10, 10), mats.amber);
    sphere.position.copy(pl.position); group.add(sphere);
  }
  // camera nodes for security view
  cameraNodes.push({ name: 'Hall A', pos: V3(-2, 10, -3), look: V3(10, 0, 5) });
  cameraNodes.push({ name: 'Hall B', pos: V3(20, 10, 18), look: V3(5, 0, 2) });

  // ------------------------------------------------------------------
  // ZONE 3 — WAREHOUSE  (x: 22..48, z: -14..16)
  // Tall shelves, tight aisles — perfect for ambushes.
  // ------------------------------------------------------------------
  zones.push({ name: 'Warehouse', bounds: { x1: 22, x2: 50, z1: -16, z2: 18 } });
  floor(36, 1, 28, 32, mats.concreteFloor);
  ceiling(36, 1, 28, 32, mats.concrete, 6);
  wall(50, 1, 0.6, 32, 6);
  wall(36, -14, 28, 0.6, 6);
  wall(36, 18, 28, 0.6, 6);
  // shelf rows
  for (let rz = -10; rz <= 14; rz += 6) {
    for (let rx = 26; rx <= 46; rx += 3) {
      if (Math.random() < 0.82) {
        const s = shelf(mats);
        s.position.set(rx, 0, rz);
        group.add(s);
        walls.push(makeAABB(rx, rz, 2.6, 1.1, 0, 4.5, 'cover'));
      }
    }
  }
  // barrels scattered
  for (let i = 0; i < 14; i++) {
    const b = barrel(mats.rustyMetal);
    b.position.set(rand(24, 48), 0.55, rand(-13, 15));
    group.add(b);
  }
  // single amber/red warning light
  const warnLight = new THREE.PointLight(0xff5544, 1.6, 20, 1.5);
  warnLight.position.set(36, 5.6, 1); group.add(warnLight); lights.push(warnLight);
  cameraNodes.push({ name: 'Warehouse', pos: V3(36, 5.4, 1), look: V3(36, 0, 0) });

  // ------------------------------------------------------------------
  // ZONE 4 — VENT TUNNELS (x: 7..22, z: 20..34)  low ceiling
  // ------------------------------------------------------------------
  zones.push({ name: 'Vent Tunnels', bounds: { x1: 7, x2: 22, z1: 20, z2: 34 } });
  floor(14.5, 27, 15, 14, mats.rustyMetalFine);
  ceiling(14.5, 27, 15, 14, mats.rustyMetalFine, 2.2);
  wall(22, 27, 0.3, 14, 2.2, mats.rustyMetalFine);
  wall(7, 27, 0.3, 14, 2.2, mats.rustyMetalFine);
  wall(14.5, 34, 15, 0.3, 2.2, mats.rustyMetalFine);
  // (south edge at z=20 intentionally open to the factory hall doorway)
  // vertical pipes / wall dividers
  for (let i = 0; i < 6; i++) {
    const p = pipe(2.2, mats.rustyMetal, 0.18);
    p.position.set(rand(8, 21), 1.1, rand(21, 33));
    group.add(p);
  }
  // emergency lights
  for (let i = 0; i < 3; i++) {
    const el = new THREE.PointLight(0xff3322, 1.2, 10, 1.8);
    el.position.set(10 + i * 5, 2, 23 + (i % 2) * 6);
    group.add(el);
  }
  cameraNodes.push({ name: 'Vents', pos: V3(14.5, 2.1, 27), look: V3(14.5, 0, 30) });

  // ------------------------------------------------------------------
  // ZONE 5 — OUTDOOR YARD (x: -20..40, z: -50..-20) with yellow truck
  // ------------------------------------------------------------------
  zones.push({ name: 'Outdoor Yard', bounds: { x1: -22, x2: 42, z1: -55, z2: -18 } });
  // Big ground
  const groundGeo = new THREE.PlaneGeometry(120, 120, 40, 40);
  // displace vertices for uneven terrain
  const gp = groundGeo.attributes.position;
  for (let i = 0; i < gp.count; i++) {
    const x = gp.getX(i), y = gp.getY(i);
    const h = (Math.sin(x * 0.15) * 0.25 + Math.cos(y * 0.2) * 0.2 + (Math.random() - 0.5) * 0.1);
    gp.setZ(i, h);
  }
  groundGeo.computeVertexNormals();
  const ground = new THREE.Mesh(groundGeo, mats.asphalt);
  ground.rotation.x = -Math.PI / 2; ground.position.set(10, 0, -35);
  ground.receiveShadow = true;
  group.add(ground);

  // perimeter fence (AABB walls only, visual as posts & wire)
  const fenceMat = mats.rustyMetal;
  // south fence
  for (let x = -30; x <= 50; x += 3) {
    const post = new THREE.Mesh(new THREE.BoxGeometry(0.08, 2.4, 0.08), fenceMat);
    post.position.set(x, 1.2, -55); group.add(post);
  }
  for (let i = 0; i < 3; i++) {
    const wire = new THREE.Mesh(new THREE.BoxGeometry(80, 0.02, 0.02), fenceMat);
    wire.position.set(10, 0.5 + i * 0.6, -55); group.add(wire);
  }
  walls.push(makeAABB(10, -55, 80, 0.4, 0, 2.4, 'wall'));
  // west / east fences
  for (let z = -54; z <= -20; z += 3) {
    const p = new THREE.Mesh(new THREE.BoxGeometry(0.08, 2.4, 0.08), fenceMat);
    p.position.set(-30, 1.2, z); group.add(p);
    const p2 = p.clone(); p2.position.x = 50; group.add(p2);
  }
  walls.push(makeAABB(-30, -37, 0.4, 36, 0, 2.4, 'wall'));
  walls.push(makeAABB(50, -37, 0.4, 36, 0, 2.4, 'wall'));

  // back side of main factory (wall = factory south edge + outdoor north edge)
  // 3m-wide garage doorway gap at x=10.5 (from x=9 to x=12)
  wall(-2, -18, 14, 0.6, 6);      // left of garage door (covers x=-9 to 5)
  wall(15, -18, 10, 0.6, 6);      // right of garage door (covers x=10 to 20)
  wall(25, -18, 10, 0.6, 6);      // far east warehouse south wall (x=20 to 30)

  // ----- YELLOW TRUCK (KAMAZ-ish) -----
  const truck = new THREE.Group();
  truck.position.set(15, 0, -42);
  truck.rotation.y = -Math.PI / 2;

  // chassis
  const chassis = new THREE.Mesh(new THREE.BoxGeometry(5, 0.3, 2.2), mats.dark);
  chassis.position.y = 0.7; truck.add(chassis);
  // cabin
  const cabin = new THREE.Mesh(new THREE.BoxGeometry(2, 1.7, 2.1), mats.truck);
  cabin.position.set(-1.2, 1.75, 0); cabin.castShadow = true; cabin.receiveShadow = true;
  truck.add(cabin);
  // cabin windows
  const win = new THREE.Mesh(new THREE.PlaneGeometry(1.7, 0.7), mats.glass);
  win.position.set(-1.2, 2.2, 1.06); truck.add(win);
  const winB = win.clone(); winB.position.z = -1.06; winB.rotation.y = Math.PI; truck.add(winB);
  const winS = new THREE.Mesh(new THREE.PlaneGeometry(1.7, 0.7), mats.glass);
  winS.position.set(-0.2, 2.2, 0); winS.rotation.y = Math.PI / 2; truck.add(winS);
  // hood
  const hood = new THREE.Mesh(new THREE.BoxGeometry(1.8, 0.9, 2), mats.truck);
  hood.position.set(-3.1, 1.15, 0); truck.add(hood);
  // bed (cargo)
  const bed = new THREE.Mesh(new THREE.BoxGeometry(2.5, 1.2, 2.1), mats.truck);
  bed.position.set(0.9, 1.5, 0); truck.add(bed);
  // wheels
  const wheelGeo = new THREE.CylinderGeometry(0.55, 0.55, 0.45, 14);
  for (const wx of [-2.6, 1.4]) for (const wz of [-1, 1]) {
    const w = new THREE.Mesh(wheelGeo, mats.dark);
    w.position.set(wx, 0.55, wz); w.rotation.z = Math.PI / 2; truck.add(w);
  }
  // headlights (geometry)
  const hL = new THREE.Mesh(new THREE.CylinderGeometry(0.18, 0.18, 0.12, 10), mats.amber);
  hL.rotation.z = Math.PI / 2; hL.position.set(-4, 1.2, 0.6); truck.add(hL);
  const hR = hL.clone(); hR.position.z = -0.6; truck.add(hR);
  // add spot lights (off at start)
  const spotL = new THREE.SpotLight(0xffeeaa, 0, 35, Math.PI / 7, 0.5, 1.3);
  spotL.position.set(-4, 1.2, 0.6); truck.add(spotL);
  const spotLT = new THREE.Object3D(); spotLT.position.set(-20, 0, 0.6); truck.add(spotLT); spotL.target = spotLT;
  const spotR = new THREE.SpotLight(0xffeeaa, 0, 35, Math.PI / 7, 0.5, 1.3);
  spotR.position.set(-4, 1.2, -0.6); truck.add(spotR);
  const spotRT = new THREE.Object3D(); spotRT.position.set(-20, 0, -0.6); truck.add(spotRT); spotR.target = spotRT;
  truck.userData.headlights = [spotL, spotR];

  group.add(truck);
  walls.push(makeAABB(truck.position.x, truck.position.z, 6, 3, 0, 2.5, 'truck'));

  // ----- Power switch (red panel) -----
  const panel = new THREE.Group();
  const pb = new THREE.Mesh(new THREE.BoxGeometry(0.6, 0.8, 0.2), mats.rustyMetal);
  pb.position.y = 1.2; panel.add(pb);
  const lever = new THREE.Mesh(new THREE.BoxGeometry(0.12, 0.35, 0.08), mats.wood);
  lever.position.set(0, 1.2, 0.18); panel.add(lever);
  panel.position.set(-18, 0, -38);
  group.add(panel);
  const panelHit = new THREE.Mesh(new THREE.BoxGeometry(1.2, 2, 1.2), new THREE.MeshBasicMaterial({ visible: false }));
  panelHit.position.set(-18, 1, -38);
  group.add(panelHit);
  interactables.push({ mesh: panelHit, type: 'power', prompt: '[E] Activate power', data: { panel, lever, active: false } });

  // ----- Key pickup inside warehouse -----
  const keyGroup = new THREE.Group();
  const keyBase = new THREE.Mesh(new THREE.TorusGeometry(0.08, 0.02, 6, 12), mats.amber);
  keyBase.rotation.x = Math.PI / 2; keyGroup.add(keyBase);
  const keyTeeth = new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.03, 0.03), mats.amber);
  keyTeeth.position.z = 0.12; keyGroup.add(keyTeeth);
  keyGroup.position.set(44, 1.3, 12);
  group.add(keyGroup);
  const keyHit = new THREE.Mesh(new THREE.BoxGeometry(1, 1, 1), new THREE.MeshBasicMaterial({ visible: false }));
  keyHit.position.copy(keyGroup.position);
  group.add(keyHit);
  interactables.push({ mesh: keyHit, type: 'key', prompt: '[E] Take truck key', data: { mesh: keyGroup } });

  // ----- Ignition on truck (interactable once power + key) -----
  const ignHit = new THREE.Mesh(new THREE.BoxGeometry(2, 2.5, 2.5), new THREE.MeshBasicMaterial({ visible: false }));
  ignHit.position.copy(truck.position);
  ignHit.position.x -= 1.5;
  group.add(ignHit);
  interactables.push({ mesh: ignHit, type: 'ignition', prompt: '[E] Start truck', data: { truck } });

  // junk props in yard (cars, containers, bricks)
  for (let i = 0; i < 6; i++) {
    const container = new THREE.Mesh(new THREE.BoxGeometry(rand(4, 6), 2.5, 2.5), mats.truck);
    container.material = mats.rustyMetal;
    container.position.set(rand(-25, 45), 1.25, rand(-50, -22));
    // avoid truck
    if (Math.hypot(container.position.x - 15, container.position.z + 42) < 6) continue;
    container.castShadow = container.receiveShadow = true;
    group.add(container);
    walls.push(makeAABB(container.position.x, container.position.z, container.geometry.parameters.width, 2.5, 0, 2.5, 'cover'));
  }
  for (let i = 0; i < 14; i++) {
    const b = barrel(mats.rustyMetal);
    b.position.set(rand(-25, 45), 0.55, rand(-52, -22));
    if (Math.hypot(b.position.x - 15, b.position.z + 42) < 4) continue;
    group.add(b);
  }
  // spotlight on a pole
  const pole = new THREE.Mesh(new THREE.BoxGeometry(0.15, 6, 0.15), mats.rustyMetal);
  pole.position.set(-15, 3, -28); group.add(pole);
  const spot = new THREE.SpotLight(0xccddff, 2.5, 35, Math.PI / 6, 0.35, 1.2);
  spot.position.set(-15, 6, -28); spot.target.position.set(15, 0, -42);
  spot.castShadow = true; spot.shadow.mapSize.set(512, 512);
  group.add(spot); group.add(spot.target);

  // tall grass clusters in yard
  const grassGeo = new THREE.PlaneGeometry(0.25, 0.55);
  const grassMat = new THREE.MeshStandardMaterial({ color: 0x3a4223, side: THREE.DoubleSide, roughness: 1 });
  const grassInst = new THREE.InstancedMesh(grassGeo, grassMat, 400);
  const m4 = new THREE.Matrix4(), q = new THREE.Quaternion(), euler = new THREE.Euler();
  for (let i = 0; i < 400; i++) {
    euler.set(0, Math.random() * Math.PI * 2, 0);
    q.setFromEuler(euler);
    const x = rand(-28, 48), z = rand(-54, -22);
    m4.compose(new THREE.Vector3(x, 0.28, z), q, new THREE.Vector3(1, rand(0.7, 1.4), 1));
    grassInst.setMatrixAt(i, m4);
  }
  group.add(grassInst);

  // =====================================================================
  // GLOBAL LIGHTING — moonlight + hemisphere + subtle sun per brief
  // =====================================================================
  const hemi = new THREE.HemisphereLight(0x8aa0c0, 0x0a0c10, 0.4);
  scene.add(hemi);
  const moon = new THREE.DirectionalLight(0xbfd0e8, 1.1);
  moon.position.set(-40, 60, -30);
  moon.castShadow = true;
  moon.shadow.mapSize.set(2048, 2048);
  moon.shadow.camera.near = 1; moon.shadow.camera.far = 160;
  moon.shadow.camera.left = -80; moon.shadow.camera.right = 80;
  moon.shadow.camera.top = 80; moon.shadow.camera.bottom = -80;
  moon.shadow.bias = -0.0005;
  scene.add(moon);
  // subtle "sun" fill per your brief (слегка солнечно но всё ещё ночь)
  const sunFill = new THREE.DirectionalLight(0xfff2cc, 0.25);
  sunFill.position.set(40, 30, 20);
  scene.add(sunFill);

  // ambient fog = cold blue
  scene.fog = new THREE.FogExp2(0x0b1016, 0.022);

  // ----- spawn points for skeletons (one per zone, + extras outdoors) -----
  const spawnPoints = [
    V3(-20, 0, -10),     // admin corridor
    V3(-20, 0, 10),      // admin corridor other end
    V3(5, 0, -10),       // hall
    V3(15, 0, 12),       // hall
    V3(30, 0, -8),       // warehouse
    V3(42, 0, 8),        // warehouse
    V3(14, 0, 28),       // vents
    V3(-10, 0, -40),     // outdoor
    V3(35, 0, -35),      // outdoor
    V3(0, 0, -50),       // outdoor near fence
  ];

  return {
    group, walls, zones, spawnPoints,
    truck, flickerLamps, lights,
    crtGroup, cameraNodes, interactables,
    moon, hemi, sunFill,
  };
}
