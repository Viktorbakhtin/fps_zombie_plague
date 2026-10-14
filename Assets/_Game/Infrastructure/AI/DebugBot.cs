using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;
using ZombiePlague.Domain.Actors;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Combat;
using ZombiePlague.Infrastructure.Configs;

namespace ZombiePlague.Infrastructure.AI
{
    [RequireComponent(typeof(CharacterController), typeof(NetworkActor), typeof(NetworkWeapon))]
    public sealed class DebugBot : NetworkBehaviour
    {
        public BotConfig Config;
        public string State { get; private set; } = "Patrol";
        public bool HasVisibleTarget { get; private set; }
        private NetworkActor actor, target;
        private CharacterController controller;
        private NetworkWeapon weapon;
        private NavMeshPath path;
        private readonly Vector3[] corners = new Vector3[32];
        private readonly RaycastHit[] sightHits = new RaycastHit[16];
        private Vector3 destination, steering, aim, lastSeen;
        private float nextThink, seenUntil, readyAt, nextBurst, nextTactic, vertical;
        private int burstAmmo, side = 1;
        private bool bursting;
        private void Awake()
        {
            actor = GetComponent<NetworkActor>(); controller = GetComponent<CharacterController>();
            weapon = GetComponent<NetworkWeapon>(); path = new NavMeshPath();
        }
        private void OnDisable() { if (weapon != null) weapon.SetTriggerOnServer(false, Vector3.forward); }
        public void ResetMotionOnServer()
        {
            if (!IsServerInitialized) return;
            target = null; HasVisibleTarget = bursting = false; vertical = nextThink = nextBurst = seenUntil = 0;
            destination = transform.position; steering = transform.position; State = "Patrol";
            weapon.SetTriggerOnServer(false, Vector3.forward);
        }
        private void Update()
        {
            if (!IsServerInitialized || !actor.Alive || Config == null) return;
            if (!actor.CombatEnabled) { State = "Waiting for round"; weapon.SetTriggerOnServer(false, transform.forward); return; }
            if (Time.time >= nextThink) { nextThink = Time.time + Config.ThinkInterval; Think(); }
            if (target != null && (!target.Alive || target.Faction == actor.Faction)) { target = null; HasVisibleTarget = false; }
            Vector3 move = Navigate();
            if (target != null && HasVisibleTarget)
            {
                Vector3 desired = (target.Eyes - Vector3.up * 0.35f - actor.Eyes).normalized;
                aim = Vector3.RotateTowards(aim.sqrMagnitude < 0.1f ? transform.forward : aim, desired, 8 * Time.deltaTime, 0);
                Vector3 facing = desired; facing.y = 0;
                if (facing.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), 1 - Mathf.Exp(-12 * Time.deltaTime));
                bool canAttack = Time.time >= readyAt && Visible(target) && Vector3.Angle(aim, desired) < 6;
                if (actor.Faction == Faction.Zombie)
                    weapon.SetTriggerOnServer(canAttack && (target.transform.position - transform.position).sqrMagnitude < 3.2f, desired);
                else
                {
                    if (weapon.Ammo == 0) weapon.ReloadOnServer();
                    if (bursting && (burstAmmo - weapon.Ammo >= Config.BurstShots || weapon.IsReloading || !canAttack))
                    { bursting = false; nextBurst = Time.time + Config.BurstPause + Random.Range(0, 0.25f); }
                    if (!bursting && canAttack && !weapon.IsReloading && weapon.Ammo > 0 && Time.time >= nextBurst)
                    { bursting = true; burstAmmo = weapon.Ammo; }
                    weapon.SetTriggerOnServer(bursting && canAttack, aim);
                }
            }
            else
            {
                weapon.SetTriggerOnServer(false, transform.forward); bursting = false;
                if (move.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 1 - Mathf.Exp(-8 * Time.deltaTime));
                if (actor.Faction == Faction.Human && weapon.Ammo < Config.BurstShots) weapon.ReloadOnServer();
            }
            if (controller.isGrounded && vertical < 0) vertical = -2;
            vertical += Physics.gravity.y * Time.deltaTime;
            controller.Move((move + actor.Knockback.Step(Time.deltaTime, actor.Config.KnockbackDrag) + Vector3.up * vertical) * Time.deltaTime);
        }
        private void Think()
        {
            NetworkActor visible = null; float best = float.MaxValue;
            foreach (var candidate in NetworkActor.ServerActors)
            {
                if (candidate == actor || !candidate.Alive || candidate.Faction == actor.Faction) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance > Config.SightRange * Config.SightRange || !Visible(candidate)) continue;
                float score = candidate == target ? distance * 0.6f : distance;
                if (score < best) { best = score; visible = candidate; }
            }
            HasVisibleTarget = visible != null;
            if (visible != null)
            {
                if (target != visible) { readyAt = Time.time + Config.ReactionTime; bursting = false; }
                target = visible; lastSeen = visible.transform.position; seenUntil = Time.time + Config.MemoryDuration;
                Vector3 away = transform.position - visible.transform.position; away.y = 0;
                if (actor.Faction == Faction.Zombie) { State = "Hunt"; destination = visible.transform.position; }
                else
                {
                    if (Time.time >= nextTactic) { nextTactic = Time.time + Random.Range(1.2f, 2.2f); side = Random.value < 0.5f ? -1 : 1; }
                    State = weapon.IsReloading ? "Reload / retreat" : away.magnitude < Config.HumanDistance - 1 ? "Retreat" : "Combat";
                    destination = visible.transform.position + away.normalized * Config.HumanDistance + Vector3.Cross(Vector3.up, away.normalized) * side * 1.5f;
                }
            }
            else if (target != null && Time.time < seenUntil) { State = "Investigate"; destination = lastSeen; }
            else
            {
                target = null; State = "Patrol";
                if ((destination - transform.position).sqrMagnitude < 1 || Time.time >= nextTactic)
                { destination = transform.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f)); nextTactic = Time.time + 3; }
            }
            steering = transform.position;
            NavMeshHit start, end;
            if (NavMesh.SamplePosition(transform.position, out start, 2, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(destination, out end, 3, NavMesh.AllAreas) && NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path))
            {
                int count = path.GetCornersNonAlloc(corners);
                for (int i = 1; i < count; i++)
                {
                    Vector3 offset = corners[i] - transform.position; offset.y = 0;
                    if (offset.sqrMagnitude > 0.01f) { steering = corners[i]; break; }
                }
            }
        }
        private Vector3 Navigate()
        {
            Vector3 direction = steering - transform.position; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f || actor.Faction == Faction.Zombie && HasVisibleTarget &&
                target != null && (target.transform.position - transform.position).sqrMagnitude < 2) return Vector3.zero;
            direction.Normalize();
            Vector3 separation = Vector3.zero;
            foreach (var other in NetworkActor.ServerActors)
            {
                if (other == actor || !other.Alive) continue;
                Vector3 offset = transform.position - other.transform.position; offset.y = 0;
                if (offset.sqrMagnitude > 0.01f && offset.sqrMagnitude < 1) separation += offset.normalized * (1 - offset.magnitude);
            }
            return (direction + separation).normalized * (actor.Faction == Faction.Zombie ? Config.ZombieSpeed : Config.HumanSpeed);
        }
        private bool Visible(NetworkActor candidate)
        {
            Vector3 delta = candidate.Eyes - Vector3.up * 0.35f - actor.Eyes;
            int count = Physics.RaycastNonAlloc(actor.Eyes, delta.normalized, sightHits, delta.magnitude + 0.1f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue; NetworkActor first = null;
            for (int i = 0; i < count; i++)
            {
                var hitActor = sightHits[i].collider.GetComponentInParent<NetworkActor>();
                if (hitActor == actor || sightHits[i].distance >= nearest) continue;
                nearest = sightHits[i].distance; first = hitActor;
            }
            return first == candidate;
        }
    }
}
