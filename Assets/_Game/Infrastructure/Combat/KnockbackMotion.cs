using UnityEngine;

namespace ZombiePlague.Infrastructure.Combat
{
    // Shared transient motion for predicted players and server-driven bots.
    public sealed class KnockbackMotion
    {
        public Vector3 Velocity { get; private set; }
        public void Add(Vector3 impulse, float maximumSpeed)
        {
            impulse.y = 0;
            Velocity = Vector3.ClampMagnitude(Velocity + impulse, maximumSpeed);
        }
        public Vector3 Step(float dt, float drag)
        {
            if (dt <= 0) return Vector3.zero;
            float decay = Mathf.Exp(-drag * dt);
            Vector3 average = Velocity * (drag > 0 ? (1 - decay) / (drag * dt) : 1);
            Velocity *= decay;
            if (Velocity.sqrMagnitude < 0.0001f) Velocity = Vector3.zero;
            return average;
        }
        public void Restore(Vector3 value) { Velocity = value; }
        public void Clear() { Velocity = Vector3.zero; }
    }
}
