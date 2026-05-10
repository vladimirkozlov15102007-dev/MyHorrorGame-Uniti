# Old Amber Factory — Three.js Survival Horror

A single-level cinematic survival-horror / tactical-shooter built with **Three.js**.
Everything is procedural — no external textures or models are loaded. Just open and play.

## How to run

Because of ES modules + pointer lock, serve the folder via a local web server
(don't open `index.html` as a `file://`):

```bash
cd web-game
# any static server works, e.g. Python:
python3 -m http.server 8000
# then open http://localhost:8000/
```

Or with Node:
```bash
npx http-server -p 8000
```

## Controls

| key | action |
|---|---|
| WASD | move |
| Shift | sprint |
| Space | jump |
| Ctrl | crouch |
| LMB | fire pistol / hold to charge-throw |
| RMB | aim down sights |
| R | reload |
| E | interact / pickup |
| G | drop held throwable |
| F | flashlight |
| Esc | exit security-camera view |

## Objectives

1. Kill or evade the **10 skeleton archers** across five zones:
   Admin → Factory Hall → Warehouse → Vent Tunnels → Outdoor Yard.
2. Activate the **power panel** in the yard.
3. Find the **truck key** in the warehouse.
4. Reach the **yellow truck** and start the engine.
5. Survive the final wave and escape.

## Systems

- Procedural PBR materials (canvas-generated albedo / normal / roughness maps).
- Directional moonlight + shadow mapping + hemisphere + subtle sun fill.
- Cinematic post-fx: UnrealBloom, custom shader for chromatic aberration,
  vignette, film grain, cold-teal/orange grade, hurt-red flash.
- Volumetric "god rays" cone meshes + atmospheric exponential fog.
- Floating dust particle field.
- Behaviour Tree–style state machine per skeleton: patrol / investigate /
  alert / combat / flank / ambush / search / retreat / group_attack.
- `GroupCoordinator` broadcasts sightings and dispatches flankers / ambushers.
- `PlayerBehaviorAnalyzer` detects cover-camping / rushing / aggression and
  adapts enemy tactics (flanking, ambush ahead, suppression fire, distance keeping).
- Spatial WebAudio: synthesized gunshots, bow shots, arrow impacts,
  footsteps per surface, layered adaptive music (ambient / tension / combat).
- Throwable objects (bottles, pipes, cans) with noise events the AI reacts to.
- Arrow projectiles with ballistic drop, pierce damage by body zone.
- Security-cam room with CRT bank, switchable camera views, cooldown.
- Player capsule physics against AABB walls, head-bob, stamina,
  directional damage indicators, hurt vignette, low-HP blood overlay.

## Files

```
web-game/
├── index.html
├── style.css
├── README.md
└── js/
    ├── main.js             bootstrap & menu
    ├── game.js             top-level orchestrator
    ├── level.js            5-zone factory + truck + interactables
    ├── materials.js        procedural PBR material factory
    ├── player.js           FPS controller
    ├── weapon.js           pistol: raycast + recoil + shells + muzzle flash
    ├── projectiles.js      arrows + throwable items
    ├── skeleton.js         procedural skeleton archer + procedural animation
    ├── ai_group.js         coordinator + player behavior analyzer
    ├── postfx.js           composer, grade shader, dust, god rays
    ├── audio.js            WebAudio engine (synth SFX + layered music)
    └── util.js             collision / LOS / math helpers
```

## Honest limitations (vs the Unity-HDRP brief)

Three.js runs in the browser on WebGL 2; some features in the original brief
don't have a direct Three.js counterpart at production quality:

- No ray-traced reflections / ray-traced GI — we approximate with bloom,
  moonlight shadow maps, and cone-mesh god rays.
- No Nanite-level micro-geometry — geometry is LOD-free low/medium-poly with
  procedural normal maps to fake displacement.
- No SSR (Three has a `SSRPass` add-on but it's expensive in-browser; disabled).
- No hardware tessellation — materials use high-frequency normal maps instead.
- Skeleton animation is procedural (bone transforms), not motion-matched from
  mocap. Ragdoll is a simple orientation/fall.

Everything the brief requested at a behavioral level (five zones, 10 adaptive
AI archers, pistol combat, throwables with noise events, security cameras,
yellow truck escape with power + key + ignition, HUD, adaptive music, cinematic
post-fx) is implemented and playable end-to-end.
