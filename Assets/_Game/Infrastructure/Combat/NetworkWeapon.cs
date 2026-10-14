using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;
using ZombiePlague.Domain.Actors;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Player;
using ZombiePlague.Domain.Combat;
using ZombiePlague.Infrastructure.Bootstrap;

namespace ZombiePlague.Infrastructure.Combat
{
    [RequireComponent(typeof(NetworkActor))]
    public sealed class NetworkWeapon : NetworkBehaviour
    {
        private readonly SyncVar<int> ammo = new SyncVar<int>();
        private readonly SyncVar<Vector2> punch = new SyncVar<Vector2>();
        private readonly Ak47Accuracy accuracy = new Ak47Accuracy();
        private bool recoilDirection;
        private bool smokeShot;
        private float clientStarted;
        private NetworkActor actor;
        private ServerFpsMotor motor;
        private CharacterController controller;
        private float nextShot, nextRequest, reloadUntil, nextMelee;
        private bool triggerHeld;
        private bool clientTrigger;
        private Vector3 triggerAim;
        private float inputExpires, lastInput;
        private Vector2 displayedPunch;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        public int Ammo => ammo.Value;
        public Vector2 Punch => IsServerInitialized ? punch.Value : displayedPunch;
        public float LastShotSpread { get; private set; }
        public Vector3 LastShotDirection { get; private set; }
        public bool IsReloading => reloadUntil > 0;
        private Actors.ActorPresentation presentation;
        private bool Grounded => motor != null ? motor.Grounded : controller.isGrounded;
        private float SpeedUnits => motor != null ? motor.SpeedUnits : new Vector2(controller.velocity.x, controller.velocity.z).magnitude / 0.0254f;
        public void ResetOnServer() { if (!IsServerInitialized) return; ammo.Value = actor.Config.MagazineSize; accuracy.Reset(); punch.Value = Vector2.zero; recoilDirection = triggerHeld = false; reloadUntil = nextShot = nextRequest = nextMelee = 0; }
        private void Awake() { actor = GetComponent<NetworkActor>(); motor = GetComponent<ServerFpsMotor>(); controller = GetComponent<CharacterController>(); presentation = GetComponent<Actors.ActorPresentation>(); }
        public override void OnStartServer() { base.OnStartServer(); ammo.Value = actor.Config.MagazineSize; }
        public override void OnStartClient() { base.OnStartClient(); clientStarted = Time.time; }
        private void Update()
        {
            if (!IsServerInitialized) displayedPunch = Vector2.Lerp(displayedPunch, punch.Value, 1 - Mathf.Exp(-35 * Time.deltaTime));
            if (IsServerInitialized && triggerHeld && Time.time <= inputExpires) FireOnServer(triggerAim);
            if (IsServerInitialized && reloadUntil > 0 && Time.time >= reloadUntil)
            { ammo.Value = actor.Config.MagazineSize; reloadUntil = 0; accuracy.Reset(); }
            if (IsServerInitialized)
            {
                if (!triggerHeld || Time.time > inputExpires) accuracy.Release(Time.time);
                float length = punch.Value.magnitude;
                if (length > 0) punch.Value *= Mathf.Max(0, length - (10 + length * 0.5f) * Time.deltaTime) / length;
                if (accuracy.Shots == 0 && punch.Value.sqrMagnitude < 0.0001f) recoilDirection = false;
            }
            if (PrototypeSmoke.Enabled && IsOwner && !smokeShot && actor.Alive && Time.time - clientStarted > 0.75f)
            {
                foreach (var target in FindObjectsByType<NetworkActor>(FindObjectsSortMode.None))
                    if (target.StartsAsZombie && target.Alive)
                    { RequestFire((target.transform.position + Vector3.up * 0.9f - actor.Eyes).normalized); smokeShot = true; Debug.Log("[ZP] Smoke owner submitted shot"); break; }
            }
            if (!IsOwner || motor == null || !motor.AcceptLocalInput) return;
            bool held = actor.Alive && motor.AcceptLocalInput && Cursor.lockState == CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (held != clientTrigger || held && Time.time >= nextRequest)
            {
                clientTrigger = held;
                nextRequest = Time.time + 0.05f;
                RequestTrigger(held, motor.AimDirection);
            }
            if (!actor.Alive || !motor.AcceptLocalInput || Cursor.lockState != CursorLockMode.Locked) return;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RequestReload();
        }

        // Send input intent; the server alone schedules the automatic weapon cadence.
        [ServerRpc]
        private void RequestTrigger(bool held, Vector3 direction)
        {
            if (held && Time.time < lastInput + 0.01f || !ValidAim(direction)) return;
            lastInput = Time.time;
            SetTriggerOnServer(held, direction);
        }
        public void SetTriggerOnServer(bool held, Vector3 direction)
        {
            if (!IsServerInitialized || !ValidAim(direction)) return;
            triggerHeld = held; triggerAim = direction; inputExpires = Time.time + 0.25f;
        }
        private static bool ValidAim(Vector3 direction) => ServerFpsMotor.Finite(direction.x) && ServerFpsMotor.Finite(direction.y) &&
            ServerFpsMotor.Finite(direction.z) && direction.sqrMagnitude >= 0.9f && direction.sqrMagnitude <= 1.1f;

