using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 결과 패널의 텍스트 갱신과 버튼 콜백을 담당합니다.
    /// </summary>
    public class ResultPanelManager : MonoBehaviour
    {
        [Header("ResultPanel 요소")]
        [Tooltip("결과 타이틀 텍스트 (SUCCESS / BUST)")]
        [SerializeField] private TextMeshProUGUI _resultTitleText;

        [Tooltip("수식 문자열 텍스트")]
        [SerializeField] private TextMeshProUGUI _resultExprText;

        [Tooltip("수치 비교 텍스트 (21 ≤ 21)")]
        [SerializeField] private TextMeshProUGUI _resultCompareText;

        [Tooltip("다음/재도전 버튼")]
        [SerializeField] private Button _resultButton;

        [Tooltip("다음/재도전 버튼 텍스트")]
        [SerializeField] private TextMeshProUGUI _resultButtonLabel;

        /// <summary>
        /// 결과 패널을 초기화합니다.
        /// 승패에 따라 텍스트 색상과 버튼 레이블을 설정합니다.
        /// </summary>
        public void Setup(string description, bool win, Action onNext)
        {
            _resultTitleText.text = win ? "S U C C E S S" : "B U S T !";
            _resultTitleText.color = win
                ? UIColor.Hex("#4CAF50")
                : UIColor.Hex("#F44336");

            _resultExprText.text = description;
            _resultButtonLabel.text = win ? "다음 스테이지" : "재  도  전";

            _resultButton.onClick.RemoveAllListeners();
            _resultButton.onClick.AddListener(() => onNext?.Invoke());
        }

        /// <summary>
        /// 수치 비교 텍스트를 갱신합니다.
        /// GameManager의 OnEnterResult에서 Setup 전에 호출합니다.
        /// </summary>
        public void SetCompareInfo(long finalValue, long quota, bool win)
        {
            if (_resultCompareText == null) return;

            _resultCompareText.text = win
                ? $"{finalValue:N0} ≤ {quota:N0}"
                : $"{finalValue:N0} > {quota:N0}";

            _resultCompareText.color = win
                ? UIColor.Hex("#FFD700")
                : UIColor.Hex("#F44336");
        }
    }
}