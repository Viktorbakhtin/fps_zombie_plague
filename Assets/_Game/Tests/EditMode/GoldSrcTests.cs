using NUnit.Framework;
using ZombiePlague.Domain.Movement;
using ZombiePlague.Domain.Combat;

namespace ZombiePlague.Tests
{
    public sealed class GoldSrcTests
    {
        [Test] public void FrictionUsesStopSpeedBelowThreshold()
        { Assert.That(GoldSrcMath.FrictionScale(50, 75, 4, 0.01f), Is.EqualTo(0.94f).Within(0.00001f)); }
        [Test] public void AirAccelerationCapsProjectionNotTotalSpeed()
        {
            Assert.That(GoldSrcMath.Acceleration(0, 221, 10, 0.01f, 1, true), Is.EqualTo(22.1f).Within(0.001f));
            Assert.That(GoldSrcMath.Acceleration(25, 221, 10, 0.01f, 1, true), Is.EqualTo(5));
            Assert.That(GoldSrcMath.Acceleration(31, 221, 10, 0.01f, 1, true), Is.Zero);
        }
        [Test] public void StandardJumpAndBunnyhopLimit()
        {
            Assert.That(GoldSrcMath.JumpVelocity(0), Is.EqualTo(268.32816f).Within(0.001f));
            Assert.That(GoldSrcMath.BunnyhopScale(300, 221), Is.EqualTo(0.7072f).Within(0.0001f));
        }
        [Test] public void Ak47UsesPreviousAccuracyAndCubicGrowth()
        {
            var weapon = new Ak47Accuracy();
            Assert.That(weapon.Spread(true, 0), Is.EqualTo(0.0055f).Within(0.00001f));
            weapon.Fired();
            Assert.That(weapon.Accuracy, Is.EqualTo(0.355f).Within(0.00001f));
            Assert.That(weapon.Spread(false, 0), Is.EqualTo(0.182f).Within(0.00001f));
            for (int i = 0; i < 5; i++) weapon.Fired();
            Assert.That(weapon.Accuracy, Is.EqualTo(1.25f));
        }
        [Test] public void SustainedBurstFullyRecoversAfterPause()
        {
            var weapon = new Ak47Accuracy();
            for(int i = 0; i < 30; i++) weapon.Fired();
            weapon.Release(1f);
            Assert.That(weapon.Accuracy, Is.EqualTo(1.25f));
            weapon.Release(2f);
            Assert.That(weapon.Shots, Is.Zero);
            Assert.That(weapon.Spread(true, 0), Is.EqualTo(0.0055f).Within(0.00001f));
            weapon.Fired(); Assert.That(weapon.Shots, Is.EqualTo(1));
        }
        [Test] public void RecoveryIsIndependentOfUpdateFrequency()
        {
            var fast = new Ak47Accuracy(); var slow = new Ak47Accuracy();
            for(int i = 0; i < 15; i++) { fast.Fired(); slow.Fired(); }
            fast.Release(0); slow.Release(0);
            for(int i = 1; i <= 100; i++) fast.Release(i / 100f);
            slow.Release(1);
            Assert.That(fast.Shots, Is.EqualTo(slow.Shots));
            Assert.That(fast.Accuracy, Is.EqualTo(slow.Accuracy));
        }
    }
}
