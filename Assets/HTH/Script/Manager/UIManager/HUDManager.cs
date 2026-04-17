using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 스테이지 정보, 시야 바, 현재 연산값 등
    /// 게임 중 상단 HUD와 패널 텍스트를 담당합니다.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        // ─── Inspector — BetPanel ────────────────────────────────
        [Header("BetPanel")]
        [SerializeField] private TextMeshProUGUI _betStageText;
        [SerializeField] private TextMeshProUGUI _betQuotaText;
        [SerializeField] private TextMeshProUGUI _betVisionText;

        // ─── Inspector — GamePanel ───────────────────────────────
        [Header("GamePanel")]
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private TextMeshProUGUI _quotaText;
        [SerializeField] private Image _visionBarFill;
        [SerializeField] private TextMeshProUGUI _visionValueText;
        [SerializeField] private TextMeshProUGUI _currentValueText;

        // ─── Inspector — ResultPanel ─────────────────────────────
        [Header("ResultPanel")]
        [SerializeField] private TextMeshProUGUI _resultCompareText;

        // ─── 의존성 ───────────────────────────────────────────────
        private FieldUIManager _fieldUIManager;
        private HandUIManager _handUIManager;

        // ─── 초기화 ───────────────────────────────────────────────

        /// <summary>
        /// 블러 적용을 위해 FieldUIManager / HandUIManager를 주입합니다.
        /// GameUIManager.Awake에서 호출합니다.
        /// </summary>
        public void Initialize(FieldUIManager fieldUI, HandUIManager handUI)
        {
            _fieldUIManager = fieldUI;
            _handUIManager = handUI;
        }

        // ─── 생명주기 ─────────────────────────────────────────────

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

        // ─── BetPanel HUD ─────────────────────────────────────────

        /// <summary>배팅 패널의 스테이지 정보를 갱신합니다.</summary>
        public void SetBetPanelInfo(int stageIndex, long quota)
        {
            if (_betStageText != null) _betStageText.text = $"STAGE {stageIndex}";
            if (_betQuotaText != null) _betQuotaText.text = $"할당량: {quota:N0}";
            if (_betVisionText != null)
                _betVisionText.text =
                    $"보유 시야: {VisionManager.Instance?.CurrentVision}";
        }

        // ─── GamePanel HUD ────────────────────────────────────────

        /// <summary>게임 패널 스테이지 정보를 갱신합니다.</summary>
        public void SetGamePanelInfo(int stageIndex, long quota)
        {
            if (_stageText != null) _stageText.text = $"STAGE {stageIndex}";
            if (_quotaText != null) _quotaText.text = $"할당량: {quota:N0}";
        }

        /// <summary>
        /// 현재 필드 연산값 텍스트를 갱신합니다.
        /// 할당량 초과 시 빨간색, 일치 시 금색, 정상 시 흰색입니다.
        /// </summary>
        public void UpdateCurrentValue(List<CardDataSO> field, long quota = 0, bool flexibleAce = false,
            long bustThreshold = 21, bool currentValueSet = true)
        {
            if (_currentValueText == null) return;

            if (field.Count == 0)
            {
                _currentValueText.text = "= 0";
                _currentValueText.color = UIColor.Hex("#E8E0D0");
                return;
            }

            long value = ExpressionEvaluator.Evaluate(field, flexibleAce, bustThreshold);
            _currentValueText.text = $"= {value:N0}";

            if (currentValueSet)
            {
                // 높아야 하는 스테이지 — 초과 시 빨강
                if (quota > 0 && value > quota)
                    _currentValueText.color = UIColor.Hex("#F44336");
                else if (quota > 0 && value == quota)
                    _currentValueText.color = UIColor.Hex("#FFD700");
                else
                    _currentValueText.color = UIColor.Hex("#E8E0D0");
            }
            else
            {
                // 낮아야 하는 스테이지 — 미달 시 빨강
                if (quota > 0 && value > quota)
                    _currentValueText.color = UIColor.Hex("#F44336");
                else if (quota > 0 && value == quota)
                    _currentValueText.color = UIColor.Hex("#FFD700");
                else
                    _currentValueText.color = UIColor.Hex("#E8E0D0");
            }
        }

        // ─── ResultPanel ──────────────────────────────────────────

        /// <summary>결과 패널의 수치 비교 텍스트를 갱신합니다.</summary>
        public void SetResultInfo(long finalValue, long quota, bool win)
        {
            if (_resultCompareText == null) return;

            _resultCompareText.text = win
                ? $"{finalValue:N0} ≤ {quota:N0}"
                : $"{finalValue:N0} > {quota:N0}";

            _resultCompareText.color = win
                ? UIColor.Hex("#FFD700")
                : UIColor.Hex("#F44336");
        }

        // ─── Vision 이벤트 ────────────────────────────────────────

        /// <summary>
        /// VisionManager.OnVisionChanged 핸들러.
        /// 시야 바 갱신 + 카드 블러 적용을 처리합니다.
        /// </summary>
        private void HandleVisionChanged(int current, int max)
        {
            float ratio = (float)current / max;
            UpdateVisionBar(current, ratio);

            _fieldUIManager?.ApplyBlur(ratio);
            _handUIManager?.ApplyBlur(ratio);
        }

        /// <summary>시야 바 비율과 색상을 갱신합니다.</summary>
        private void UpdateVisionBar(int current, float ratio)
        {
            if (_visionBarFill != null)
            {
                _visionBarFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
                _visionBarFill.color = ratio > 0.5f
                    ? Color.Lerp(
                        UIColor.Hex("#FFC107"),
                        UIColor.Hex("#4CAF50"),
                        (ratio - 0.5f) * 2f)
                    : Color.Lerp(
                        UIColor.Hex("#F44336"),
                        UIColor.Hex("#FFC107"),
                        ratio * 2f);
            }

            if (_visionValueText != null)
                _visionValueText.text = current.ToString();
        }
    }
}