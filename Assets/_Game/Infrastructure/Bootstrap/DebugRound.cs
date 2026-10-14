using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;
using ZombiePlague.Domain.Actors;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Combat;

namespace ZombiePlague.Infrastructure.Bootstrap
{
    public sealed class DebugRound : MonoBehaviour
    {
        public PrototypeSession Session;
        public NetworkObject BotPrefab;
        public float InfectionDelay = 5f;
        public float InfectionGrace = 3f;
        public float RoundDuration = 60f;
        private readonly List<NetworkActor> participants = new List<NetworkActor>();
        private string status = "Starting local FishNet host...";
        private float deadline;
        private bool infected;
        private bool ending;
        private float combatStartsAt;
        public bool CombatActive { get; private set; }
        public int RoundNumber { get; private set; }
        public int ParticipantCount => participants.Count;
        public string Status => status;
        private IEnumerator Start()
        {
            while (!Session.Manager.ServerManager.Started || Session.Manager.ServerManager.Clients.Count == 0) yield return null;
            for (int i = 0; i < 2; i++)
            {
                var bot = Instantiate(BotPrefab, new Vector3(i == 0 ? -4 : 4, 0.1f, 4), Quaternion.identity);
                Session.Manager.ServerManager.Spawn(bot);
            }
            while (NetworkActor.ServerActors.Count < 3) yield return null;
            BeginRound();
        }
        public void BeginRound()
        {
            if (!Session.Manager.ServerManager.Started) return;
            participants.Clear(); participants.AddRange(NetworkActor.ServerActors);
            participants.Sort((a,b) => a.ObjectId.CompareTo(b.ObjectId));
            int playerSpawn = 0, botSpawn = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                var actor = participants[i]; actor.ResetHumanOnServer(); actor.SetCombatOnServer(false); actor.GetComponent<NetworkWeapon>().ResetOnServer();
                var controller = actor.GetComponent<CharacterController>(); controller.enabled = false;
                actor.transform.position = actor.Owner.IsValid ? new Vector3(playerSpawn++ * 4, 0.1f, -5) : new Vector3(botSpawn++ % 2 == 0 ? -4 : 4, 0.1f, 4);
                controller.enabled = true;
                var motor = actor.GetComponent<Player.ServerFpsMotor>(); if (motor != null) motor.ResetMotionOnServer();
                var bot = actor.GetComponent<AI.DebugBot>(); if (bot != null) bot.ResetMotionOnServer();
            }
            RoundNumber++; infected = ending = CombatActive = false; combatStartsAt = float.PositiveInfinity; deadline = Time.time + InfectionDelay;
            status = "Round " + RoundNumber + ": infection in " + InfectionDelay + " seconds";
        }
        private void Update()
        {
            if (!Session.Manager.ServerManager.Started || participants.Count == 0) return;
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame) BeginRound();
            if (ending) { if (Time.time >= deadline) BeginRound(); return; }
            if (!infected && Time.time >= deadline)
            {
                var alive = participants.FindAll(a => a != null && a.Alive);
                if (alive.Count == 0) { BeginRound(); return; }
                var chosen = alive[Random.Range(0, alive.Count)]; chosen.InfectOnServer(true); infected = true;
                status = "First zombie: " + (chosen.Owner.IsValid ? "YOU" : "BOT " + chosen.ObjectId);
                combatStartsAt = Time.time + InfectionGrace;
                deadline = combatStartsAt + RoundDuration;
                Debug.Log("[ZP Debug] " + status);
            }
            if (infected)
            {
                if (!CombatActive && Time.time >= combatStartsAt)
                {
                    CombatActive = true;
                    foreach (var actor in participants) if (actor != null) actor.SetCombatOnServer(true);
                }
                int humans = 0, zombies = 0;
                foreach (var actor in participants) if (actor != null && actor.Alive) { if (actor.Faction == Faction.Human) humans++; else zombies++; }
                if (humans == 0 || zombies == 0 || Time.time >= deadline)
                {
                    status = humans == 0 ? "Zombies win! New round in" : zombies == 0 ? "Humans win! New round in" : "Time expired. New round in";
                    ending = true; deadline = Time.time + 3;
                    CombatActive = false;
                    foreach (var actor in participants) if (actor != null) actor.SetCombatOnServer(false);
                }
            }
        }
        private void OnGUI()
        {
            GUI.Box(new Rect(15, 85, 550, 95), "DEBUG ARENA");
            GUI.Label(new Rect(30, 110, 530, 25), status + " | " + Mathf.CeilToInt(Mathf.Max(0, deadline - Time.time)) + "s");
            GUI.Label(new Rect(30, 135, 530, 25), "2 bots | F5: new round | Green: zombie | Blue: human");
            if (infected && !ending && !CombatActive) GUI.Label(new Rect(30, 158, 530, 22), "Prepare! Combat starts in " + Mathf.CeilToInt(Mathf.Max(0, combatStartsAt - Time.time)) + "s");
            if (GUI.Button(new Rect(580, 100, 150, 35), "New round (F5)")) BeginRound();
            int row = 0;
            foreach (var actor in participants)
            {
                if (actor == null) continue;
                var bot = actor.GetComponent<AI.DebugBot>();
                if (bot != null) GUI.Label(new Rect(30, 185 + row++ * 22, 530, 22), "BOT " + actor.ObjectId + " | " + actor.Faction + " | " + actor.Health + " HP | " + bot.State);
            }
        }
    }
}
