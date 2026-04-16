using System;
using UnityEngine;

/// <summary>
/// 시야(Vision) 관리 시스템 (싱글톤).
/// 기획안: "시야가 줄어들수록 화면 외곽 암전, UI 왜곡, 블러 처리가 일어납니다."
/// 배팅 → 실패 시 영구 상실 → 시각적 페널티 강화 루프.
/// </summary>
public class VisionManager : MonoBehaviour
{
    public static VisionManager Instance { get; private set; }

    [Header("Vision Settings")]
    [SerializeField] private int maxVision = 100;

    public int MaxVision => maxVision;
    public int CurrentVision { get; private set; }

    /// <summary>현재 시야 비율 (0.0 ~ 1.0)</summary>
    public float VisionRatio => (float)CurrentVision / maxVision;

    /// <summary>시야 변경 이벤트 (currentVision, maxVision)</summary>
    public event Action<int, int> OnVisionChanged;

    private int currentBet;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CurrentVision = maxVision;
    }

    /// <summary>배팅 금액 설정 (1 ~ 현재 시야)</summary>
    public void SetBet(int amount)
    {
        currentBet = Mathf.Clamp(amount, 1, CurrentVision);
    }

    /// <summary>현재 배팅 금액 반환</summary>
    public int GetCurrentBet() => currentBet;

    /// <summary>승리: 배팅 시야 보존, 배팅 초기화</summary>
    public void WinBet()
    {
        currentBet = 0;
    }

    /// <summary>패배: 배팅한 시야 영구 상실</summary>
    public void LoseBet()
    {
        CurrentVision = Mathf.Max(0, CurrentVision - currentBet);
        currentBet = 0;
        OnVisionChanged?.Invoke(CurrentVision, maxVision);
    }

    /// <summary>시야가 완전히 상실되었는지 확인</summary>
    public bool IsBlind() => CurrentVision <= 0;

    /// <summary>배팅 가능 여부 (최소 배팅량 이상의 시야 보유)</summary>
    public bool CanBet(int minimumBet) => CurrentVision >= minimumBet;

    /// <summary>게임 리셋 시 시야 초기화</summary>
    public void ResetVision()
    {
        CurrentVision = maxVision;
        currentBet = 0;
        OnVisionChanged?.Invoke(CurrentVision, maxVision);
    }

    /// <summary>시야 변경 이벤트를 강제 발행 (초기 UI 동기화용)</summary>
    public void ForceNotify()
    {
        OnVisionChanged?.Invoke(CurrentVision, maxVision);
    }
}
