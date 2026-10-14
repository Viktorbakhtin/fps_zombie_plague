using System;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using ZombiePlague.Infrastructure.Configs;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Combat;

namespace ZombiePlague.Infrastructure.Bootstrap
{
    public sealed class PrototypeSession : MonoBehaviour
    {
        public NetworkManager Manager;
        public NetworkObject ZombiePrefab;
        public PrototypeConfig Config;
        public bool AutoHost;
        public bool SpawnInitialZombie = true;
        public ushort Port = 7777;
        private string address = "127.0.0.1";
        private NetworkObject zombie;
        private Camera overview;
        private void Awake()
        {
            UnityEngine.Application.runInBackground = true;
            var overviewObject = GameObject.Find("Overview Camera");
            if (overviewObject != null) overview = overviewObject.GetComponent<Camera>();
            Manager.TransportManager.Transport.SetPort(Port);
            Manager.TransportManager.Transport.SetMaximumClients(Config.MaxPlayers);
            Manager.ServerManager.OnServerConnectionState += ServerState;
        }
        private void Start()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-zpServer") >= 0) Manager.ServerManager.StartConnection();
            else if (Array.IndexOf(args, "-zpHost") >= 0) StartHost();
            else if (Array.IndexOf(args, "-zpClient") >= 0) Manager.ClientManager.StartConnection(address);
            else if (AutoHost) StartHost();
        }
        private void OnDestroy() { if (Manager != null) Manager.ServerManager.OnServerConnectionState -= ServerState; }
        private void ServerState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started) return;
            if (!SpawnInitialZombie) return;
            zombie = Instantiate(ZombiePrefab, new Vector3(0, 1, 8), Quaternion.identity);
            Manager.ServerManager.Spawn(zombie);
        }
        private void StartHost() { Manager.ServerManager.StartConnection(); Manager.ClientManager.StartConnection(address); }
        private void LateUpdate()
        {
            if (overview != null) overview.enabled = NetworkActor.LocalPlayer == null;
        }
        private void OnGUI()
        {
            if (!Manager.ClientManager.Started && !Manager.ServerManager.Started)
            {
                GUILayout.BeginArea(new Rect(20, 20, 300, 210), GUI.skin.box);
                GUILayout.Label("ZOMBIE PLAGUE / FISHNET PROTOTYPE");
                address = GUILayout.TextField(address);
                if (GUILayout.Button("Host")) StartHost();
                if (GUILayout.Button("Join")) Manager.ClientManager.StartConnection(address);
                if (GUILayout.Button("Dedicated server")) Manager.ServerManager.StartConnection();
                GUILayout.Label("WASD / Mouse / LMB / R reload / Esc cursor");
                GUILayout.EndArea();
            }
            else
            {
                GUI.Label(new Rect(20, 20, 550, 30), "FishNet | Server: " + Manager.ServerManager.Started +
                    " | Connections: " + Manager.ServerManager.Clients.Count);
                var actor = NetworkActor.LocalPlayer;
                    if (actor != null)
                    {
                        GUI.Label(new Rect(20, 50, 500, 30), actor.Faction + " | HP " + actor.Health +
                            (actor.Faction == ZombiePlague.Domain.Actors.Faction.Human ? " | Ammo " + actor.GetComponent<NetworkWeapon>().Ammo : " | LMB: infect"));
                        GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
                    }
            }
        }
    }
}
