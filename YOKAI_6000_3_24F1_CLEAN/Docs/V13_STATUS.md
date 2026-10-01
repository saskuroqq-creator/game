# YOKAI Unity V1.3 — Release Candidate Hardening

## Priority
Yokai is the primary project. Anima is intentionally deferred until Yokai reaches a verified playable Android APK.

## Added over V1.2
- Android release build path in addition to development APK.
- BuildReport enforcement: failed/empty builds now stop the pipeline instead of being treated as ready.
- Fixed application identifier, version 1.3.0 and Android version code 130.
- Release-source validator for scene/bootstrap, combat-critical scripts, all original audio, IL2CPP, ARM64 and package modules.
- Runtime QA component that validates player, camera, mobile HUD, combat, encounter composition, boss, gates, projectile pool, audio/performance managers and walkable spawn ground.
- Pause flow with mobile pause button and desktop Escape key.
- Pause overlay: Resume, Restart Checkpoint, New Game and Quality profile cycling.
- Robust checkpoint restart without requiring player death.
- Save flush on app pause/quit.
- Saved boss victory now restores the real Victory flag, not only the stage label.
- Boss death/respawn reset now clears phase, intro, invulnerability, attack pattern and boss ambience state before replaying the encounter.
- Mobile quality defaults tightened for a 30/45/60 FPS target.

## Release gate
Do not call an APK finished until Unity 6000.3.24f1 actually compiles the project and `YokaiProjectBootstrap.BuildAndroidCi()` or `Build Android RELEASE APK` completes with `BuildResult.Succeeded` and a non-empty `Builds/Android/YOKAI_V13_RC.apk`.

## First-device smoke test
1. No black/gray screen.
2. Hunter spawns on road and camera follows.
3. Touch joystick and camera swipe work simultaneously.
4. Light/heavy/dodge/parry/guard/art/heal/lock all respond.
5. First gate opens after first encounter.
6. Elite gate opens after elite encounter.
7. Shrine heals/saves/refills.
8. Death respawns at checkpoint and resets active encounter.
9. Boss intro, all three phases, projectiles and death work.
10. Pause/resume/restart/new-game/quality controls work.
11. Reload after boss victory preserves completion.
