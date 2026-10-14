using FishNet.Object;
using FishNet.Transporting;
using FishNet.Object.Prediction;
using UnityEngine;
using UnityEngine.InputSystem;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Configs;
using ZombiePlague.Domain.Movement;
using ZombiePlague.Infrastructure.Bootstrap;

namespace ZombiePlague.Infrastructure.Player
{
    [RequireComponent(typeof(CharacterController), typeof(NetworkActor))]
    public sealed class ServerFpsMotor : NetworkBehaviour
    {
        public Camera View;
        public Transform ViewPivot;
        public Renderer Body;
        public GoldSrcMovementConfig Movement;
        private CharacterController controller;
        private NetworkActor actor;
        private ZombiePlague.Infrastructure.Combat.NetworkWeapon weapon;
        private Vector2 input;
        private float yaw, pitch, vertical;
        private Vector2 visualRecoil, recoilVelocity;
        private bool grounded;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];
        private Vector3 velocity;
        private bool jump, duck, walk, jumpHeld;
        private float stamina;
        private float started;
        private readonly Collider[] standOverlaps = new Collider[8];
        public bool Grounded => grounded;
        public bool Ducking => duck;
        public float SpeedUnits => new Vector2(velocity.x, velocity.z).magnitude / Movement.MetresPerUnit;
        public bool FireHeld { get; private set; }
        public bool AcceptLocalInput { get; set; } = true;
        public Vector3 AimDirection => Quaternion.Euler(pitch, yaw, 0) * Vector3.forward;

        public struct MoveData : IReplicateData
        {
            public Vector2 Input; public float Yaw; public bool Jump, Duck, Walk, Fire;
            private uint tick;
            public uint GetTick() => tick; public void SetTick(uint value) => tick = value; public void Dispose() { }
        }
        public struct StateData : IReconcileData
        {
            public Vector3 Position, Velocity, Knockback; public float Yaw, Stamina; public bool JumpHeld, Duck, Grounded;
            private uint tick;
            public uint GetTick() => tick; public void SetTick(uint value) => tick = value; public void Dispose() { }
        }

        private void Awake() { controller = GetComponent<CharacterController>(); controller.minMoveDistance = 0; actor = GetComponent<NetworkActor>(); weapon = GetComponent<ZombiePlague.Infrastructure.Combat.NetworkWeapon>(); }
        public override void OnStartClient()
        {
            base.OnStartClient();
            View.enabled = IsOwner;
            View.GetComponent<AudioListener>().enabled = IsOwner;
            if (IsOwner) { Body.enabled = false; yaw = transform.eulerAngles.y; }
            started = Time.time;
        }
        public override void OnStartNetwork() { base.OnStartNetwork(); TimeManager.OnTick += Tick; TimeManager.OnPostTick += CreateReconcile; }
        public override void OnStopNetwork() { TimeManager.OnTick -= Tick; TimeManager.OnPostTick -= CreateReconcile; base.OnStopNetwork(); }

