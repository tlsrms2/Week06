using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        private Vignette _vignette;
        private Coroutine _vignetteCoroutine;
        
        private void Start()
        {
            // 1. 기존 테스트 코드와 동일하게 메인 카메라 내에서 먼저 확인
            if (Camera.main != null)
            {
                Volume camVolume = Camera.main.GetComponentInChildren<Volume>();
                if (camVolume != null && camVolume.profile != null && camVolume.profile.TryGet(out _vignette))
                {
                    Debug.Log($"[VisionManager] 카메라 내부에서 Vignette 효과를 찾았습니다! ({camVolume.gameObject.name})");
                    return;
                }
            }

            // 2. 못 찾았을 경우 씬 전체 탐색
            Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            foreach (var vol in volumes)
            {
                if (vol.profile != null && vol.profile.TryGet(out _vignette))
                {
                    Debug.Log($"[VisionManager] 씬에서 Vignette 효과를 찾았습니다! ({vol.gameObject.name})");
                    return;
                }
            }

            Debug.LogWarning("[VisionManager] 씬에서 Vignette(비네뜨) 효과가 포함된 Volume을 찾을 수 없습니다.");
        }

        private void OnEnable()
        {
            OnVisionChanged += HandleVisionChanged;
        }

        private void OnDisable()
        {
            OnVisionChanged -= HandleVisionChanged;
        }

        /// <summary>VisionManager.OnVisionChanged 구독 핸들러 (UI 갱신용 등).</summary>
        private void HandleVisionChanged(int current, int max)
        {
            Debug.Log($"[GameUI] Vision — {current}/{max}");
        }

        private void ApplyVignetteEffect()
        {
            if (_vignette == null)
            {
                Volume vol = FindAnyObjectByType<Volume>();
                if (vol != null && vol.profile != null)
                {
                    vol.profile.TryGet(out _vignette);
                    Debug.Log($"[VisionManager] Apply 시점에 비네뜨 재검색 성공! ({vol.gameObject.name})");
                }
            }

            // 시야 비율 계산 (1.0 = 풀 시야, 0.0 = 시야 없음)
            float visionRatio = (float)CurrentVision / MaxVision;
            float targetVignette = 1.0f - visionRatio;
            
            // FOV 계산: 시야가 100%일 때 60, 0%일 때 125
            float targetFOV = Mathf.Lerp(125f, 60f, visionRatio);
            
            if (_vignetteCoroutine != null) StopCoroutine(_vignetteCoroutine);
            _vignetteCoroutine = StartCoroutine(SmoothVisionRoutine(targetVignette, targetFOV, 1.0f));
        }

        private System.Collections.IEnumerator SmoothVisionRoutine(float targetVignette, float targetFOV, float duration)
        {
            float startVignette = _vignette != null ? _vignette.intensity.value : 0f;
            float startFOV = Camera.main != null ? Camera.main.fieldOfView : 60f;
            float elapsed = 0f;

            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.intensity.overrideState = true;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 비네트 적용
                if (_vignette != null)
                {
                    _vignette.intensity.value = Mathf.Lerp(startVignette, targetVignette, t);
                }

                // FOV 적용
                if (Camera.main != null)
                {
                    Camera.main.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                }

                yield return null;
            }

            if (_vignette != null) _vignette.intensity.value = targetVignette;
            if (Camera.main != null) Camera.main.fieldOfView = targetFOV;
            
            _vignetteCoroutine = null;
            Debug.Log($"[VisionManager] 시각 연출 완료 — Vignette:{targetVignette:F2}, FOV:{targetFOV:F1}");
        }

        // ─── 배팅 API ─────────────────────────────────────────────
        
        public void SetBet(int amount)
        {
            CurrentBet = Mathf.Clamp(amount, 1, CurrentVision);
        }

        public bool CanBet(int minimumBet) => CurrentVision >= minimumBet;

        public void WinBet()
        {
            CurrentBet = 0;
            OnVisionChanged?.Invoke(CurrentVision, _maxVision);
            ApplyVignetteEffect();
        }

        public void LoseBet()
        {
            CurrentVision = Mathf.Max(0, CurrentVision - CurrentBet);
            CurrentBet = 0;
            OnVisionChanged?.Invoke(CurrentVision, _maxVision);
            
            Debug.Log($"[VisionManager] LoseBet 호출 완료! 현재 시야: {CurrentVision}/{_maxVision}");
            ApplyVignetteEffect();

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
            ApplyVignetteEffect();
        }
    }
}