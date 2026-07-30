using UnityEngine;

namespace WitchChronicle.Fishing
{
    /// <summary>
    /// 낚시 애니메이션 훅
    /// FishingManager의 상태 변화를 받아 Animator 트리거를 호출한다.
    /// 
    /// 사용법 (애니 담당자):
    /// 1. 플레이어 캐릭터에 이 컴포넌트를 붙인다
    /// 2. Animator 필드에 낚시 애니 Animator를 연결한다
    /// 3. 각 메서드 안의 주석을 참고해 SetTrigger/SetBool 호출을 추가한다
    /// 
    /// FishingManager는 자동으로 이 컴포넌트를 찾아 이벤트를 전달한다.
    /// </summary>
    public class FishingAnimatorHook : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Animator Parameter Names (담당자가 지정)")]
        [Tooltip("낚시 모드 진입/이탈 Bool 파라미터")]
        [SerializeField] private string isFishingBool = "IsFishing";
        [Tooltip("줄 던지기 Trigger")]
        [SerializeField] private string castTrigger = "Cast";
        [Tooltip("입질 반응 Trigger")]
        [SerializeField] private string biteTrigger = "Bite";
        [Tooltip("줄 감기 시작 Trigger")]
        [SerializeField] private string reelTrigger = "Reel";
        [Tooltip("성공 - 물고기 잡음 Trigger")]
        [SerializeField] private string catchSuccessTrigger = "CatchSuccess";
        [Tooltip("실패 - 놓침 Trigger")]
        [SerializeField] private string catchFailTrigger = "CatchFail";

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        // ─────────────────────────────────────────
        // 세션 진입/이탈
        // ─────────────────────────────────────────

        /// <summary>
        /// 낚시 시작 - SitPoint에 앉을 때 호출
        /// 담당자: 앉기 애니메이션 or 낚싯대 든 대기 자세
        /// </summary>
        public void OnEnterFishing()
        {
            Debug.Log("[FishingAnimatorHook] OnEnterFishing");
            if (animator != null && !string.IsNullOrEmpty(isFishingBool))
                animator.SetBool(isFishingBool, true);
        }

        /// <summary>
        /// 낚시 종료 - 나가기 눌러 원위치 복귀할 때 호출
        /// 담당자: 일어서기 애니메이션 or IsFishing Bool false
        /// </summary>
        public void OnExitFishing()
        {
            Debug.Log("[FishingAnimatorHook] OnExitFishing");
            if (animator != null && !string.IsNullOrEmpty(isFishingBool))
                animator.SetBool(isFishingBool, false);
        }

        // ─────────────────────────────────────────
        // 낚시 사이클 상태별 트리거
        // ─────────────────────────────────────────

        /// <summary>
        /// 줄 풀기 버튼 → Casting 진입 시 호출
        /// 담당자: 낚싯대 던지는 모션 (팔 스윙)
        /// </summary>
        public void OnCastStart()
        {
            Debug.Log("[FishingAnimatorHook] OnCastStart");
            if (animator != null && !string.IsNullOrEmpty(castTrigger))
                animator.SetTrigger(castTrigger);
        }

        /// <summary>
        /// 물고기 입질 - Bite 진입 시 호출
        /// 담당자: 놀라는 반응 or 낚싯대 흔들림
        /// </summary>
        public void OnBite()
        {
            Debug.Log("[FishingAnimatorHook] OnBite");
            if (animator != null && !string.IsNullOrEmpty(biteTrigger))
                animator.SetTrigger(biteTrigger);
        }

        /// <summary>
        /// 줄 감기 시작 - Reeling 진입 시 호출
        /// 담당자: 릴 감는 반복 모션
        /// </summary>
        public void OnReelStart()
        {
            Debug.Log("[FishingAnimatorHook] OnReelStart");
            if (animator != null && !string.IsNullOrEmpty(reelTrigger))
                animator.SetTrigger(reelTrigger);
        }

        /// <summary>
        /// 물고기 잡음 - 성공 시 호출
        /// 담당자: 낚싯대 들어올리며 환호 or 물고기 확인 모션
        /// </summary>
        public void OnCatchSuccess()
        {
            Debug.Log("[FishingAnimatorHook] OnCatchSuccess");
            if (animator != null && !string.IsNullOrEmpty(catchSuccessTrigger))
                animator.SetTrigger(catchSuccessTrigger);
        }

        /// <summary>
        /// 물고기 놓침 - 실패 시 호출
        /// 담당자: 아쉬워하는 모션 or 낚싯대 축 처짐
        /// </summary>
        public void OnCatchFail()
        {
            Debug.Log("[FishingAnimatorHook] OnCatchFail");
            if (animator != null && !string.IsNullOrEmpty(catchFailTrigger))
                animator.SetTrigger(catchFailTrigger);
        }
    }
}