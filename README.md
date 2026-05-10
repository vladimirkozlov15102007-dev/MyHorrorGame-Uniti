# Old Amber Factory

**Survival-horror / tactical-shooter for Unity 6 HDRP**, inspired by abandoned Soviet industrial sites.
The player wakes up at the abandoned "Amber" factory. Survive, destroy the skeleton archers, activate the yellow truck and drive away.

This repository contains the **full gameplay code base** (C# runtime) and an
**Editor block-out generator** for the 5-zone level layout. All art, audio,
animations and shaders are **pluggable** - you drop in your own assets
(recommended: Quixel Megascans, Mixamo, Kevin MacLeod music, Freesound SFX).

---

## Concept

- Sub-genres: survival horror + immersive sim + tactical shooter + stealth + adaptive AI
- Level: **Old Amber Factory** - 5 zones (Admin, Main Hall, Warehouse, Tunnels, Outdoor)
- Enemies: **10 skeleton archers** with advanced, adaptive AI
- Win condition: kill all skeletons, power the truck, find the key, start it, escape
- Lose condition: player HP reaches 0

See [docs/SETUP.md](docs/SETUP.md) for installation and [docs/DESIGN.md](docs/DESIGN.md) for full design breakdown.

---

## Requirements

- **Unity 6.0.x / 6.3.x** (tested target: `6000.0.36f1` - the project will also open in `6.3.6f1` as requested by the design brief; update `ProjectSettings/ProjectVersion.txt` if your installed version is different).
- HDRP 17.x (installed automatically via `Packages/manifest.json`).
- Scripting backend: **IL2CPP** (recommended), .NET Standard 2.1.

---

## What is included

### Runtime code (`Assets/OldAmberFactory/Scripts/`)

| Folder | Contents |
|---|---|
| `Core/` | `GameManager` (state machine, objectives), `EventBus` (noise, combat) |
| `Player/` | FPS `PlayerController` (WASD/Shift/Space/Ctrl + mouse), `Interactor`, `FootstepController`, `DamageIndicator` |
| `Weapons/` | `WeaponBase`, `Pistol`, `WeaponController` (LMB fire, RMB ADS, R reload, procedural sway, recoil, shell eject, muzzle flash) |
| `Damage/` | `Health`, `Hitbox` (head x3 / torso x2 / legs x1), `DamageInfo`, `RagdollController`, `BloodFX` |
| `Throwables/` | `Throwable`, `ThrowableHolder` - pick up bottles / pipes / rebar, hold LMB to charge throw, release to throw. Impact raises a noise event |
| `AI/Core/` | `Blackboard`, `BehaviorTree`, `Perception` (sight + hearing, occlusion) |
| `AI/Combat/` | `SkeletonCombat` (bow + melee), physics-based `Arrow` |
| `AI/Animation/` | `ProceduralMotion` (IK hooks, head look, stagger, breath) |
| `AI/Navigation/` | `CoverFinder`, `CoverPoint` (static cover + ambush markers) |
| `AI/Adaptive/` | `PlayerBehaviorAnalyzer` (observes style), `TacticsController` (picks counter-tactic) |
| `AI/Group/` | `SquadCoordinator` (share contact, assign roles: attacker / flanker / suppressor / ambusher) |
| `AI/` | `SkeletonAgent`, `SkeletonBehaviorTreeFactory`, `BTActions` (patrol, investigate, search, flank, ambush, suppress, keep distance, intercept, shoot, melee, retreat to cover) |
| `Environment/` | `SecurityRoom` (CRT monitors, camera switching, cooldown), `YellowTruck`, `TruckKey`, `PowerSwitch`, `DripEffect`, `FlickerLight`, `DestructibleProp`, `AreaTrigger` |
| `Graphics/` | `CameraShake`, `HDRPPostFXController` (indoor / outdoor-day / escape tone mapping) |
| `Audio/` | `AudioManager`, `AdaptiveMusic` (5-layer crossfade), `MusicDirector` |
| `UI/` | `HUDController` (HP, stamina, ammo, prompt, objective, damage flash) |
| `Editor/` | `LevelGenerator` - one-click blockout of all 5 zones |

### Not included (bring your own)

- 3D meshes (skeletons, bow, arrow, pistol, truck, props, pre-fractured destructibles)
- PBR 4K-8K textures (walls, floors, metal, rust, concrete)
- Animations: idle / walk / run / crouch / reload / fire / bow draw+release / melee / hit / death
- Audio clips: footsteps per surface, gunshot, reload, bowstring, arrow whoosh, skeleton vocalizations, ambient loops, adaptive music stems
- VFX: muzzle flash, shell, blood spray, impact decals, dust, dripping water, sparks
- HDRP VolumeProfile assets (a runtime profile is auto-created by `HDRPPostFXController` but for best results bake a rich one)

Suggested free / commercial sources:

- **Quixel Megascans** (free with Unity/Epic account): https://quixel.com/megascans
- **Mixamo** (free animations): https://www.mixamo.com
- **Sketchfab** (CC-BY skeleton models): https://sketchfab.com
- **Freesound** (CC SFX): https://freesound.org
- **Unity Asset Store**: HDRP Scene Samples, FinalIK, Animation Rigging, Kinematica (motion matching), Amplify Shader Editor

---

## Quick start

1. Open the folder `MyHorrorGame-Uniti` with Unity Hub.
2. Let Unity resolve packages from `Packages/manifest.json` (HDRP, Input System, Cinemachine, NavMesh).
3. In **Edit > Project Settings > Player > Other Settings**, switch **Scripting Backend** to **IL2CPP** and **Api Compatibility Level** to **.NET Standard 2.1**.
4. In **Edit > Project Settings > Graphics**, assign the HDRP asset generated during first import.
5. Open menu **Old Amber Factory > Build Blockout Level** to generate the 5-zone level skeleton.
6. Add a **NavMeshSurface** to the level root and bake (AI > NavMesh).
7. Set up the Player prefab (see `docs/SETUP.md` for the component checklist) and place it at `PlayerStart`.
8. Assign references on your Skeleton prefab - `SkeletonAgent` + `NavMeshAgent` + `Health` + `Perception` + `SkeletonCombat` + `RagdollController` (baked via Unity's Ragdoll Wizard).
9. Press Play.

---

## Controls

| Action | Key |
|---|---|
| Move | `WASD` |
| Run | `Shift` (hold) |
| Jump | `Space` |
| Crouch | `Ctrl` (hold) |
| Interact | `E` |
| Fire | `LMB` |
| Aim | `RMB` |
| Reload | `R` |
| CCTV switch camera (in security room) | `Q`, `F`, `Tab` |

---

## Architecture overview

```
GameManager ---- EventBus (noise, combat)
   |
   +-- Player
   |     +-- PlayerController (movement, stamina, noise emission)
   |     +-- Health + DamageIndicator + BloodFX
   |     +-- WeaponController -> Pistol (WeaponBase)
   |     +-- ThrowableHolder
   |     +-- Interactor
   |
   +-- Skeletons (x10)
   |     +-- NavMeshAgent + Health + Ragdoll
   |     +-- Perception (LOS + hearing)
   |     +-- SkeletonCombat (bow + arrow + melee)
   |     +-- TacticsController <- PlayerBehaviorAnalyzer
   |     +-- SquadCoordinator <-- role assignment
   |     +-- BehaviorTree (patrol / investigate / search / combat variants)
   |
   +-- Environment
   |     +-- SecurityRoom, YellowTruck, TruckKey, PowerSwitch
   |     +-- AreaTrigger -> HDRPPostFXController
   |
   +-- Audio
         +-- MusicDirector -> AdaptiveMusic (5-layer)
```

---

## Adaptive AI - what it actually does

`PlayerBehaviorAnalyzer` samples the player's position every 0.5s and computes normalized scores:

- `CoverUsage` - frequency of standing next to geometry
- `Aggression` - shots fired + forward motion
- `Mobility` - total distance traveled / window
- `Campiness` - spatial variance
- `Predictability` - low variance + repeat routes

`TacticsController` picks a counter-tactic using priority rules (see spec):

| Player style | Tactic | Effect |
|---|---|---|
| High cover | `flank` | Skeletons route around cover |
| High mobility | `intercept` | Predict player's next position |
| Campy | `ambush` | Skeletons take ambush points and wait |
| Aggressive | `keep_distance` | Skeletons retreat to kite range |
| Predictable | `suppress` | Multiple skeletons fire suppressive arrows |

`SquadCoordinator` broadcasts contact to nearby members and re-assigns roles
(attacker / flanker / suppressor / ambusher) every few seconds.

---

## License

Code: MIT (see `LICENSE`).
You are responsible for the license of any third-party assets you drop into `Assets/`.
