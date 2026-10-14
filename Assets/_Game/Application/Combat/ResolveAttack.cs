using System;
using ZombiePlague.Domain.Actors;

namespace ZombiePlague.Application.Combat
{
    public interface ICombatRules
    {
        bool CanAttack(Faction attacker, Faction target);
        bool InfectsOnContact { get; }
    }

    public sealed class ClassicInfectionRules : ICombatRules
    {
        public bool CanAttack(Faction attacker, Faction target) => attacker != target;
        public bool InfectsOnContact => true;
    }

    public sealed class OutbreakRules : ICombatRules
    {
        public bool CanAttack(Faction attacker, Faction target) =>
            attacker == Faction.Human || target == Faction.Human;
        public bool InfectsOnContact => false;
    }

    // Server adapter validates ownership, range, line of sight and cooldown first.
    public sealed class ResolveAttack
    {
        private readonly ICombatRules rules;
        public ResolveAttack(ICombatRules rules) =>
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));

        public bool Execute(ActorState attacker, ActorState target, int damage,
            bool isContact, int zombieHealth)
        {
            if (attacker == null || target == null || ReferenceEquals(attacker, target) ||
                !attacker.IsAlive || !target.IsAlive ||
                !rules.CanAttack(attacker.Faction, target.Faction)) return false;

            if (isContact && rules.InfectsOnContact && attacker.Faction == Faction.Zombie)
                return target.TryInfect(zombieHealth);

            target.ApplyDamage(damage);
            return true;
        }
    }
}
