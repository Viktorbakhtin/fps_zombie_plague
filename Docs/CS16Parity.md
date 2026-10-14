# Standard Steam CS 1.6 target

The target is standard Steam CS 1.6, not a ZP server with modified air acceleration or auto-bhop.
Current prototype is a first implementation, not verified 100% parity.

Reference implementations:
- Valve movement: https://github.com/ValveSoftware/halflife/blob/master/pm_shared/pm_shared.c
- ReGameDLL CS movement: https://github.com/rehlds/ReGameDLL_CS/blob/master/regamedll/pm_shared/pm_shared.cpp
- AK-47: https://github.com/rehlds/ReGameDLL_CS/blob/master/regamedll/dlls/wpn_shared/wpn_ak47.cpp
- Weapon constants: https://github.com/rehlds/ReGameDLL_CS/blob/master/regamedll/dlls/weapons.h
- Recoil/recovery: https://github.com/rehlds/ReGameDLL_CS/blob/master/regamedll/dlls/weapons.cpp

Formula implementation is independently written C#, not imported Valve SDK source.
GoldSrc units are converted at the Unity boundary; authored scale is 0.0254 metres/unit.
Movement config: accelerate 5, airaccelerate 10, friction 4, stopspeed 75, gravity 800,
step 18, speed 221 with AK equipped. Air acceleration caps projected wish speed at 30
while its acceleration multiplier uses uncapped wish speed. Jump impulse sqrt(2*800*45),
release-to-jump behavior, standard 1.2x bunnyhop threshold/0.8 cropping and fuser2-style stamina.
The current 100 Hz simulation is a chosen reproducible baseline; GoldSrc usercmd msec/FPS
must be matched when comparing trajectories.

AK prototype: 30 rounds, 0.0955s cycle, 2.45s reload, base damage 36, range falloff 0.98,
air/running/still spread branches, previous-shot accuracy ordering, floating-point cubic
accuracy and posture-dependent KickBack parameters. Accuracy recovers on release after
0.4 seconds, reducing the burst count every 0.0225 seconds and returning to base accuracy
when the count reaches zero. This deliberately fixes the stale-accuracy legacy behavior.
Only AK is implemented at this milestone.

Remaining parity work:
- GoldSrc box hull traces, multi-plane clipping, stairs/edges/slopes, duck transitions and ceiling checks;
  Unity capsule collision currently approximates these and is not identical.
- Capture reference usercmd sequences/trajectories from the specified Steam build and compare traces.
- Shared seeded GoldSrc random generator and exact shot seed protocol, recoil timing/prediction.
- Hitgroups, armour, penetration and surface-dependent damage.
- Other CS weapons, firing sounds, viewmodels and weapon animations.
- Lag compensation and packet-loss/jitter tests.

Ten EditMode checks pass for domain/combat, formula reference values and burst recovery.
These validate individual rules, not whole-engine parity.

Standing spread regression: CharacterController.minMoveDistance was 1mm, larger than the
0.2mm standing ground push per 100Hz tick. Reconciliation also disabled/re-enabled the
controller, invalidating its isGrounded flag. This incorrectly selected the air spread
branch (0.54 at maximum AK accuracy rather than standing 0.034375).
The motor now uses zero minimum movement, caches ground contact from movement collision
flags with a short walkable-surface probe, and transmits ground state in reconciliation.
A PlayMode regression checks 100 standing frames, a full 30-shot standing spray, recovered
first-shot spread, and the genuine airborne penalty after moving the actor above the floor.
