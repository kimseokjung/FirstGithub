using System.Linq;               // LastOrDefault 사용
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// 플레이어의 상태 기록을 읽어 특정 지연시간만큼 지난 “과거의 상태”를 그대로 따라가는 펫.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PetController : MonoBehaviour
    {
        [Tooltip("따라갈 플레이어 오브젝트를 지정합니다.")]
        public PlayerController player;

        [Tooltip("플레이어를 따라갈 때의 지연 시간(초). 기본 0.1s")]
        public float delay = 0.1f;

        private SpriteRenderer spriteRenderer;
        private Animator animator;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            if (player == null)
            {
                Debug.LogError("[PetController] Player 참조가 비어 있습니다. 인스펙터에서 할당해주세요.");
                enabled = false;
            }
        }

        void LateUpdate()
        {
            if (!enabled || player == null) return;
            if (player.stateHistory == null || player.stateHistory.Count == 0) return;

            float targetTime = Time.time - delay;

            // targetTime 이하 중 가장 최근(가장 가까운 과거)의 상태
            PlayerState past = player.stateHistory.LastOrDefault(s => s.Timestamp <= targetTime);

            // 유효한 과거 상태
            if (past.Timestamp > 0f)
            {
                ApplyState(past);
            }
            else
            {
                // 히스토리가 아직 짧을 때: 가장 오래된 상태 사용
                ApplyState(player.stateHistory[0]);
            }
        }

        private void ApplyState(PlayerState s)
        {
            transform.position = s.Position;

            if (spriteRenderer != null)
                spriteRenderer.flipX = s.IsFlipped;

            if (animator != null)
            {
                animator.SetFloat("velocityX", s.VelocityX);
                animator.SetBool("grounded", s.IsGrounded);
            }
        }
    }
}