        [ServerRpc]
        private void RequestReload()
        {
            ReloadOnServer();
        }
        public void ReloadOnServer()
        {
            if (!IsServerInitialized) return;
            if (!actor.Alive || actor.Faction != Faction.Human || reloadUntil > 0 ||
                ammo.Value >= actor.Config.MagazineSize) return;
            reloadUntil = Time.time + 2.45f;
            ReloadFeedback();
        }

        [ServerRpc]
        private void RequestFire(Vector3 direction)
        {
            FireOnServer(direction);
        }
        public void FireOnServer(Vector3 direction)
        {
            if (!IsServerInitialized) return;
            if (!actor.Alive || !ServerFpsMotor.Finite(direction.x) || !ServerFpsMotor.Finite(direction.y) ||
                !ServerFpsMotor.Finite(direction.z) || direction.sqrMagnitude < 0.9f || direction.sqrMagnitude > 1.1f) return;
            if (actor.Faction == Faction.Zombie)
            {
                if (!actor.CombatEnabled) return;
                if (Time.time < nextMelee) return;
                nextMelee = Time.time + 0.75f;
                Hit(direction, 2f, true);
                return;
            }
            if (Time.time < nextShot || reloadUntil > 0 || ammo.Value <= 0) return;
            // Preserve the phase across render frames without accumulating shots after a stall.
            nextShot = Time.time - nextShot >= actor.Config.FireInterval ? Time.time + actor.Config.FireInterval : nextShot + actor.Config.FireInterval;
            ammo.Value--;
            float spread = accuracy.Spread(Grounded, SpeedUnits);
            LastShotSpread = spread;
            accuracy.Fired();
            Vector3 angles = Quaternion.LookRotation(direction).eulerAngles;
            Vector3 aimed = Quaternion.Euler(angles.x + punch.Value.x, angles.y + punch.Value.y, 0) * Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, aimed).normalized;
            Vector3 up = Vector3.Cross(aimed, right).normalized;
            float x = Random.Range(-0.5f, 0.5f) + Random.Range(-0.5f, 0.5f);
            float y = Random.Range(-0.5f, 0.5f) + Random.Range(-0.5f, 0.5f);
            LastShotDirection = (aimed + right * x * spread + up * y * spread).normalized;
            Hit(LastShotDirection, actor.Config.WeaponRange, false);
            KickBack();
            ShotFeedback();
        }

        [ObserversRpc] private void ShotFeedback() { if (presentation != null) presentation.Shot(); }
        [ObserversRpc] private void ReloadFeedback() { if (presentation != null) presentation.Reload(); }

        private void KickBack()
        {
            float upBase, sideBase, upMod, sideMod, upMax, sideMax; int change;
            if (SpeedUnits > 0) { upBase = 1.5f; sideBase = 0.45f; upMod = 0.225f; sideMod = 0.05f; upMax = 6.5f; sideMax = 2.5f; change = 7; }
            else if (!Grounded) { upBase = 2; sideBase = 1; upMod = 0.5f; sideMod = 0.35f; upMax = 9; sideMax = 6; change = 5; }
            else if (motor != null && motor.Ducking) { upBase = 0.9f; sideBase = 0.35f; upMod = 0.15f; sideMod = 0.025f; upMax = 5.5f; sideMax = 1.5f; change = 9; }
            else { upBase = 1; sideBase = 0.375f; upMod = 0.175f; sideMod = 0.0375f; upMax = 5.75f; sideMax = 1.75f; change = 8; }
            float count = accuracy.Shots == 1 ? 0 : accuracy.Shots;
            var value = punch.Value;
            value.x = Mathf.Max(-upMax, value.x - upBase - count * upMod);
            value.y = Mathf.Clamp(value.y + (recoilDirection ? 1 : -1) * (sideBase + count * sideMod), -sideMax, sideMax);
            punch.Value = value;
            if (Random.Range(0, change + 1) == 0) recoilDirection = !recoilDirection;
        }

        private void Hit(Vector3 direction, float range, bool contact)
        {
            int count = Physics.RaycastNonAlloc(actor.Eyes, direction.normalized, hits, range, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue; NetworkActor target = null; int nearestIndex = -1;
            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i].collider.GetComponentInParent<NetworkActor>();
                if (candidate == actor || hits[i].distance >= nearest) continue;
                nearest = hits[i].distance; target = candidate; nearestIndex = i;
            }
            if (target != null)
            {
                int damage = contact ? actor.Config.WeaponDamage : Mathf.Max(1, Mathf.FloorToInt(36 * Mathf.Pow(0.98f, nearest / 0.0254f / 500f)));
                if (actor.Attack(target, damage, contact) && !contact)
                    target.ApplyBulletKnockbackOnServer(direction, damage, nearest);
            }
            if (!contact && target == null && nearestIndex >= 0) ImpactFeedback(hits[nearestIndex].point, hits[nearestIndex].normal);
        }
        [ObserversRpc] private void ImpactFeedback(Vector3 point, Vector3 normal)
        { if (CombatEffects.Instance != null) CombatEffects.Instance.Impact(point, normal); }
    }
}

