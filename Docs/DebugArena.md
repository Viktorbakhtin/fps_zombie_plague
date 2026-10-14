# Quick debug scene

Open Assets/_Game/Scenes/DebugArena.unity and press Play. A local FishNet host starts
automatically on port 7778. Two AI bots join as humans. After five seconds the server
randomly infects one of all three participants, including the local player.

Blue = human, green = zombie. Human bots shoot and retreat; infected bots chase and melee.
LMB shoots as a human and infects on a zombie melee hit. WASD, mouse, Space jump,
Ctrl duck, Shift walk, R reload, Escape releases cursor. F5 or New round resets the round.
Rounds restart when a side has no living participants or the 60-second round timer ends.

Reused template assets: tileable floor/walls, Armature_3P character, assault rifle model,
Idle, forward/backward Run, Shoot and Reload clips. Visuals use a small new Animator; original Netcode components and
animation callbacks are excluded from these new prefabs. The rifle model is the template's
fictional rifle; current firing formulas are the AK-47 prototype, not a claim of model parity.
Upper-body shooting/reload animations and template rifle audio are connected to confirmed server actions.
First-person arms, full first-person reload animation and VFX integration remain future work.

Verified: PlayMode test asserts auto-host, 3 participants, exactly one initial zombie and
automatic restart after all participants become infected. Corrected floor tile rotations
were rendered and visually inspected. TestArena remains the separate manual multiplayer scene.

Windows development build: Builds/DebugArena/ZombiePlagueDebug.exe. Build succeeded
with 0 errors and 0 warnings on 2026-10-14. It starts the debug match automatically.

Shooting fixes: burst accuracy now recovers after releasing fire, yellow debug ray lines removed.
Camera aim runs in LateUpdate; its view rig uses FishNet position smoothing between ticks.
Only the player camera renders during gameplay. Command-line smoke detection is cached,
HUD no longer scans the scene each GUI pass, and actor tint updates only on state changes.
Editor sample CPU frame times: 6.0ms before, 4.7ms after. Single samples are not a sustained
benchmark or a guarantee for standalone/network conditions.

Audit fixes: bots reload when empty; crouch uses the authoritative lowered eye position;
standing up is blocked by overhead geometry; round reset clears movement velocity/stamina;
retreating bots blend to backwards run rather than forward run. Animation speed is damped
to avoid Idle/Move flicker between network ticks. Upper-body actions use the template AvatarMask.
PlayMode audit verifies cooldown, reload gating/completion, crouch eye height, wall-blocked
and unobstructed melee infection, autonomous bot reload, Shoot/Reload Animator states,
backwards animation input, burst recovery, camera aim and random infection/round restart.
Infected participants hide their rifle models; zombie HUD shows the infection melee action.

Automatic fire now sends held-trigger/aim input every 50ms. The server owns the 95.5ms
AK cadence and preserves its phase between render frames; input expires after 250ms.
There are no independent client/server shot timers competing to accept the same shot.
Remote-owner camera recoil interpolates the replicated punch instead of stepping to it.

DebugBots.asset configures perception, reaction, memory, speed, spacing and burst size.
Bots patrol, acquire visible enemies, retain the last seen position for three seconds,
use a baked NavMesh around template wall cover, and separate from nearby actors.
Humans reposition to keep distance, fire three-shot bursts and reload; zombies pursue
and infect at melee range. Perception/path decisions run at 5Hz; aim and movement run
per frame. The debug HUD shows each bot's faction, health and current action.
Round results now remain visible for three seconds before respawning participants.
This is a prototype AI: advanced hearing, coordinated tactics and dynamic navigation
are not implemented. No claim of complete CS 1.6 parity or 64-player performance.

Simulation checks added: held-trigger cadence, release and stale-input timeout;
wall-blocked perception, limited human bursts, baked path around cover, and an actual
zombie chase around cover ending in infection. Corner following uses a tighter arrival
threshold to prevent a CharacterController from cutting into a wall at a path bend.
Input press/release is submitted immediately, with 50ms refreshes while held.

Softer shooting presentation: the camera recoil offset now uses 90ms SmoothDamp with
an angular speed limit of 30 degrees/second, including on the local host. Mouse look
remains immediate. Rifle kick is reduced from 45mm to 18mm and smoothed over 35ms.
These are presentation changes; server recoil, spread, cadence and damage are unchanged.

Round startup now has server-owned combat gating: bots wait at separated spawns during
5s preparation; exactly one random actor is infected, followed by 3s grace before combat.
Damage and infection are rejected by NetworkActor during preparation, grace and results.
Human practice shots remain available during preparation. Bots resume only when combat
opens. StartupProtectionAndSurfaceFeedback exercises two consecutive startup cycles.

Confirmed shots play template muzzle flash particles at the rifle barrel. Confirmed
surface hits send the nearest server raycast point/normal to observer clients, which
play template dust/decal particles in a pool of 32 impacts expiring after eight seconds.
Legacy Netcode authoring scripts are stripped from the copied effect prefabs, and legacy
particle materials are copied to URP materials without changing template originals.

Visual QA rendered the real first-person camera: the adjusted warm muzzle burst is visible
at the rifle tip; the template bullet decal appears on the server-confirmed wall face.
Player spawn/reset positions are separated for two connected humans as well as the bots.

2026-10-14 verification: all three PlayMode scenarios passed individually: startup/VFX
(two round starts, blocked grace infection, exact wall impact point/normal, muzzle creation,
32-impact cap and expiry), prior gameplay audit, and automatic cadence/bot visibility/chase.
DebugArena Windows development build succeeded with zero errors and zero warnings.

First infected health: Prototype.asset now exposes FirstZombieHealth = 1500, separate
from the regular ZombieHealth = 250. Only DebugRound's randomly selected initial zombie
receives this bonus. Contact infection still assigns regular health; round reset restores
human health. The gameplay audit verifies both authoritative health values.

First infected health is now 3000 HP (ordinary contact infection remains 250 HP).
Confirmed Classic Infection bullet damage pushes surviving zombies horizontally in the
actual server shot direction. Preset: 0.16 m/s impulse per damage point, 12.7m range,
12m/s accumulated speed cap, drag 4/s, crouching multiplier 0.25. Values are authored
in Prototype.asset. Repeated accurate fire holds a charging zombie back; reload pauses
let it recover. Movement uses CharacterController collision, so knockback cannot teleport
through walls. Preparation/results, friendly hits, melee and Outbreak do not apply it.
The same KnockbackMotion is composed into both AI controllers and the predicted player
motor; remaining impulse velocity is included in authoritative reconciliation and reset.
The ZP reference configuration is https://github.com/Gam3ronE/ZP/blob/master/plugin/addons/amxmodx/configs/zombieplague.cfg
This preset is tuned for the prototype; exact ZP server class/weapon settings are not claimed.
