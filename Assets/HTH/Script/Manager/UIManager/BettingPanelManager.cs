using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 배팅 패널의 배팅량 조작과 버튼 콜백을 담당합니다.
    /// 배팅량 증감 및 확정 처리를 이 클래스가 직접 관리합니다.
    /// </summary>
    public class BettingPanelManager : MonoBehaviour
    {
        [Header("BetPanel 요소")]
        [SerializeField] private TextMeshProUGUI _betAmountText;
        [SerializeField] private Button _betDecButton;
        [SerializeField] private Button _betIncButton;
        [SerializeField] private Button _betConfirmButton;

        private int _betAmount;

        /// <summary>
        /// 배팅 패널을 초기화합니다.
        /// 현재 배팅량을 설정하고 +/- 버튼과 확정 버튼에 콜백을 등록합니다.
        /// </summary>
        public void Setup(int currentBet, Action<int> onConfirm)
        {
            _betAmount = currentBet;
            _betAmountText.text = _betAmount.ToString();

            _betDecButton.onClick.RemoveAllListeners();
            _betDecButton.onClick.AddListener(() =>
            {
                _betAmount = Mathf.Clamp(
                    _betAmount - 5, 1,
                    VisionManager.Instance?.CurrentVision ?? 1);
                _betAmountText.text = _betAmount.ToString();
            });

            _betIncButton.onClick.RemoveAllListeners();
            _betIncButton.onClick.AddListener(() =>
            {
                _betAmount = Mathf.Clamp(
                    _betAmount + 5, 1,
                    VisionManager.Instance?.CurrentVision ?? 1);
                _betAmountText.text = _betAmount.ToString();
            });

            _betConfirmButton.onClick.RemoveAllListeners();
            _betConfirmButton.onClick.AddListener(
                () => onConfirm?.Invoke(_betAmount));
        }
    }
}