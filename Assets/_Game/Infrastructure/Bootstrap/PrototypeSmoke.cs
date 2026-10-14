using System;
using UnityEngine;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Player;
using ZombiePlague.Infrastructure.Combat;

namespace ZombiePlague.Infrastructure.Bootstrap
{
    public static class PrototypeSmoke
    {
        public static bool Enabled { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Enabled = Array.IndexOf(Environment.GetCommandLineArgs(), "-zpSmoke") >= 0;
#else
            Enabled = false;
#endif
        }
    }
}
