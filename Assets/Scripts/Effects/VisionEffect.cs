using UnityEngine;

/// <summary>
/// 시야(Vision) 감소에 따른 시각적 페널티 효과 관리.
/// 기획안: "시야가 줄어들수록 화면 외곽 암전(Vignette), UI 왜곡, 블러 처리가 일어납니다.
///         수치가 극단적으로 낮아지면 카드의 형태가 뭉개져 마우스를 올려
///         집중(Focus)해야만 간신히 숫자를 판독할 수 있습니다."
///
/// VisionManager의 OnVisionChanged 이벤트를 구독하여 자동으로 효과를 조절합니다.
/// GameUI의 비네트 오버레이를 제어합니다.
/// </summary>
public class VisionEffect : MonoBehaviour
{
    private VisionManager visionManager;
    private GameUI gameUI;

    // 부드러운 전환을 위한 현재 효과 강도
    private float currentVignetteAlpha = 0f;
    private float targetVignetteAlpha = 0f;
    private float transitionSpeed = 2f;

    void Start()
    {
        // 지연 초기화 (GameManager가 다른 컴포넌트를 먼저 생성)
        Invoke(nameof(Initialize), 0.1f);
    }

    private void Initialize()
    {
        visionManager = VisionManager.Instance;
        if (visionManager != null)
        {
            visionManager.OnVisionChanged += OnVisionChanged;
        }
    }

    void OnDestroy()
    {
        if (visionManager != null)
            visionManager.OnVisionChanged -= OnVisionChanged;
    }

    void Update()
    {
        // 부드러운 비네트 전환
        if (Mathf.Abs(currentVignetteAlpha - targetVignetteAlpha) > 0.01f)
        {
            currentVignetteAlpha = Mathf.Lerp(currentVignetteAlpha, targetVignetteAlpha,
                Time.deltaTime * transitionSpeed);

            // GameUI의 비네트 직접 제어
            if (gameUI == null)
                gameUI = FindFirstObjectByType<GameUI>();

            if (gameUI != null && gameUI.vignetteGroup != null)
            {
                gameUI.vignetteGroup.alpha = currentVignetteAlpha;
            }
        }
    }

    private void OnVisionChanged(int current, int max)
    {
        float ratio = (float)current / max;
        CalculateEffects(ratio);
    }

    /// <summary>
    /// 시야 비율에 따른 효과 강도를 계산합니다.
    /// 80%+ : 정상 (효과 없음)
    /// 50~80% : 가벼운 Vignette
    /// 20~50% : 강한 Vignette
    /// 20% 미만 : 극단적 암전 + 카드 블러
    /// </summary>
    private void CalculateEffects(float visionRatio)
    {
        if (visionRatio >= 0.8f)
        {
            targetVignetteAlpha = 0f;
        }
        else if (visionRatio >= 0.5f)
        {
            // 0.5~0.8 → 비네트 0~0.3
            targetVignetteAlpha = Mathf.Lerp(0.3f, 0f, (visionRatio - 0.5f) / 0.3f);
        }
        else if (visionRatio >= 0.2f)
        {
            // 0.2~0.5 → 비네트 0.3~0.6
            targetVignetteAlpha = Mathf.Lerp(0.6f, 0.3f, (visionRatio - 0.2f) / 0.3f);
        }
        else
        {
            // 0~0.2 → 비네트 0.6~1.0
            targetVignetteAlpha = Mathf.Lerp(1f, 0.6f, visionRatio / 0.2f);
        }
    }
}
