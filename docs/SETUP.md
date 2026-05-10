# SETUP

Step-by-step guide to get the game running from a fresh clone.

## 1. Unity version

This project targets Unity 6 HDRP (compatible with `6.3.6f1` / `6000.0.x`).
If your installed editor version is different, update:

- `ProjectSettings/ProjectVersion.txt` - set the first line to your `m_EditorVersion`.

## 2. Open the project

1. Unity Hub -> Open -> select the `MyHorrorGame-Uniti` folder.
2. Wait for package resolution (HDRP 17.x may take several minutes to import the first time).
3. When prompted, let Unity auto-create the HDRP asset + default profile.

If HDRP did not auto-configure:
- `Edit > Project Settings > Graphics > Scriptable Render Pipeline Asset` - assign `HDRenderPipelineAsset`.
- `Edit > Project Settings > Quality` - replace the URP asset with HDRP in each quality level.
- `Window > Rendering > HDRP Wizard` - run "Fix All".

## 3. Build the blockout level

Menu: **Old Amber Factory > Build Blockout Level**

This creates:

- 4 indoor zones (Admin / Main Hall / Warehouse / Tunnels)
- 1 outdoor zone with the yellow truck, key, power switch
- Patrol / cover / ambush markers
- Skeleton spawn markers

## 4. NavMesh

1. Select the `Level_Blockout` root.
2. Add component `NavMeshSurface` (from `Unity.AI.Navigation`).
3. Click **Bake**. Adjust agent radius to `0.3`, step height to `0.4`.

## 5. Create the Player prefab

1. New empty `GameObject` named `Player`.
2. Add `CharacterController` (height 1.8, radius 0.35).
3. Add `Health` (max 100).
4. Add `PlayerController` (assign `CameraRig` to a child `Camera`).
5. Child `Camera` named `CameraRig`:
    - Add `Camera` (HDRP-ready).
    - Add `CameraShake`.
    - Add a child `WeaponSocket` (for weapon mesh).
6. Add `FootstepController` (assign `PlayerController` ref, populate surface sets).
7. Add `Interactor` (assign `rayOrigin` to the Camera).
8. Add `ThrowableHolder` (assign `handSocket` next to the camera).
9. Add `WeaponController` (assign the Camera and a `Pistol` child for the firearm).
10. Tag the player `Player`. Set layer to `Player`.
11. Place at `PlayerStart`.

## 6. Skeleton prefab

1. Import a humanoid skeleton mesh (e.g. Mixamo "Skeleton Warrior"). Rig as Humanoid.
2. Use Unity's **GameObject > 3D Object > Ragdoll...** wizard to set up joints.
3. On the root add:
    - `NavMeshAgent` (radius 0.35, speed 3, acceleration 12, stopping distance 0.3)
    - `Health`
    - `Perception` (assign `eyes` transform between the eye sockets)
    - `SkeletonCombat` (assign `bowMuzzle`, `arrowPrefab` with `Arrow` on root)
    - `ProceduralMotion` (assign spine + head)
    - `CoverFinder`
    - `TacticsController`
    - `RagdollController` (auto-finds bones)
    - `SkeletonAgent` (tie all refs together)
4. On each collider that represents a body part, add `Hitbox` and set its `BodyPart` (Head / Torso / Legs / Arms). Set layer `Hitbox`.
5. Tag `Skeleton`.

## 7. Squad & Analyzer

Create two empty GameObjects in the scene (or on the GameManager):
- `SquadCoordinator` - singleton
- `PlayerBehaviorAnalyzer` - singleton, assign the player transform

## 8. Audio

1. Create a child GameObject `MusicRoot`.
2. Add 5 AudioSource components - one for each layer (calm / tension / detect / combat / critical). Set looping, volume 0.
3. Add `AdaptiveMusic` and assign all 5 sources.
4. Add `MusicDirector`, assign `AdaptiveMusic`, the player transform and player health.

## 9. HDRP Volume

1. Create a GameObject `Global HDRP Volume`.
2. Add a `Volume` (global) with a new `VolumeProfile`.
3. Add overrides: `Fog`, `Exposure`, `ColorAdjustments`, `WhiteBalance`, `Vignette`, `DepthOfField`, `Bloom`, `MotionBlur`, `Film Grain`, `ChromaticAberration`, `ScreenSpaceReflection`, `ScreenSpaceAmbientOcclusion`, `VolumetricClouds`, `ScreenSpaceGlobalIllumination`.
4. Add the `HDRPPostFXController` component; it will drive overrides at runtime.
5. In outdoor zone: place an `AreaTrigger` box, set Area to `OutdoorDay`.
6. In every indoor zone: place an `AreaTrigger`, set Area to `IndoorIndustrial`.

## 10. HDRP Quality

`Edit > Project Settings > Quality > HDRP` - enable:
- Volumetric Fog (high res)
- Screen Space Reflection
- Screen Space Global Illumination
- Ray Tracing (if your GPU supports DXR - optional)
- Contact Shadows
- Subsurface Scattering

## 11. Lighting

1. Sun light: directional, warm temp (5500K), Shadowmap + PCSS filter, volumetrics enabled.
2. Indoor fluorescents: Spot / Area, color temp 4500-6500K, `FlickerLight` component.
3. `Window > Rendering > Lighting > Generate Lighting` - bake static GI for indoor zones.

## 12. Input

This project uses classic `Input.GetKey/Axis` calls for simplicity, so no InputActions asset is required. To migrate to the new Input System:

1. `Edit > Project Settings > Player > Active Input Handling: Both`.
2. Create `OldAmberInput.inputactions` with actions: `Move, Look, Jump, Crouch, Run, Fire, Aim, Reload, Interact`.
3. Replace the `Input.xxx` calls in `PlayerController` / `WeaponController`.

## 13. Build

- **Build Settings**: scene `OldAmberFactory.unity`, Windows/Mac standalone.
- **Graphics API**: DX12 (required for ray tracing) or Vulkan.
- **Scripting Backend**: IL2CPP.
- **Api Compatibility Level**: .NET Standard 2.1.
