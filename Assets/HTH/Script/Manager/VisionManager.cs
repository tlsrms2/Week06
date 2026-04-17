using System;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 시야(Vision) 수치와 배팅을 모두 관리합니다.
    /// GameManager(블랙잭 로직)와 역할을 분리하기 위해
    /// 배팅 확정/승패 처리까지 이 클래스가 담당합니다.
    /// GameManager는 결과(승/패)만 전달하면 됩니다.
    /// </summary>
    public class VisionManager : MonoBehaviour
    {
        public static VisionManager Instance;
        // ─── Inspector ───────────────────────────────────────────
        [Header("시야 설정")]
        [Tooltip("시야 최대치")]
        [SerializeField] private int _maxVision = 100;

        // ─── 프로퍼티 ─────────────────────────────────────────────
        public int MaxVision => _maxVision;
        public int CurrentVision { get; private set; }

        /// <summary>현재 시야 비율 (0.0 ~ 1.0). VisionEffect가 구독합니다.</summary>
        public float VisionRatio => (float)CurrentVision / _maxVision;

        /// <summary>현재 배팅 중인 시야량</summary>
        public int CurrentBet { get; private set; }

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>시야 수치 변경 시 발행 (currentVision, maxVision)</summary>
        public event Action<int, int> OnVisionChanged;

        /// <summary>시야가 0이 되어 게임 오버가 되어야 할 때 발행</summary>
        public event Action OnVisionDepleted;

        // ─── 생명주기 ─────────────────────────────────────────────
        private void Awake()
        {
            // 중복 인스턴스 방어
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            CurrentVision = _maxVision;
            CurrentBet = 0;
        }
        private void OnEnable()
        {
            if (VisionManager.Instance != null)
                VisionManager.Instance.OnVisionChanged += HandleVisionChanged;
        }

        private void OnDisable()
        {
            if (VisionManager.Instance != null)
                VisionManager.Instance.OnVisionChanged -= HandleVisionChanged;
        }

        /// <summary>VisionManager.OnVisionChanged 구독 핸들러.</summary>
        private void HandleVisionChanged(int current, int max)
        {
            // TODO: UpdateVisionBar(current, max);
            // TODO: UpdateVignetteEffect((float)current / max);
            Debug.Log($"[GameUI] Vision — {current}/{max}");
        }

        // ─── 배팅 API ─────────────────────────────────────────────
        /// <summary>
        /// 배팅할 시야량을 설정합니다.
        /// 최소 1, 최대 현재 보유 시야로 자동 클램핑됩니다.
        /// </summary>
        public void SetBet(int amount)
        {
            CurrentBet = Mathf.Clamp(amount, 1, CurrentVision);
        }

        /// <summary>
        /// 배팅 가능 여부를 확인합니다.
        /// 스테이지 최소 배팅량 이상의 시야를 보유하고 있어야 합니다.
        /// </summary>
        public bool CanBet(int minimumBet) => CurrentVision >= minimumBet;

        /// <summary>
        /// 승리 처리: 배팅한 시야를 유지합니다.
        /// 시야는 차감되지 않으며 배팅량만 초기화됩니다.
        /// </summary>
        public void WinBet()
        {
            CurrentBet = 0;
            OnVisionChanged?.Invoke(CurrentVision, _maxVision);
        }

        /// <summary>
        /// 패배 처리: 배팅한 시야를 영구 차감합니다.
        /// 시야가 0 이하가 되면 OnVisionDepleted를 발행합니다.
        /// </summary>
        public void LoseBet()
        {
            CurrentVision = Mathf.Max(0, CurrentVision - CurrentBet);
            CurrentBet = 0;
            OnVisionChanged?.Invoke(CurrentVision, _maxVision);

            if (CurrentVision <= 0)
                OnVisionDepleted?.Invoke();
        }

        /// <summary>
        /// 시야가 완전히 소진되었는지 확인합니다.
        /// </summary>
        public bool IsBlind() => CurrentVision <= 0;

        /// <summary>
        /// 게임 재시작 시 시야를 최대치로 초기화합니다.
        /// </summary>
        public void ResetVision()
        {
            CurrentVision = _maxVision;
            CurrentBet = 0;
            OnVisionChanged?.Invoke(CurrentVision, _maxVision);
        }
    }
}