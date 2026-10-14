using System;

namespace ZombiePlague.Domain.Actors
{
    public enum Faction { Human, Zombie }

    // Mutated only by authoritative application use cases.
    public sealed class ActorState
    {
        public Faction Faction { get; private set; }
        public int Health { get; private set; }
        public bool IsAlive => Health > 0;

        public ActorState(int health, Faction faction = Faction.Human)
        {
            if (health <= 0) throw new ArgumentOutOfRangeException(nameof(health));
            Health = health;
            Faction = faction;
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Health = Math.Max(0, Health - amount);
        }

        public bool TryInfect(int zombieHealth)
        {
            if (zombieHealth <= 0) throw new ArgumentOutOfRangeException(nameof(zombieHealth));
            if (!IsAlive || Faction == Faction.Zombie) return false;
            Faction = Faction.Zombie;
            Health = zombieHealth;
            return true;
        }
    }
}
