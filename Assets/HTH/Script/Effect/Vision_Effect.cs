using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 시야(Vision) 감소에 따른 화면 후처리 효과를 담당합니다.
    /// VisionManager.OnVisionChanged를 구독해 자동으로 갱신됩니다.
    ///
    /// 시야 비율별 효과
    /// 80% 이상  : 효과 없음
    /// 50~80%    : 경미한 비네트
    /// 20~50%    : 강한 비네트
    /// 20% 미만  : 극단적 암전 + 카드 블러
    /// </summary>
    public class Vision_Effect : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("비네트 패널")]
        [Tooltip("화면 외곽 암전 이미지 (Canvas 최상단에 배치)")]
        [SerializeField] private Image _vignetteImage;

        [Tooltip("비네트 전환 속도 (초)")]
        [SerializeField] private float _transitionDuration = 0.5f;

        // ─── 내부 상태 ───────────────────────────────────────────
        private Coroutine _transitionCoroutine;
        private float _targetAlpha;

        // ─── 비네트 단계별 알파값 ─────────────────────────────────
        private const float ALPHA_NONE = 0f;
        private const float ALPHA_LIGHT = 0.3f;
        private const float ALPHA_STRONG = 0.65f;
        private const float ALPHA_EXTREME = 0.9f;

        // ═══════════════════════════════════════════════════════
        //  생명주기
        // ═══════════════════════════════════════════════════════

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

        private void Start()
        {
            // 초기 상태 — 완전 투명
            if (_vignetteImage != null)
                SetAlphaDirect(ALPHA_NONE);
        }

        // ═══════════════════════════════════════════════════════
        //  Vision 이벤트 핸들러
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// VisionManager.OnVisionChanged 구독 핸들러.
        /// 시야 비율에 따라 비네트 강도를 부드럽게 전환합니다.
        /// </summary>
        private void HandleVisionChanged(int current, int max)
        {
            float ratio = (float)current / max;
            _targetAlpha = GetTargetAlpha(ratio);

            // 기존 전환 중단 후 새 전환 시작
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(
                TransitionAlpha(_targetAlpha));
        }

        /// <summary>
        /// 시야 비율을 비네트 알파값으로 변환합니다.
        /// </summary>
        private float GetTargetAlpha(float visionRatio)
        {
            if (visionRatio >= 0.8f) return ALPHA_NONE;
            if (visionRatio >= 0.5f) return Mathf.Lerp(
                ALPHA_LIGHT, ALPHA_NONE,
                (visionRatio - 0.5f) / 0.3f);
            if (visionRatio >= 0.2f) return Mathf.Lerp(
                ALPHA_STRONG, ALPHA_LIGHT,
                (visionRatio - 0.2f) / 0.3f);
            return Mathf.Lerp(
                ALPHA_EXTREME, ALPHA_STRONG,
                visionRatio / 0.2f);
        }

        // ═══════════════════════════════════════════════════════
        //  전환 코루틴
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 비네트 알파값을 목표값까지 부드럽게 전환합니다.
        /// </summary>
        private IEnumerator TransitionAlpha(float targetAlpha)
        {
            if (_vignetteImage == null) yield break;

            float startAlpha = _vignetteImage.color.a;
            float elapsed = 0f;

            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _transitionDuration;

                // EaseInOut 커브 적용
                t = t * t * (3f - 2f * t);

                SetAlphaDirect(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetAlphaDirect(targetAlpha);
            _transitionCoroutine = null;
        }

        /// <summary>알파값을 즉시 적용합니다.</summary>
        private void SetAlphaDirect(float alpha)
        {
            if (_vignetteImage == null) return;
            var col = _vignetteImage.color;
            _vignetteImage.color = new Color(col.r, col.g, col.b, alpha);
        }
    }
}