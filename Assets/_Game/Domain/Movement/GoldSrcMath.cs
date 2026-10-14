using System;

namespace ZombiePlague.Domain.Movement
{
    // All arguments are in GoldSrc units, not Unity metres.
    public static class GoldSrcMath
    {
        public static float FrictionScale(float speed, float stopSpeed, float friction, float dt)
        {
            if (speed < 0.1f) return 1f;
            return Math.Max(0f, speed - Math.Max(speed, stopSpeed) * friction * dt) / speed;
        }
        public static float Acceleration(float projectedSpeed, float wishSpeed, float acceleration,
            float dt, float surfaceFriction, bool airborne)
        {
            float limit = airborne ? Math.Min(wishSpeed, 30f) : wishSpeed;
            return Math.Max(0, Math.Min(limit - projectedSpeed, acceleration * wishSpeed * dt * surfaceFriction));
        }
        public static float JumpVelocity(float staminaMilliseconds) =>
            (float)Math.Sqrt(2 * 800 * 45) * StaminaScale(staminaMilliseconds);
        public static float StaminaScale(float milliseconds) => 1f - milliseconds * 0.00019f;
        public static float BunnyhopScale(float speed, float maxSpeed) =>
            speed > maxSpeed * 1.2f ? maxSpeed * 1.2f / speed * 0.8f : 1f;
    }
}
