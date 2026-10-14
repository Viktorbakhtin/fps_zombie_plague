using UnityEngine;

namespace ZombiePlague.Infrastructure.Configs
{
    public enum PrototypeMode { ClassicInfection, Outbreak }

    [CreateAssetMenu(menuName = "Zombie Plague/Prototype Config")]
    public sealed class PrototypeConfig : ScriptableObject
    {
        [SerializeField] private PrototypeMode mode;
        [SerializeField, Range(2, 64)] private int maxPlayers = 2;
        [SerializeField, Min(1)] private int humanHealth = 100;
        [SerializeField, Min(1)] private int zombieHealth = 250;
        [SerializeField, Min(1)] private int firstZombieHealth = 3000;
        [SerializeField, Min(0)] private float bulletKnockbackPerDamage = 0.16f;
        [SerializeField, Min(0)] private float knockbackRange = 12.7f;
        [SerializeField, Min(0)] private float knockbackMaximumSpeed = 12f;
        [SerializeField, Min(0)] private float knockbackDrag = 4f;
        [SerializeField, Range(0, 1)] private float crouchedKnockback = 0.25f;
        [SerializeField, Min(1)] private int weaponDamage = 25;

        public PrototypeMode Mode => mode;
        public int MaxPlayers => maxPlayers;
        public int HumanHealth => humanHealth;
        public int ZombieHealth => zombieHealth;
        public int FirstZombieHealth => Mathf.Max(zombieHealth, firstZombieHealth);
        public float BulletKnockbackPerDamage => bulletKnockbackPerDamage;
        public float KnockbackRange => knockbackRange;
        public float KnockbackMaximumSpeed => knockbackMaximumSpeed;
        public float KnockbackDrag => knockbackDrag;
        public float CrouchedKnockback => crouchedKnockback;
        public int WeaponDamage => weaponDamage;
        public float MoveSpeed => 5f;
        public float ZombieSpeed => 2f;
        public float FireInterval => 0.0955f;
        public float WeaponRange => 8192 * 0.0254f;
        public int MagazineSize => 30;
    }
}
