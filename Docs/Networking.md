# FishNet prototype

FishNet 4.7.3 pinned through UPM. Active prototype: Assets/_Game/Scenes/TestArena.unity.
Old Unity Netcode scenes and scripts are preserved; the prototype build includes only TestArena.

Local launch: Play in Unity, click Host. Run Builds/Prototype/ZombiePlague.exe and click Join.
Default endpoint: 127.0.0.1:7777, two clients maximum in Prototype.asset.
WASD movement, mouse look, Space jump, Ctrl duck, Shift walk, LMB fire/melee, R reload,
Escape releases cursor; click captures cursor again.

Player movement uses FishNet Replicate/Reconcile at 100 ticks/sec. Server owns health,
factions, damage, ammo, reload, shot rate and AI. Inputs are magnitude-clamped and checked
for NaN/infinity. RPCs require ownership. Hits are raycast on the server from its player position.
NetworkTransform is server-authoritative; prediction state forwarding is disabled for spectators.
AI walks directly towards the nearest human; no navigation around obstacles yet.

Classic: zombie contact infects. Outbreak: contact damages, with human PvP enabled.
Change Prototype.asset mode before starting a session.
No rounds, matchmaking, persistence, authentication or reconnect yet.
No lag compensation; current raycasts use current server colliders.

Development-only integration smoke: start one build with -zpServer and two builds with
-zpClient -zpSmoke. Smoke owners move for one second and submit one real weapon RPC.
Server logs [ZP] spawn/damage/infection transitions. Smoke input is disabled in release builds.
This checks connection/spawning/combat/infection; it does not establish performance at 64 clients.

Verified 2026-10-14: Windows development build succeeded, 0 build errors/warnings.
Tools/Test-Prototype.ps1 passed with two separate standalone clients and a headless server:
AI spawned at 250 HP, weapon RPC reduced it to 215 HP, both human players became zombies
at 250 HP, no checked runtime exceptions. Server log: Logs/smoke-server.log.
Headless testing uses the player executable; a Dedicated Server target build is still pending.

2026-10-14 shooting/camera regression checks: 10 EditMode tests passed; PlayMode verifies
actual server burst recovery (accuracy and recoil), camera aim after root rotation, and debug rounds.
Debug shot lines removed entirely. Player view graphics are position-smoothed by FishNet;
mouse rotation is applied in LateUpdate and is not interpolated with server rotation.

The expanded gameplay audit also covers reload completion/gating, low crouch shot origin,
wall-blocked melee, bot auto-reload, and actual Shoot/Reload Animator states. Player velocity
and stamina are cleared on debug round reset. Template rifle audio is triggered by server
confirmation, and zombie rifle models are hidden. Full first-person arm animation remains pending.

DebugArena AI now uses server-only NavMesh navigation, line-of-sight perception and target
memory. The simpler ServerZombie in TestArena remains the original multiplayer smoke actor.
Automatic player fire sends owned held-trigger/aim input immediately on press/release and
refreshes aim every 50ms while held. The server validates finite unit aim, throttles input,
expires stale intent after 250ms and alone schedules shots, ammo, hit tests and damage.
Cadence preserves phase across render frames without a burst of catch-up shots after a stall.
Remote-owner visual recoil interpolates replicated punch; damage still uses server punch.
