# YOKAI Unity V1.0 — Playable Vertical Slice

## Implemented gameplay loop
- Third-person humanoid Hunter with attached katana.
- Camera collision and lock-on.
- Walk/run/sprint with acceleration/braking and slopes through CharacterController.
- 3-hit light chain with cancel timing.
- Heavy attack with Blade Flow scaling.
- Directional dodge with a shorter i-frame window and an even shorter perfect-dodge window.
- Guard, perfect parry, posture damage, posture break, stagger and finishers.
- Blade Flow gain/decay and high-flow heavy bonus.
- Gale / Stone / Spirit stances.
- Four Hunter Arts: Fire, Storm, Spirit and Shadow.
- Heal charges and shrine refill.
- Grunt, Ronin, Stalker, Elite and Heavy tuning support.
- Attack-token coordinator limits concurrent attacks.
- Three-pattern, three-phase Oni Warden boss.
- Boss telegraphs and punish windows.
- Procedural Hoshikawa Cedar Pass: forest road, lanterns, gates, shrine, elite encounter and boss arena.
- Encounter progression and victory state.
- Persistent checkpoint/boss save.
- Landscape mobile HUD with analog touch movement, camera swipe and combat buttons.
- Performance profiles: 30 / 45 / 60 FPS.
- Desktop keyboard/mouse fallback for testing.

## Art boundary
Characters are composite humanoid procedural rigs, not capsules. Weapon is parented to the right hand.
They are a functional fallback and are NOT claimed as final production art.

## Not yet claimed
- Unity compile/runtime test in this environment.
- Final imported skeletal meshes and authored animation clips.
- 2–4 player network co-op.
- Physical Android device profiling.

Co-op is intentionally the next major checkpoint after the first real Unity compile, to avoid mixing network issues with base gameplay issues.
