using System;

namespace ZombiePlague.Domain.Combat
{
    // AK accuracy with floating-point cubic growth and timed recovery.
    public sealed class Ak47Accuracy
    {
        public int Shots { get; private set; }
        public float Accuracy { get; private set; } = 0.2f;
        private float decayAt;
        private bool delayed;
        public float Spread(bool grounded, float speed) => !grounded ? 0.04f + 0.4f * Accuracy
            : speed > 140f ? 0.04f + 0.07f * Accuracy : 0.0275f * Accuracy;
        public void Fired()
        {
            Shots = Math.Min(Shots + 1, 100);
            Accuracy = Math.Min(1.25f, (Shots * Shots * Shots / 200f) + 0.35f);
            delayed = true;
        }
        public void Release(float now)
        {
            if (delayed) { delayed = false; Shots = Math.Min(Shots, 15); decayAt = now + 0.4f; }
            if (Shots > 0 && now >= decayAt)
            {
                int recovered = Math.Min(Shots, 1 + (int)((now - decayAt) / 0.0225f));
                Shots -= recovered;
                decayAt += recovered * 0.0225f;
                Accuracy = Shots == 0 ? 0.2f : Math.Min(1.25f, Shots * Shots * Shots / 200f + 0.35f);
            }
        }
        public void Reset() { Shots = 0; Accuracy = 0.2f; delayed = false; }
    }
}