        private void Update()
        {
            if (!IsOwner || !IsClientInitialized) return;
            if (!AcceptLocalInput) { input = Vector2.zero; jump = duck = walk = false; return; }
            if (PrototypeSmoke.Enabled)
            {
                input = Time.time - started < 1f ? Vector2.up : Vector2.zero;
                return;
            }
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (mouse.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
            if (keyboard.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
            if (Cursor.lockState != CursorLockMode.Locked) { input = Vector2.zero; jump = duck = walk = false; return; }
            Vector2 delta = mouse.delta.ReadValue() * 0.12f;
            yaw += delta.x; pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            input = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            jump = keyboard.spaceKey.isPressed; duck = keyboard.leftCtrlKey.isPressed; walk = keyboard.leftShiftKey.isPressed;
        }

        private void LateUpdate()
        {
            if (!IsOwner || !IsClientInitialized) return;
            Vector2 punch = weapon != null ? weapon.Punch : Vector2.zero;
            // Smooth only the recoil offset, keeping mouse aim immediate.
            visualRecoil = Vector2.SmoothDamp(visualRecoil, punch, ref recoilVelocity, 0.09f, 30f, Time.deltaTime);
            // Apply after all predicted/replayed root rotations, including idle recoil recovery.
            ViewPivot.rotation = Quaternion.Euler(pitch + visualRecoil.x, yaw + visualRecoil.y, 0);
        }

        private void Tick()
        {
            Simulate(IsOwner ? new MoveData { Input = input, Yaw = yaw, Jump = jump, Duck = duck, Walk = walk,
                Fire = AcceptLocalInput && Cursor.lockState == CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.isPressed } : default);
        }

        [Replicate]
        private void Simulate(MoveData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if ((!IsServerInitialized && !IsOwner) || !actor.Alive || !Finite(data.Input.x) || !Finite(data.Input.y) || !Finite(data.Yaw)) return;
            float dt = (float)TimeManager.TickDelta;
            FireHeld = data.Fire;
            float unit = Movement.MetresPerUnit;
            transform.rotation = Quaternion.Euler(0, data.Yaw, 0);
            bool onGround = Grounded;
            stamina = Mathf.Max(0, stamina - dt * 1000);
            bool wantsDuck = data.Duck;
            if (duck && !wantsDuck)
            {
                float radius = controller.radius - controller.skinWidth;
                int count = Physics.OverlapCapsuleNonAlloc(transform.position + Vector3.up * (radius + 0.01f),
                    transform.position + Vector3.up * (72 * unit - radius), radius, standOverlaps, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++) if (standOverlaps[i] != controller) { wantsDuck = true; break; }
            }
            duck = wantsDuck;
            float height = (duck ? 36 : 72) * unit;
            if (!Mathf.Approximately(controller.height, height)) { controller.height = height; controller.center = Vector3.up * height * 0.5f; }
            ViewPivot.localPosition = Vector3.up * (duck ? 30 : 64) * unit;
            float maximum = Movement.MaxSpeed * (duck ? 0.333f : data.Walk ? 0.52f : 1f);
            vertical = velocity.y - Movement.Gravity * unit * dt * 0.5f;
            if (onGround && data.Jump && !jumpHeld)
            {
                velocity *= GoldSrcMath.BunnyhopScale(velocity.magnitude / unit, maximum);
                vertical = GoldSrcMath.JumpVelocity(stamina) * unit - Movement.Gravity * unit * dt * 0.5f;
                stamina = 1315.789429f; onGround = false;
            }
            jumpHeld = data.Jump;
            var horizontal = new Vector3(velocity.x, 0, velocity.z);
            if (onGround)
            {
                float edge = 1f;
                if (horizontal.sqrMagnitude > 0.001f && !Physics.Raycast(transform.position + Vector3.up * 0.01f + horizontal.normalized * 16 * unit,
                    Vector3.down, 34 * unit, ~0, QueryTriggerInteraction.Ignore)) edge = Movement.EdgeFriction;
                horizontal *= GoldSrcMath.FrictionScale(horizontal.magnitude / unit, Movement.StopSpeed, Movement.Friction * edge, dt);
                horizontal *= GoldSrcMath.StaminaScale(stamina);
                vertical = -0.02f;
            }
            Vector2 axes = Vector2.ClampMagnitude(data.Input, 1f);
            Vector3 wish = (transform.forward * axes.y + transform.right * axes.x);
            float wishSpeed = wish.magnitude * maximum;
            if (wishSpeed > 0)
            {
                Vector3 direction = wish.normalized;
                float add = GoldSrcMath.Acceleration(Vector3.Dot(horizontal, direction) / unit, wishSpeed,
                    onGround ? Movement.GroundAcceleration : Movement.AirAcceleration, dt, 1f, !onGround);
                horizontal += direction * add * unit;
            }
            velocity = horizontal + Vector3.up * vertical;
            var flags = controller.Move((velocity + actor.Knockback.Step(dt, actor.Config.KnockbackDrag)) * dt);
            grounded = (flags & CollisionFlags.Below) != 0 || velocity.y <= 0 && HasGroundSupport();
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0) velocity.y = 0;
            velocity.y -= Movement.Gravity * unit * dt * 0.5f;
            if (Grounded && velocity.y < 0) velocity.y = 0;
        }

        public override void CreateReconcile() => Reconcile(new StateData { Position = transform.position,
            Velocity = velocity, Knockback = actor.Knockback.Velocity, Yaw = transform.eulerAngles.y, Stamina = stamina, JumpHeld = jumpHeld, Duck = duck, Grounded = grounded });
        [Reconcile]
        private void Reconcile(StateData data, Channel channel = Channel.Unreliable)
        {
            controller.enabled = false; transform.position = data.Position; transform.rotation = Quaternion.Euler(0, data.Yaw, 0);
            controller.enabled = true; velocity = data.Velocity; stamina = data.Stamina; jumpHeld = data.JumpHeld; duck = data.Duck; grounded = data.Grounded;
            actor.Knockback.Restore(data.Knockback);
        }
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public void ResetMotionOnServer()
        {
            if (!IsServerInitialized) return;
            velocity = Vector3.zero; vertical = stamina = 0; jumpHeld = false;
            grounded = HasGroundSupport(); visualRecoil = recoilVelocity = Vector2.zero;
            actor.Knockback.Clear();
        }
        private bool HasGroundSupport()
        {
            int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * 0.05f, Vector3.down,
                groundHits, 0.08f, ~0, QueryTriggerInteraction.Ignore);
            float slope = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
                if (groundHits[i].collider != controller && groundHits[i].normal.y >= slope &&
                    groundHits[i].collider.GetComponentInParent<NetworkActor>() != actor) return true;
            return false;
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            float inward = Vector3.Dot(velocity, hit.normal);
            if (inward < 0) velocity -= hit.normal * inward;
        }
    }
}
