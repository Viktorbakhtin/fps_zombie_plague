using UnityEngine;
namespace ZombiePlague.Infrastructure.Configs
{
    [CreateAssetMenu(menuName = "Zombie Plague/Bot Config")]
    public sealed class BotConfig : ScriptableObject
    {
        [Min(1)] public float SightRange = 24;
        [Min(0.05f)] public float ThinkInterval = 0.2f;
        [Min(0)] public float ReactionTime = 0.3f;
        [Min(0)] public float MemoryDuration = 3;
        [Min(1)] public float HumanDistance = 6;
        [Min(0.1f)] public float HumanSpeed = 2.8f;
        [Min(0.1f)] public float ZombieSpeed = 3.4f;
        [Range(1, 8)] public int BurstShots = 3;
        [Min(0.1f)] public float BurstPause = 0.55f;
    }
}
