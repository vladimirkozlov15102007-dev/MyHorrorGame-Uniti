# Old Amber Factory - Design breakdown

## 1. Player

- First person. `PlayerController` handles WASD + Shift + Space + Ctrl + mouse look.
- Stamina drains while running, regenerates after a short delay.
- Every few tenths of a second the player broadcasts a `NoiseEvent` with radius based on speed.
- Health 100. Damage resolved via `Hitbox` children on the capsule (torso / head cap / legs).

## 2. Weapon - Pistol

- Base damage 10 -> head 30 / torso 20 / legs 10 via `DamageInfo.FinalDamage()`.
- Features: recoil kick + recovery, procedural weapon sway tied to mouse delta, walk bob, muzzle flash, shell ejection rigidbody, tracer line, audio shot + reload + dry-fire + empty mag.
- Reload time 1.8 s. Magazine 12 / reserve 60 by default.
- On fire, broadcasts a loud `NoiseEvent` (radius 40).

## 3. Throwables

- Bottles, pipes, cans, rebar, stones. Any object with a `Throwable` + `Rigidbody` + `Collider` qualifies.
- Interactor picks up -> `ThrowableHolder` attaches to hand socket, makes kinematic.
- LMB hold charges throw power 0..1 over 1.4 s, release adds force along camera forward.
- Impact above `minImpactSpeed` plays a material-dependent clip and broadcasts a `NoiseEvent`.
- Skeletons subscribed via `Perception` hear the noise and investigate.

## 4. Damage & Ragdoll

- `DamageInfo` carries point, direction, force, body part.
- `Health` subtracts modified damage, fires `OnDamaged` and `OnDied`.
- `RagdollController` is pre-configured in a "kinematic" state. On death:
    - Disables `Animator`, NavMeshAgent, `SkeletonAgent`.
    - Enables per-bone rigidbodies and colliders.
    - Applies impulse at the nearest bone to the impact point for a physics-accurate knockdown.

## 5. Skeleton AI

### Perception

Sight: 130 FOV cone, 22 m range, LoS ray against occluders mask.
Hearing: subscribes to `EventBus.NoiseEvent`. Remembers last known position for 8 s.

### Behavior Tree

```
Selector
  Combat (priority 1)
    Condition: has LoS OR squad shared contact
    Selector
      Retreat to cover if HP < 25%
      Selector (tactic)
        flank: move to flank -> shoot
        ambush: move to ambush point -> wait and strike
        suppress: hold range -> suppressive fire (does not need LoS)
        keep_distance: kite away, shoot
        intercept: predict player velocity, move to intercept
        default (attacker): approach -> melee OR shoot
  Alert (priority 2)
    Condition: has recent knowledge
    Sequence: investigate last known pos -> search around
  Patrol (priority 3)
    Default: move between patrol points
```

### Adaptive

`PlayerBehaviorAnalyzer` samples the player's state every 0.5 s over a 45 s window.
`TacticsController` picks one counter-tactic per agent, refreshed every 4 s.

### Squad

`SquadCoordinator` shares contacts within a 35 m radius of any member who sees the player.
Role assignment every 5 s:
- closest -> attacker
- farthest -> suppressor
- middle -> flanker(s)
- agents with `wantAmbush` stay ambushers

### Combat

`SkeletonCombat`:
- Bow: draw -> release animation event spawns `Arrow` with initial velocity along aim, adds random inaccuracy.
- Arrow: rigidbody, orient-to-velocity, sticks on impact, applies `DamageInfo` to hitboxes, physical force to rigidbodies, pierces light surfaces based on `pierceChance`.
- Melee: sphere overlap at torso height, 15 base damage (30 head / 20 torso / 10 legs if hitting a hitbox).

### Procedural motion

`ProceduralMotion` exposes head look target, spine sway from breathing, stagger angle from hits,
idle noise. Hook up Animation Rigging or FinalIK constraints that read these transforms for full
IK behavior.

## 6. Security Camera Room

- `SecurityRoom` holds references to several disabled `Camera` components placed across the level.
- Each camera renders to a `RenderTexture` assigned on the monitor's material.
- Interact -> activates the first camera, pins the player at `viewAnchor` so they cannot move.
- `Q` / `F` / `Tab` to cycle cameras (see `SecurityRoom.Update`).
- Timer (`useDuration`) auto-exits, then `cooldown` before next use.

## 7. Yellow Truck escape

- Gated objectives on `GameManager`: power activated -> key collected -> hold E to start -> drives to exit waypoint -> victory.
- Each step broadcasts HUD updates via the objective text.
- Final wave: spawn remaining skeletons near the outdoor zone as soon as engine starts (hook to `GameManager.OnStateChanged`).

## 8. Environment

- `FlickerLight` for fluorescent strobing, supports a permanently-broken variant.
- `DripEffect` for periodic water droplets + sound.
- `DestructibleProp` swaps pristine mesh with pre-fractured mesh on destruction, emits noise.
- `AreaTrigger` transitions post-fx between `IndoorIndustrial`, `OutdoorDay`, `Escape`.

## 9. Graphics / HDRP

`HDRPPostFXController` drives a `Volume` profile based on area:
- Indoor: dense fog (35 m mean-free-path), cold tint (-20 white balance), -12 saturation, heavy vignette.
- Outdoor day: fog essentially disabled, warm tint, +4 saturation, low vignette - sunny, all detail visible.
- Escape: outdoor tone + cinematic DoF focused at 12 m.

Recommended extra overrides: ray-traced reflections, screen-space GI, contact shadows, volumetric clouds, Micro-Shadows, SSGI, Motion Blur (shutter 0.3), Bloom, Film Grain, Chromatic Aberration.

## 10. Audio

- `AudioManager.PlayOneShot3D` for ad-hoc positional SFX.
- `AdaptiveMusic` crossfades between calm / tension / detect / combat / critical layers.
- `MusicDirector` evaluates every 0.4 s based on nearest skeleton distance, number of engaged enemies, and player HP critical state.

## 11. UI

`HUDController` drives:
- Health fill
- Stamina fill
- `XX / XX` ammo text
- Interact prompt
- Objective text (auto-updates from `GameManager`)
- Damage flash overlay + camera shake on hit
