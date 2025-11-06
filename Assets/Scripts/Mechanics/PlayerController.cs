using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using UnityEngine.InputSystem;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private InputAction m_MoveAction;
        private InputAction m_JumpAction;

        public Bounds Bounds => collider2d.bounds;

        // === 펫 추적용 상태 히스토리 ===
        public List<PlayerState> stateHistory = new List<PlayerState>();
        [SerializeField] private float historyDuration = 0.5f;
        // =============================

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            if (model != null) model.player = this;

            m_MoveAction = InputSystem.actions.FindAction("Player/Move");
            m_JumpAction = InputSystem.actions.FindAction("Player/Jump");

            m_MoveAction?.Enable();
            m_JumpAction?.Enable();

            if (collider2d == null) Debug.LogWarning("[PlayerController] Collider2D is missing.");
            if (health == null) Debug.LogWarning("[PlayerController] Health is missing.");
            if (audioSource == null) Debug.LogWarning("[PlayerController] AudioSource is missing.");
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = m_MoveAction != null ? m_MoveAction.ReadValue<Vector2>().x : 0f;

                if (jumpState == JumpState.Grounded && m_JumpAction != null && m_JumpAction.WasPressedThisFrame())
                {
                    jumpState = JumpState.PrepareToJump;
                }
                else if (m_JumpAction != null && m_JumpAction.WasReleasedThisFrame())
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }

            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            if (animator != null)
            {
                animator.SetBool("grounded", IsGrounded);
                animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / Mathf.Max(0.0001f, maxSpeed));
            }

            targetVelocity = move * maxSpeed;
        }

        void LateUpdate()
        {
            var state = new PlayerState
            {
                Timestamp = Time.time,
                Position = transform.position,
                IsFlipped = spriteRenderer != null && spriteRenderer.flipX,
                VelocityX = Mathf.Abs(velocity.x) / Mathf.Max(0.0001f, maxSpeed),
                IsGrounded = IsGrounded
            };
            stateHistory.Add(state);

            float cutoff = Time.time - historyDuration;
            stateHistory.RemoveAll(s => s.Timestamp < cutoff);
        }


        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}
