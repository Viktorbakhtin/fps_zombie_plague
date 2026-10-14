using UnityEngine;

namespace ZombiePlague.Infrastructure.Configs
{
    [CreateAssetMenu(menuName = "Zombie Plague/GoldSrc Movement")]
    public sealed class GoldSrcMovementConfig : ScriptableObject
    {
        [Min(0.001f)] public float MetresPerUnit = 0.0254f;
        [Min(1)] public float MaxSpeed = 221f; // AK-47 equipped.
        [Min(0)] public float GroundAcceleration = 5f;
        [Min(0)] public float AirAcceleration = 10f;
        [Min(0)] public float Friction = 4f;
        [Min(0)] public float StopSpeed = 75f;
        [Min(0)] public float Gravity = 800f;
        [Min(0)] public float EdgeFriction = 2f;
        [Min(0)] public float StepSize = 18f;
    }
}
