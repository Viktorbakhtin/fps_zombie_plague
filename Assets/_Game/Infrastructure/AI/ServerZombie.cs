using FishNet.Object;
using UnityEngine;
using ZombiePlague.Domain.Actors;
using ZombiePlague.Infrastructure.Actors;

namespace ZombiePlague.Infrastructure.AI
{
    [RequireComponent(typeof(CharacterController), typeof(NetworkActor))]
    public sealed class ServerZombie : NetworkBehaviour
    {
        private NetworkActor actor;
        private CharacterController controller;
        private float nextAttack, vertical;
        private void Awake() { actor = GetComponent<NetworkActor>(); controller = GetComponent<CharacterController>(); }
        private void Update()
        {
            if (!IsServerInitialized || !actor.Alive) return;
            NetworkActor target = null; float best = 20f * 20f;
            foreach (var candidate in NetworkActor.ServerActors)
            {
                if (!candidate.Alive || candidate.Faction != Faction.Human) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; target = candidate; }
            }
            Vector3 move = Vector3.zero;
            if (target != null)
            {
                Vector3 offset = target.transform.position - transform.position; offset.y = 0;
                if (offset.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(offset);
                if (best > 1.8f * 1.8f) move = offset.normalized * actor.Config.ZombieSpeed;
                else if (Time.time >= nextAttack && !Physics.Linecast(actor.Eyes, target.Eyes, out var hit,
                    ~0, QueryTriggerInteraction.Ignore)) TryAttack(target);
                else if (Time.time >= nextAttack && Physics.Raycast(actor.Eyes, (target.Eyes - actor.Eyes).normalized,
                    out var contact, 2.2f) && contact.collider.GetComponentInParent<NetworkActor>() == target) TryAttack(target);
            }
            if (controller.isGrounded && vertical < 0) vertical = -2;
            vertical += Physics.gravity.y * Time.deltaTime;
            controller.Move((move + actor.Knockback.Step(Time.deltaTime, actor.Config.KnockbackDrag) + Vector3.up * vertical) * Time.deltaTime);
        }
        private void TryAttack(NetworkActor target) { nextAttack = Time.time + 1f; actor.Attack(target, 25, true); }
    }
}
