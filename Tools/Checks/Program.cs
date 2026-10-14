using System;
using ZombiePlague.Domain.Actors;
using ZombiePlague.Application.Combat;

static void Check(bool value) { if (!value) throw new Exception("Rule check failed"); }
var zombie = new ActorState(250, Faction.Zombie);
var human = new ActorState(100);
var classic = new ResolveAttack(new ClassicInfectionRules());
Check(classic.Execute(zombie, human, 25, true, 250));
Check(human.Faction == Faction.Zombie && human.Health == 250);
Check(!classic.Execute(zombie, human, 25, true, 250));
human = new ActorState(100);
Check(new ResolveAttack(new OutbreakRules()).Execute(zombie, human, 25, true, 250));
Check(human.Faction == Faction.Human && human.Health == 75);
human.ApplyDamage(int.MaxValue);
Check(!human.IsAlive && !human.TryInfect(250));
Check(!classic.Execute(human, zombie, 25, false, 250));
Console.WriteLine("PASS: infection, mode rules, damage clamp, dead actor guards.");
