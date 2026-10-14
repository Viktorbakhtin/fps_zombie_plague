using NUnit.Framework;
using ZombiePlague.Domain.Actors;
using ZombiePlague.Application.Combat;

namespace ZombiePlague.Tests
{
    public sealed class CombatTests
    {
        [Test] public void ClassicContactInfectsOnce()
        {
            var zombie = new ActorState(250, Faction.Zombie);
            var human = new ActorState(100);
            var attack = new ResolveAttack(new ClassicInfectionRules());
            Assert.That(attack.Execute(zombie, human, 25, true, 250), Is.True);
            Assert.That(human.Faction, Is.EqualTo(Faction.Zombie));
            Assert.That(human.Health, Is.EqualTo(250));
            Assert.That(attack.Execute(zombie, human, 25, true, 250), Is.False);
        }

        [Test] public void OutbreakContactDamagesWithoutInfection()
        {
            var human = new ActorState(100);
            new ResolveAttack(new OutbreakRules()).Execute(
                new ActorState(250, Faction.Zombie), human, 25, true, 250);
            Assert.That(human.Health, Is.EqualTo(75));
            Assert.That(human.Faction, Is.EqualTo(Faction.Human));
        }

        [Test] public void DeadActorsCannotAttackOrBecomeInfected()
        {
            var dead = new ActorState(100);
            dead.ApplyDamage(100);
            Assert.That(dead.TryInfect(250), Is.False);
            Assert.That(new ResolveAttack(new ClassicInfectionRules()).Execute(
                dead, new ActorState(250, Faction.Zombie), 25, false, 250), Is.False);
        }

        [Test] public void ExcessDamageClampsHealthToZero()
        {
            var actor = new ActorState(100);
            actor.ApplyDamage(int.MaxValue);
            Assert.That(actor.Health, Is.Zero);
        }
    }
}
