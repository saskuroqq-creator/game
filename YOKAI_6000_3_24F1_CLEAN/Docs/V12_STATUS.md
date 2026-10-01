# YOKAI Unity V1.2 — Character & Animation Polish

## New in V1.2
- Dedicated procedural Pose Animator separated from visual rig construction.
- Layered idle / breathing / walk / run / sprint motion.
- Light combo poses vary by combo index.
- Heavy, dodge, parry, guard, Hunter Art, finisher, hit, stagger and death poses.
- Ground-normal lower-leg alignment with self-collider filtering.
- More readable humanoid silhouette: hands, feet, belt, shoulder guards, sheath and katana.
- Optional Animator bridge remains ready for imported authored clips.
- Enemy health/posture indicators.
- Boss phase presentation and phase-material changes.
- Oni mask and glowing eyes.
- Contextual in-game tutorial.
- Heavy archetype is now present in the playable encounter route.
- Boss title card.
- Forest fireflies and tree-crown wind sway.
- Tree foliage no longer uses blocking physics colliders.
- Existing V1.1 combat feedback, projectile pooling, elemental reactions and original audio remain intact.

## Playable route
Lantern road -> yokai ambush -> ronin/stalker/heavy/elite encounter -> spirit shrine -> Oni Warden Kagane -> victory.

## Art boundary
This remains an autonomous procedural-art vertical slice.
The rig is designed so imported humanoid skeletal models / Animator Controllers can replace the procedural visual layer without rewriting combat, AI or encounter logic.

## Next major branch
V1.3: co-op gameplay architecture and 2-player synchronization seam, after real Unity compile/play validation.
