using UnityEngine;

namespace ZombiePlague.Infrastructure.Actors
{
    public sealed class ActorPresentation : MonoBehaviour
    {
        public NetworkActor Actor;
        public GameObject ThirdPersonVisual;
        public Animator Animator;
        public Transform ViewWeapon;
        public AudioClip ShotAudio;
        public AudioClip ReloadAudio;
        public GameObject MuzzleFlashPrefab;
        public Transform Muzzle;
        private ParticleSystem muzzleFlash;
        private AudioSource audioSource;
        private Vector3 gunRest;
        private float gunKick;
        private float displayedKick, kickVelocity;
        private Renderer[] rifles;
        private static readonly int Speed = UnityEngine.Animator.StringToHash("Speed");
        private static readonly int Forward = UnityEngine.Animator.StringToHash("Forward");
        private static readonly int Shoot = UnityEngine.Animator.StringToHash("Shoot");
        private static readonly int ReloadTrigger = UnityEngine.Animator.StringToHash("Reload");
        private Vector3 previous;
        private void Start()
        {
            previous = transform.position; audioSource = GetComponent<AudioSource>(); if(ViewWeapon != null) gunRest = ViewWeapon.localPosition;
            var matches=new System.Collections.Generic.List<Renderer>();
            foreach(var renderer in ThirdPersonVisual.GetComponentsInChildren<Renderer>(true))
                if(renderer.name.StartsWith("geo_assaultRifle")) matches.Add(renderer);
            rifles=matches.ToArray();
        }
        public void Shot()
        {
            if (Animator != null && Animator.isActiveAndEnabled) Animator.SetTrigger(Shoot);
            gunKick = 0.018f;
            if (!UnityEngine.Application.isBatchMode && Muzzle != null && MuzzleFlashPrefab != null)
            {
                if (muzzleFlash == null) muzzleFlash = Instantiate(MuzzleFlashPrefab, Muzzle, false).GetComponent<ParticleSystem>();
                if (muzzleFlash != null) { muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); muzzleFlash.Play(true); }
            }
            if (audioSource != null && ShotAudio != null) audioSource.PlayOneShot(ShotAudio,0.25f);
        }
        public void Reload()
        {
            if (Animator != null && Animator.isActiveAndEnabled) Animator.SetTrigger(ReloadTrigger);
            if (audioSource != null && ReloadAudio != null) audioSource.PlayOneShot(ReloadAudio,0.25f);
        }
        private void LateUpdate()
        {
            bool owner = Actor.IsOwner;
            bool armed = Actor.Alive && Actor.Faction == ZombiePlague.Domain.Actors.Faction.Human;
            if(rifles!=null) foreach(var rifle in rifles) if(rifle.enabled!=armed) rifle.enabled=armed;
            if(ViewWeapon!=null && ViewWeapon.gameObject.activeSelf!=armed) ViewWeapon.gameObject.SetActive(armed);
            if (ThirdPersonVisual.activeSelf == owner) ThirdPersonVisual.SetActive(!owner);
            Vector3 offset = transform.position - previous; offset.y = 0;
            Vector3 movement = offset.sqrMagnitude > 4 ? Vector3.zero : offset / Mathf.Max(Time.deltaTime,0.001f);
            float speed = Actor.Alive ? movement.magnitude : 0;
            previous = transform.position;
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetFloat(Speed,Mathf.Min(speed,10),0.12f,Time.deltaTime);
                Animator.SetFloat(Forward,Actor.Alive ? Vector3.Dot(movement,transform.forward)/3f : 0,0.12f,Time.deltaTime);
            }
            if (ViewWeapon != null)
            {
                displayedKick = Mathf.SmoothDamp(displayedKick, gunKick, ref kickVelocity, 0.035f);
                gunKick = Mathf.MoveTowards(gunKick, 0, Time.deltaTime * 0.18f);
                ViewWeapon.localPosition = gunRest - Vector3.forward * displayedKick;
            }
        }
    }
}
