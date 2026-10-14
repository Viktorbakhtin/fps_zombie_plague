#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ZombiePlague.Domain.Actors;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Bootstrap;
using ZombiePlague.Infrastructure.Player;
using ZombiePlague.Infrastructure.Combat;
using ZombiePlague.Domain.Combat;
using System.Reflection;

namespace ZombiePlague.Tests
{
    public sealed class DebugArenaTests
    {
        [UnitySetUp]
        public IEnumerator ClosePreviousHost()
        {
            foreach (var manager in Object.FindObjectsByType<FishNet.Managing.NetworkManager>(FindObjectsSortMode.None))
            {
                manager.ClientManager.StopConnection(); manager.ServerManager.StopConnection(true);
                Object.Destroy(manager.gameObject);
            }
            yield return null; yield return null;
        }
        [UnityTest]
        public IEnumerator BulletsPushZombieBotsAndPredictedPlayers()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/DebugArena.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; float timeout = Time.realtimeSinceStartup + 20; DebugRound round = null;
            while (Time.realtimeSinceStartup < timeout)
            {
                round = Object.FindFirstObjectByType<DebugRound>();
                if (round != null && round.ParticipantCount == 3 && NetworkActor.LocalPlayer != null) break;
                yield return null;
            }
            Assert.That(round, Is.Not.Null); Assert.That(round.ParticipantCount, Is.EqualTo(3)); round.enabled = false;
            NetworkActor target = null, shooterBot = null;
            foreach (var actor in NetworkActor.ServerActors)
            {
                actor.ResetHumanOnServer(); actor.SetCombatOnServer(true);
                var bot = actor.GetComponent<ZombiePlague.Infrastructure.AI.DebugBot>();
                if (bot != null) { bot.enabled = false; if (target == null) target = actor; else shooterBot = actor; }
            }
            var player = NetworkActor.LocalPlayer; var motor = player.GetComponent<ServerFpsMotor>(); motor.AcceptLocalInput = false;
            Teleport(player, new Vector3(-5, 0.1f, -5)); Teleport(target, new Vector3(-5, 0.1f, -2));
            yield return new WaitForSeconds(0.3f); Physics.SyncTransforms();
            var weapon = player.GetComponent<NetworkWeapon>(); weapon.ResetOnServer();
            weapon.FireOnServer((target.Eyes - Vector3.up * 0.5f - player.Eyes).normalized);
            Assert.That(target.Knockback.Velocity, Is.EqualTo(Vector3.zero), "Human teammates must not receive zombie knockback");
            target.InfectOnServer(true); Assert.That(target.Health, Is.EqualTo(3000));
            target.SetCombatOnServer(false); weapon.ResetOnServer();
            weapon.FireOnServer((target.Eyes - Vector3.up * 0.5f - player.Eyes).normalized);
            Assert.That(target.Knockback.Velocity, Is.EqualTo(Vector3.zero), "Blocked warmup damage must not create an impulse"); target.SetCombatOnServer(true);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(-5, 1.5f, -3.5f); wall.transform.localScale = new Vector3(2, 3, 0.2f);
            Physics.SyncTransforms(); weapon.ResetOnServer(); weapon.FireOnServer(Vector3.forward);
            Assert.That(target.Knockback.Velocity, Is.EqualTo(Vector3.zero), "A wall-blocked bullet must not push a zombie");
            Object.Destroy(wall); yield return new WaitForSeconds(0.12f);
            shooterBot.InfectOnServer(); var brain = target.GetComponent<ZombiePlague.Infrastructure.AI.DebugBot>(); brain.ResetMotionOnServer(); brain.enabled = true;
            float before = target.transform.position.z; weapon.ResetOnServer();
            for (int i = 0; i < 10; i++)
            {
                Vector3 delta = (target.Eyes - Vector3.up * 0.5f - player.Eyes).normalized;
                float pitch = -Mathf.Asin(delta.y) * Mathf.Rad2Deg;
                float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                weapon.FireOnServer(Quaternion.Euler(pitch - weapon.Punch.x, yaw - weapon.Punch.y, 0) * Vector3.forward);
                yield return new WaitForSeconds(0.1f);
            }
            Assert.That(target.Health, Is.LessThan(3000)); Assert.That(player.Faction, Is.EqualTo(Faction.Human));
            Assert.That(target.transform.position.z, Is.GreaterThan(before + 0.3f), "A running zombie must move backwards under a sustained accurate burst");
            Assert.That(target.Knockback.Velocity.magnitude, Is.LessThanOrEqualTo(target.Config.KnockbackMaximumSpeed + 0.01f));
            brain.enabled = false; target.ResetHumanOnServer();
            Assert.That(target.Knockback.Velocity, Is.EqualTo(Vector3.zero), "Round reset must clear external motion");
            player.InfectOnServer(true); shooterBot.ResetHumanOnServer();
            Teleport(player, new Vector3(-5, 0.1f, -2)); Teleport(shooterBot, new Vector3(-5, 0.1f, -5));
            var botWeapon = shooterBot.GetComponent<NetworkWeapon>(); botWeapon.ResetOnServer();
            Physics.SyncTransforms(); before = player.transform.position.z;
            botWeapon.FireOnServer((player.Eyes - Vector3.up * 0.5f - shooterBot.Eyes).normalized);
            Assert.That(player.Knockback.Velocity.z, Is.GreaterThan(0)); Assert.That(player.Knockback.Velocity.y, Is.Zero);
            yield return new WaitForSeconds(0.25f);
            Assert.That(player.transform.position.z, Is.GreaterThan(before + 0.2f), "Authoritative impulse must survive player prediction/reconciliation");
            yield return new WaitForSeconds(2f);
            Assert.That(player.Knockback.Velocity.magnitude, Is.LessThan(0.05f));
        }
        [UnityTest]
        public IEnumerator StandingSprayUsesGroundSpreadAfterReconciliation()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/DebugArena.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; float timeout = Time.realtimeSinceStartup + 20;
            DebugRound round = null;
            while (Time.realtimeSinceStartup < timeout)
            {
                round = Object.FindFirstObjectByType<DebugRound>();
                if (round != null && round.ParticipantCount == 3 && NetworkActor.LocalPlayer != null) break;
                yield return null;
            }
            Assert.That(round, Is.Not.Null); Assert.That(round.ParticipantCount, Is.EqualTo(3)); round.enabled = false;
            foreach (var actor in NetworkActor.ServerActors)
            {
                var bot = actor.GetComponent<ZombiePlague.Infrastructure.AI.DebugBot>(); if (bot != null) bot.enabled = false;
                actor.ResetHumanOnServer(); actor.SetCombatOnServer(true);
            }
            var player = NetworkActor.LocalPlayer; var motor = player.GetComponent<ServerFpsMotor>();
            motor.AcceptLocalInput = false; Teleport(player, new Vector3(-5, 0.1f, -5));
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(motor.Grounded, Is.True, "Prediction/reconciliation must preserve standing ground contact");
                yield return null;
            }
            var weapon = player.GetComponent<NetworkWeapon>(); weapon.ResetOnServer();
            for (int i = 0; i < 30; i++)
            {
                Assert.That(motor.Grounded, Is.True);
                weapon.FireOnServer(Vector3.forward);
                Assert.That(weapon.LastShotSpread, Is.LessThanOrEqualTo(0.034375f), "Standing AK spread must not switch to the 0.54 air branch");
                Assert.That(weapon.LastShotDirection.y, Is.GreaterThan(-0.04f), "Horizontal standing fire cannot spray at the player's feet");
                yield return new WaitForSeconds(0.1f);
            }
            Assert.That(weapon.Ammo, Is.Zero);
            yield return new WaitForSeconds(1f); weapon.ReloadOnServer(); yield return new WaitForSeconds(2.6f);
            weapon.FireOnServer(Vector3.forward);
            Assert.That(weapon.LastShotSpread, Is.EqualTo(0.0055f).Within(0.00001f));
            Teleport(player, new Vector3(-5, 3, -5));
            yield return new WaitForSeconds(0.02f);
            Assert.That(motor.Grounded, Is.False, "An actual airborne actor must retain the air penalty");
            yield return new WaitForSeconds(0.1f); weapon.FireOnServer(Vector3.forward);
            Assert.That(weapon.LastShotSpread, Is.GreaterThanOrEqualTo(0.12f));
        }
        [UnityTest]
        public IEnumerator StartupProtectionAndSurfaceFeedback()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/DebugArena.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DebugRound round = null; float timeout = Time.realtimeSinceStartup + 20;
            while (Time.realtimeSinceStartup < timeout)
            {
                round = Object.FindFirstObjectByType<DebugRound>();
                if (round != null && round.ParticipantCount == 3 && NetworkActor.LocalPlayer != null) break;
                yield return null;
            }
            Assert.That(round, Is.Not.Null); Assert.That(round.ParticipantCount, Is.EqualTo(3));
            var player = NetworkActor.LocalPlayer; player.GetComponent<ServerFpsMotor>().AcceptLocalInput = false;
            for (int run = 0; run < 2; run++)
            {
                round.BeginRound(); float selectedAt = Time.time + round.InfectionDelay;
                timeout = Time.realtimeSinceStartup + round.InfectionDelay + round.InfectionGrace + 3;
                while (!round.CombatActive && Time.realtimeSinceStartup < timeout)
                {
                    int zombies = 0; NetworkActor infected = null, human = null;
                    foreach (var actor in NetworkActor.ServerActors)
                    {
                        Assert.That(actor.CombatEnabled, Is.False);
                        if (actor.Faction == Faction.Zombie) { zombies++; infected = actor; } else human = actor;
                    }
                    Assert.That(zombies, Is.InRange(0, 1), "Warmup and first-zombie grace must never cascade infection");
                    if (Time.time < selectedAt - 0.05f) Assert.That(zombies, Is.Zero, "No infection before countdown expires");
                    if (infected != null)
                    {
                        Assert.That(human, Is.Not.Null);
                        Assert.That(infected.Attack(human, 25, true), Is.False, "Server must reject infection during grace");
                        Assert.That(human.Faction, Is.EqualTo(Faction.Human));
                    }
                    yield return null;
                }
                Assert.That(round.CombatActive, Is.True);
                int finalZombies = 0;
                foreach (var actor in NetworkActor.ServerActors) { Assert.That(actor.CombatEnabled, Is.True); if (actor.Faction == Faction.Zombie) finalZombies++; }
                Assert.That(finalZombies, Is.EqualTo(1));
            }
            round.enabled = false;
            foreach (var actor in NetworkActor.ServerActors)
            {
                var bot = actor.GetComponent<ZombiePlague.Infrastructure.AI.DebugBot>(); if (bot != null) bot.enabled = false;
                actor.SetCombatOnServer(true);
            }
            player.ResetHumanOnServer(); Teleport(player, new Vector3(-5, 0.1f, -5));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(-5, 1.5f, -1); wall.transform.localScale = new Vector3(3, 3, 0.2f);
            Physics.SyncTransforms();
            var weapon = player.GetComponent<NetworkWeapon>(); weapon.ResetOnServer();
            var effects = CombatEffects.Instance; Assert.That(effects, Is.Not.Null);
            weapon.FireOnServer(Vector3.forward);
            Assert.That(weapon.Ammo, Is.EqualTo(29));
            timeout = Time.realtimeSinceStartup + 2;
            while (effects.ActiveImpactCount == 0 && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(effects.ActiveImpactCount, Is.GreaterThan(0), "A confirmed wall hit must show an impact");
            Assert.That(effects.LastImpactPoint.z, Is.EqualTo(-1.1f).Within(0.01f));
            Assert.That(Vector3.Dot(effects.LastImpactNormal, Vector3.back), Is.GreaterThan(0.99f));
            var presentation = player.GetComponent<ActorPresentation>();
            Assert.That(presentation.Muzzle.GetComponentsInChildren<ParticleSystem>(true).Length, Is.GreaterThan(0), "Confirmed fire must create the muzzle particle effect");
            for (int i = 0; i < 40; i++)
            {
                if (i % 20 == 0) weapon.ResetOnServer();
                yield return new WaitForSeconds(0.11f); weapon.FireOnServer(Vector3.forward);
            }
            yield return null;
            Assert.That(effects.ActiveImpactCount, Is.EqualTo(32), "Impact pool must stay bounded under sustained fire");
            yield return new WaitForSeconds(8.2f);
            Assert.That(effects.ActiveImpactCount, Is.Zero, "Old bullet marks must expire");
            Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator AutomaticCadenceAndBotVisibility()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/DebugArena.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float timeout = Time.realtimeSinceStartup + 20;
            DebugRound round = null;
            while (Time.realtimeSinceStartup < timeout)
            {
                round = Object.FindFirstObjectByType<DebugRound>();
                if (round != null && round.ParticipantCount == 3 && NetworkActor.LocalPlayer != null && NetworkActor.LocalPlayer.IsServerInitialized) break;
                yield return null;
            }
            Assert.That(round, Is.Not.Null); Assert.That(round.ParticipantCount, Is.EqualTo(3));
            round.enabled = false;
            var player = NetworkActor.LocalPlayer;
            var motor = player.GetComponent<ServerFpsMotor>(); motor.AcceptLocalInput = false;
            ZombiePlague.Infrastructure.AI.DebugBot bot = null;
            foreach (var actor in NetworkActor.ServerActors)
            {
                actor.ResetHumanOnServer();
                actor.SetCombatOnServer(true);
                var b = actor.GetComponent<ZombiePlague.Infrastructure.AI.DebugBot>();
                if (b != null) { b.enabled = false; bot = b; }
            }
            var weapon = player.GetComponent<NetworkWeapon>(); weapon.ResetOnServer();
            float until = Time.time + 1.2f;
            // Repeated input refreshes must never restart the server's fire timer.
            while (Time.time < until) { weapon.SetTriggerOnServer(true, Vector3.up); yield return null; }
            weapon.SetTriggerOnServer(false, Vector3.up);
            Assert.That(30 - weapon.Ammo, Is.InRange(12, 13), "Held automatic fire must preserve 0.0955s cadence");
            int remaining = weapon.Ammo;
            yield return new WaitForSeconds(0.3f);
            Assert.That(weapon.Ammo, Is.EqualTo(remaining), "Release must stop automatic fire");
            weapon.ResetOnServer(); weapon.SetTriggerOnServer(true, Vector3.up);
            yield return new WaitForSeconds(0.6f);
            remaining = weapon.Ammo; yield return new WaitForSeconds(0.3f);
            Assert.That(weapon.Ammo, Is.EqualTo(remaining), "Lost input must expire rather than continue shooting");

            Assert.That(bot, Is.Not.Null); Assert.That(bot.Config, Is.Not.Null);
            var config = Object.Instantiate(bot.Config); bot.Config = config; config.HumanSpeed = config.ZombieSpeed = 0;
            Teleport(player, new Vector3(-5, 0.1f, -4)); Teleport(bot.GetComponent<NetworkActor>(), new Vector3(-5, 0.1f, 0));
            player.InfectOnServer(); bot.GetComponent<NetworkWeapon>().ResetOnServer(); bot.ResetMotionOnServer();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(-5, 1.5f, -2); wall.transform.localScale = new Vector3(2, 3, 0.2f);
            Physics.SyncTransforms(); bot.enabled = true;
            yield return new WaitForSeconds(0.7f);
            Assert.That(bot.HasVisibleTarget, Is.False, "Bots cannot acquire targets through a wall");
            Assert.That(bot.GetComponent<NetworkWeapon>().Ammo, Is.EqualTo(30));
            Object.Destroy(wall); yield return new WaitForSeconds(1.3f);
            Assert.That(bot.HasVisibleTarget, Is.True);
            Assert.That(bot.GetComponent<NetworkWeapon>().Ammo, Is.InRange(24, 27), "Human bot must fire short bursts with pauses");
            var navPath = new UnityEngine.AI.NavMeshPath();
            UnityEngine.AI.NavMeshHit start, end;
            Assert.That(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(0, 0, 3), out start, 2, -1), Is.True);
            Assert.That(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(0, 0, -3), out end, 2, -1), Is.True);
            Assert.That(UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, -1, navPath), Is.True);
            Assert.That(navPath.status, Is.EqualTo(UnityEngine.AI.NavMeshPathStatus.PathComplete));
            Assert.That(navPath.corners.Length, Is.GreaterThan(2), "Navigation must route around arena cover");
            foreach (var actor in NetworkActor.ServerActors) if (actor != player) actor.InfectOnServer();
            player.ResetHumanOnServer(); config.ZombieSpeed = 3.4f;
            Teleport(bot.GetComponent<NetworkActor>(), new Vector3(0, 0.1f, 3));
            Teleport(player, new Vector3(0, 0.1f, -3)); bot.ResetMotionOnServer();
            var cover = GameObject.Find("Simulation cover").GetComponentsInChildren<Collider>();
            foreach (var collider in cover) collider.enabled = false;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.4f);
            Assert.That(bot.HasVisibleTarget, Is.True);
            foreach (var collider in cover) collider.enabled = true;
            Physics.SyncTransforms();
            timeout = Time.realtimeSinceStartup + 15;
            while (player.Faction == Faction.Human && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(player.Faction, Is.EqualTo(Faction.Zombie), "A zombie must pursue a remembered target around cover and infect it. Bot=" + bot.transform.position + " Player=" + player.transform.position + " State=" + bot.State);
            bot.enabled = false; Object.Destroy(config);
        }
        private static void Teleport(NetworkActor actor, Vector3 position)
        {
            var cc = actor.GetComponent<CharacterController>(); cc.enabled = false; actor.transform.position = position; cc.enabled = true;
            var motor = actor.GetComponent<ServerFpsMotor>(); if (motor != null) motor.ResetMotionOnServer();
        }
        [UnityTest]
        public IEnumerator AutoHostSpawnsBotsAndStartsRandomInfection()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Game/Scenes/DebugArena.unity", new LoadSceneParameters(LoadSceneMode.Single));
            float timeout = Time.realtimeSinceStartup + 25;
            DebugRound round = null;
            while (Time.realtimeSinceStartup < timeout)
            {
                round = Object.FindFirstObjectByType<DebugRound>();
                if (round != null && round.ParticipantCount == 3 && round.Status.StartsWith("First zombie:")) break;
                yield return null;
            }
            Assert.That(round, Is.Not.Null);
            Assert.That(round.ParticipantCount, Is.EqualTo(3));
            Assert.That(round.Status, Does.StartWith("First zombie:"));
            int humans = 0, zombies = 0;
            foreach (var actor in NetworkActor.ServerActors) { if (actor.Faction == Faction.Human) humans++; else zombies++; }
            Assert.That(humans, Is.EqualTo(2)); Assert.That(zombies, Is.EqualTo(1));
            foreach (var actor in NetworkActor.ServerActors) if (actor.Faction == Faction.Zombie)
                Assert.That(actor.Health, Is.EqualTo(actor.Config.FirstZombieHealth), "The initial zombie receives its separate health budget");
            int number = round.RoundNumber;
            foreach(var actor in NetworkActor.ServerActors) actor.InfectOnServer();
            yield return null;
            Assert.That(round.Status, Does.StartWith("Zombies win!"));
            yield return new WaitForSeconds(3.1f);
            Assert.That(round.RoundNumber, Is.EqualTo(number + 1));
            foreach(var actor in NetworkActor.ServerActors) Assert.That(actor.Faction, Is.EqualTo(Faction.Human));

            var player = NetworkActor.LocalPlayer;
            Assert.That(player, Is.Not.Null);
            var motor = player.GetComponent<ServerFpsMotor>();
            motor.AcceptLocalInput = false;
            motor.ResetMotionOnServer();
            var weapon = player.GetComponent<NetworkWeapon>();
            var accuracy = (Ak47Accuracy)typeof(NetworkWeapon).GetField("accuracy", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
            for(int i = 0; i < 5; i++) { weapon.FireOnServer(Vector3.up); yield return new WaitForSeconds(0.12f); }
            Assert.That(accuracy.Shots, Is.GreaterThan(0));
            yield return new WaitForSeconds(1f);
            Assert.That(accuracy.Shots, Is.Zero, "Burst must recover when fire is released");
            Assert.That(accuracy.Accuracy, Is.EqualTo(0.2f).Within(0.00001f));
            Assert.That(weapon.Punch.sqrMagnitude, Is.LessThan(0.0001f));

            typeof(ServerFpsMotor).GetField("yaw",BindingFlags.Instance | BindingFlags.NonPublic).SetValue(motor,10f);
            typeof(ServerFpsMotor).GetField("pitch",BindingFlags.Instance | BindingFlags.NonPublic).SetValue(motor,5f);
            motor.transform.rotation = Quaternion.Euler(0,60,0);
            typeof(ServerFpsMotor).GetMethod("LateUpdate",BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motor,null);
            Assert.That(Vector3.Angle(motor.View.transform.forward,Quaternion.Euler(5,10,0)*Vector3.forward), Is.LessThan(0.01f),
                "Network root rotation must not change the camera's mouse aim");

            round.enabled = false;
            ZombiePlague.Infrastructure.Actors.NetworkActor target = null;
            foreach(var a in NetworkActor.ServerActors)
            {
                a.SetCombatOnServer(true);
                var bot = a.GetComponent<ZombiePlague.Infrastructure.AI.DebugBot>();
                if(bot != null) { bot.enabled = false; target = a; }
            }
            weapon.ResetOnServer();
            weapon.FireOnServer(Vector3.up);
            Assert.That(weapon.Ammo, Is.EqualTo(29));
            weapon.FireOnServer(Vector3.up);
            Assert.That(weapon.Ammo, Is.EqualTo(29), "Fire cooldown must reject a second shot in the same frame");
            weapon.ReloadOnServer();
            Assert.That(weapon.IsReloading, Is.True);
            weapon.FireOnServer(Vector3.up);
            Assert.That(weapon.Ammo, Is.EqualTo(29), "Cannot fire while reloading");
            yield return new WaitForSeconds(2.6f);
            Assert.That(weapon.Ammo, Is.EqualTo(30));
            Assert.That(weapon.IsReloading, Is.False);

            var playerController = player.GetComponent<CharacterController>();
            playerController.height = 36 * motor.Movement.MetresPerUnit;
            playerController.center = Vector3.up * playerController.height / 2;
            Assert.That(player.Eyes.y - player.transform.position.y, Is.EqualTo(30 * motor.Movement.MetresPerUnit).Within(0.001f));
            playerController.height = 72 * motor.Movement.MetresPerUnit;
            playerController.center = Vector3.up * playerController.height / 2;

            Assert.That(target, Is.Not.Null);
            var targetController=target.GetComponent<CharacterController>(); targetController.enabled=false;
            target.transform.position=player.transform.position+Vector3.forward*1.5f; targetController.enabled=true;
            player.InfectOnServer(); target.ResetHumanOnServer();
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=player.transform.position+new Vector3(0,1,0.7f); wall.transform.localScale=new Vector3(1,2,0.1f);
            Physics.SyncTransforms(); weapon.FireOnServer(Vector3.forward);
            Assert.That(target.Faction, Is.EqualTo(Faction.Human), "Wall must block infection melee");
            Object.Destroy(wall); yield return new WaitForSeconds(0.8f);
            Physics.SyncTransforms();
            weapon.FireOnServer((targetController.bounds.center-player.Eyes).normalized);
            Assert.That(target.Faction, Is.EqualTo(Faction.Zombie), "Zombie contact must infect at melee range");
            Assert.That(target.Health, Is.EqualTo(target.Config.ZombieHealth), "Contact infection must not inherit the initial zombie bonus");

            // Server bots must autonomously reload; this was missing in the first implementation.
            target.ResetHumanOnServer(); var botWeapon=target.GetComponent<NetworkWeapon>();
            for(int i=0;i<30;i++) { botWeapon.FireOnServer(Vector3.up); yield return new WaitForSeconds(0.11f); }
            Assert.That(botWeapon.Ammo, Is.Zero);
            var presentation=target.GetComponent<ActorPresentation>();
            Assert.That(presentation.Animator.GetCurrentAnimatorStateInfo(1).IsName("Shoot"), Is.True,
                "A confirmed server shot must drive the upper-body shooting animation");
            target.GetComponent<ZombiePlague.Infrastructure.AI.DebugBot>().enabled=true;
            yield return null;
            Assert.That(botWeapon.IsReloading, Is.True);
            yield return new WaitForSeconds(0.25f);
            Assert.That(presentation.Animator.GetCurrentAnimatorStateInfo(1).IsName("Reload"), Is.True,
                "Server reload must drive the upper-body reload animation");
            Assert.That(presentation.Animator.GetFloat("Forward"), Is.LessThan(-0.1f),
                "A retreating bot must use backwards movement animation");
            yield return new WaitForSeconds(2.6f);
            Assert.That(botWeapon.Ammo, Is.GreaterThan(0));
        }
    }
}
#endif
