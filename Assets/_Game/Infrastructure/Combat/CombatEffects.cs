using UnityEngine;

namespace ZombiePlague.Infrastructure.Combat
{
    // Cosmetic client pool. Hit positions come exclusively from the server raycast.
    public sealed class CombatEffects : MonoBehaviour
    {
        public GameObject ImpactPrefab;
        public static CombatEffects Instance { get; private set; }
        public Vector3 LastImpactPoint { get; private set; }
        public Vector3 LastImpactNormal { get; private set; }
        public int ActiveImpactCount
        {
            get { int count = 0; foreach (var slot in slots) if (slot != null && slot.Root.activeSelf) count++; return count; }
        }
        private sealed class Slot { public GameObject Root; public ParticleSystem[] Particles; public float Expires; }
        private readonly Slot[] slots = new Slot[32];
        private int next;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => Instance = null;
        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Impact(Vector3 point, Vector3 normal)
        {
            if (ImpactPrefab == null || UnityEngine.Application.isBatchMode || normal.sqrMagnitude < 0.5f) return;
            LastImpactPoint = point; LastImpactNormal = normal;
            var slot = slots[next];
            if (slot == null)
            {
                var root = Instantiate(ImpactPrefab, transform); root.name = "Pooled bullet impact " + next;
                slot = new Slot { Root = root, Particles = root.GetComponentsInChildren<ParticleSystem>(true) }; slots[next] = slot;
            }
            next = (next + 1) % slots.Length;
            slot.Root.SetActive(false);
            slot.Root.transform.SetPositionAndRotation(point + normal * 0.004f, Quaternion.LookRotation(normal));
            slot.Root.SetActive(true);
            foreach (var ps in slot.Particles) { ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); ps.Play(false); }
            slot.Expires = Time.time + 8;
        }
        private void Update()
        {
            foreach (var slot in slots) if (slot != null && slot.Root.activeSelf && Time.time >= slot.Expires) slot.Root.SetActive(false);
        }
    }
}
