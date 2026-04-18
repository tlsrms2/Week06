using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 게임 중 HUD 텍스트를 관리합니다.
    /// 플레이어 현재 값 / 딜러 Stay 후 값 표시.
    /// normalJudge / reverseJudge 색상 분기.
    ///
    /// 색상 기준 (normalJudge)
    /// ├── bustValue 초과        → 붉은색
    /// ├── bustValue 80% 이상    → 노란색
    /// ├── bustValue 일치        → 어두운 녹색
    /// └── 그 외                → 흰색
    ///
    /// 색상 기준 (reverseJudge)
    /// ├── bustValue 미만        → 붉은색
    /// ├── bustValue 120% 이하   → 노란색
    /// ├── bustValue 일치        → 어두운 녹색
    /// └── 그 외                → 흰색
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("HUD 텍스트")]
        [Tooltip("스테이지 텍스트")]
        [SerializeField] private TextMeshProUGUI _stageText;

        [Tooltip("bustValue 텍스트")]
        [SerializeField] private TextMeshProUGUI _bustValueText;

        [Tooltip("플레이어 현재 값 텍스트")]
        [SerializeField] private TextMeshProUGUI _playerValueText;

        [Tooltip("딜러 Stay 후 값 텍스트")]
        [SerializeField] private TextMeshProUGUI _dealerValueText;

        // ─── 색상 상수 ───────────────────────────────────────────
        private static readonly Color ColorBust = UIColor.Hex("#F44336"); // 붉은색
        private static readonly Color ColorClose = UIColor.Hex("#FFC107"); // 노란색
        private static readonly Color ColorExact = UIColor.Hex("#388E3C"); // 어두운 녹색
        private static readonly Color ColorDefault = UIColor.Hex("#E8E0D0"); // 흰색

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>스테이지 정보를 갱신합니다.</summary>
        public void SetStageInfo(int stageIndex, long bustValue)
        {
            if (_stageText != null)
                _stageText.text = $"STAGE {stageIndex}";

            if (_bustValueText != null)
                _bustValueText.text = $"목표: {bustValue:N0}";
        }

        /// <summary>
        /// 플레이어 현재 값을 갱신합니다.
        /// 1Stage와 동일하게 bustValue 초과 기준으로 색상을 분기합니다.
        /// </summary>
        public void UpdatePlayerValue(
            List<CardDataSO> field,
            StageDataSO stage)
        {
            if (_playerValueText == null) return;

            if (field == null || field.Count == 0)
            {
                _playerValueText.text = "= 0";
                _playerValueText.color = ColorDefault;
                return;
            }

            long value = ExpressionEvaluator.Evaluate(
                field,
                stage.useFlexibleAce,
                stage.bustValue,
                stage.allowNegative);

            _playerValueText.text = $"= {value:N0}";
            _playerValueText.color = GetValueColor(value, stage);
        }

        /// <summary>
        /// 딜러 Stay 후 값을 갱신합니다.
        /// 딜러 턴 종료 시 GameUIManager가 호출합니다.
        /// </summary>
        public void UpdateDealerValue(
            List<CardDataSO> field,
            StageDataSO stage)
        {
            if (_dealerValueText == null) return;

            if (field == null || field.Count == 0)
            {
                _dealerValueText.text = "—";
                _dealerValueText.color = ColorDefault;
                return;
            }

            long value = ExpressionEvaluator.Evaluate(
                field,
                stage.useFlexibleAce,
                stage.bustValue,
                stage.allowNegative);

            _dealerValueText.text = $"딜러: {value:N0}";
            _dealerValueText.color = GetValueColor(value, stage);
        }

        /// <summary>딜러 값 텍스트를 초기화합니다.</summary>
        public void ClearDealerValue()
        {
            if (_dealerValueText == null) return;
            _dealerValueText.text = "—";
            _dealerValueText.color = ColorDefault;
        }

        // ─── 색상 판정 ────────────────────────────────────────────

        /// <summary>
        /// 값에 따른 색상을 반환합니다.
        /// 1Stage와 동일하게 bustValue 초과→붉은색 / 80%이상→노란색 / 일치→녹색
        /// </summary>
        private Color GetValueColor(long value, StageDataSO stage)
        {
            long bustValue = stage.bustValue;

            if (value > bustValue)
                return ColorBust;
            if (value == bustValue)
                return ColorExact;
            if (value >= (long)(bustValue * 0.8f))
                return ColorClose;
            return ColorDefault;
        }
    }
}
