# YOKAI Unity V1.4 — 3D Art & World Release Candidate

## Goal
Keep the verified V1.3 gameplay/build foundation intact while replacing the placeholder look with a mobile-first authored 3D layer.

## New 3D character layer
- Shared deterministic low-poly mesh library; no dependency on runtime primitive geometry for character bodies.
- Hero: tapered torso/hips, articulated limbs, low-poly head, coat panels, scarf tails, shoulder armor, topknot, half mask, sheath and katana.
- Grunt: kasa + face wrap.
- Ronin: wide kasa, neck guard and back banner.
- Stalker: fox mask, ears and dual hand claws.
- Heavy: heavy face plate, oversized shoulders and kanabo.
- Elite: oni mask, horns and chest plate.
- Oni Warden Kagane: unique boss mask/jaw/horns, large chest/shoulder armor, back banner and oversized studded kanabo.
- Existing procedural pose/attack animation still drives the articulated model parts, preserving gameplay timing.

## New 3D world layer
Four visually distinct contiguous zones on top of the existing safe collision shell:
1. Cedar Pass
2. Forsaken Hamlet
3. Spirit Shrine Basin
4. Oni Courtyard

Environment additions:
- custom low-poly cedar trees with two LOD levels;
- deterministic rock meshes;
- minka houses / ruined houses;
- bamboo fences and broken cart;
- stone lanterns;
- torii architecture;
- shrine approach steps and komainu guardians;
- boss arena stone ring;
- oni temple facade and hanging banners.

## Mobile performance strategy
- Shared cached meshes and shared cached materials.
- GPU instancing enabled on generated materials.
- Simple hidden gameplay collision slab/road retained to avoid decorative collision snags.
- Decorative world geometry has no unnecessary colliders.
- Tree LOD groups cull distant detail.
- Existing 30/45/60 quality profiles, shadow-distance controls and target FPS logic remain active.

## QA additions
Runtime QA now requires:
- V1.4 player 3D rig and minimum mesh population;
- V1.4 world art root;
- all four visual zones;
- 3D mesh population for all enemies and boss;
- existing camera/mobile/combat/encounter/boss/collision checks.

## Android
- Version: 1.4.0
- versionCode: 140
- IL2CPP + ARM64
- Release APK: `Builds/Android/YOKAI_V14_RC.apk`

## Next device test
After Unity/GitHub compile succeeds, verify on device:
- silhouettes are readable at gameplay camera distance;
- no model pieces detach during attacks/dodge/death;
- world art never blocks the invisible simple traversal collision;
- 30 FPS profile remains stable in Oni Courtyard and Hamlet;
- boss phase material changes still render correctly on the new model.
