using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using ZombiePlague.Domain.Actors;
using ZombiePlague.Application.Combat;
using ZombiePlague.Infrastructure.Configs;

namespace ZombiePlague.Infrastructure.Actors
{
    public sealed class NetworkActor : NetworkBehaviour
    {
        public static readonly HashSet<NetworkActor> ServerActors = new HashSet<NetworkActor>();
        public static NetworkActor LocalPlayer { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearRegistry() { ServerActors.Clear(); LocalPlayer = null; }
        private void OnDestroy() { ServerActors.Remove(this); if (LocalPlayer == this) LocalPlayer = null; }
        public override void OnStartClient() { base.OnStartClient(); if (IsOwner) LocalPlayer = this; }
        public PrototypeConfig Config;
        public bool StartsAsZombie;
        public Renderer Body;
        private readonly SyncVar<int> health = new SyncVar<int>();
        private readonly SyncVar<Faction> faction = new SyncVar<Faction>();
        private ActorState state;
        private ResolveAttack attack;
        private MaterialPropertyBlock block;
        private int lastHealth = -1;
        private Faction lastFaction;
        private CharacterController controller;
        private void Awake() => controller = GetComponent<CharacterController>();
        public int Health => health.Value;
        public Faction Faction => faction.Value;
        public bool Alive => Health > 0;
        public bool CombatEnabled { get; private set; } = true;
        public Combat.KnockbackMotion Knockback { get; } = new Combat.KnockbackMotion();
        public void ApplyBulletKnockbackOnServer(Vector3 direction, int damage, float distance)
        {
            if (!IsServerInitialized || !Alive || !CombatEnabled || Faction != Faction.Zombie ||
                Config.Mode != PrototypeMode.ClassicInfection || damage <= 0 || distance > Config.KnockbackRange) return;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.0001f) return;
            float multiplier = controller.height < 1.2f ? Config.CrouchedKnockback : 1;
            Knockback.Add(direction.normalized * damage * Config.BulletKnockbackPerDamage * multiplier, Config.KnockbackMaximumSpeed);
        }
        public void SetCombatOnServer(bool enabled) { if (IsServerInitialized) CombatEnabled = enabled; }
        public Vector3 Eyes => transform.position + Vector3.up * (controller == null ? 1.6f : controller.height * (controller.height < 1.2f ? 30f/36f : 64f/72f));

        public override void OnStartServer()
        {
            base.OnStartServer();
            Knockback.Clear();
            state = new ActorState(StartsAsZombie ? Config.ZombieHealth : Config.HumanHealth,
                StartsAsZombie ? Faction.Zombie : Faction.Human);
            attack = new ResolveAttack(Config.Mode == PrototypeMode.ClassicInfection
                ? (ICombatRules)new ClassicInfectionRules() : new OutbreakRules());
            Publish();
            ServerActors.Add(this);
            if (ZombiePlague.Infrastructure.Bootstrap.PrototypeSmoke.Enabled) Debug.Log("[ZP] Spawn " + name + " owner=" + OwnerId + " hp=" + Health + " faction=" + Faction);
        }

        public override void OnStopServer()
        {
            ServerActors.Remove(this);
            base.OnStopServer();
        }

        public bool Attack(NetworkActor target, int damage, bool contact)
        {
            if (!IsServerInitialized || !CombatEnabled || target == null || !target.IsServerInitialized || !target.CombatEnabled) return false;
            bool accepted = attack.Execute(state, target.state, damage, contact, Config.ZombieHealth);
            if (accepted) { target.Publish(); if (ZombiePlague.Infrastructure.Bootstrap.PrototypeSmoke.Enabled) Debug.Log("[ZP] Attack target=" + target.name + " hp=" + target.Health + " faction=" + target.Faction); }
            return accepted;
        }

        private void Publish() { health.Value = state.Health; faction.Value = state.Faction; }
        public bool InfectOnServer(bool firstZombie = false)
        {
            if (!IsServerInitialized || !state.TryInfect(firstZombie ? Config.FirstZombieHealth : Config.ZombieHealth)) return false;
            Publish(); return true;
        }
        public void ResetHumanOnServer()
        {
            if (!IsServerInitialized) return;
            state = new ActorState(Config.HumanHealth); Knockback.Clear(); Publish();
        }

        private void Update()
        {
            if (Body == null) return;
            if (lastHealth == Health && lastFaction == Faction) return;
            lastHealth = Health; lastFaction = Faction;
            if (block == null) block = new MaterialPropertyBlock();
            Body.GetPropertyBlock(block);
            block.SetColor("_BaseColor", !Alive ? Color.gray : Faction == Faction.Zombie
                ? new Color(0.3f, 0.8f, 0.2f) : new Color(0.2f, 0.6f, 1f));
            Body.SetPropertyBlock(block);
        }
    }
}
