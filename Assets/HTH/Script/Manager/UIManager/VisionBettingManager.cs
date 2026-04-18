using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 시야 배팅 패널을 담당합니다.
    /// Tab키로 패널을 열고 닫습니다.
    /// 최소 / 최대 배팅량을 UI에 표시합니다.
    /// </summary>
    public class VisionBettingManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("배팅 패널")]
        [Tooltip("배팅 패널 루트")]
        [SerializeField] private GameObject _bettingPanel;

        [Header("배팅 UI 요소")]
        [Tooltip("현재 배팅량 텍스트")]
        [SerializeField] private TextMeshProUGUI _betAmountText;

        [Tooltip("최소 배팅량 텍스트")]
        [SerializeField] private TextMeshProUGUI _betMinText;

        [Tooltip("최대 배팅량 텍스트")]
        [SerializeField] private TextMeshProUGUI _betMaxText;

        [Tooltip("배팅량 감소 버튼")]
        [SerializeField] private Button _betDecButton;

        [Tooltip("배팅량 증가 버튼")]
        [SerializeField] private Button _betIncButton;

        [Tooltip("배팅 확정 버튼")]
        [SerializeField] private Button _betConfirmButton;

        // ─── 내부 상태 ───────────────────────────────────────────
        private int _betAmount;
        private int _betMin;
        private int _betMax;
        private Action<int> _onConfirm;
        private bool _isOpen;

        // ─── 생명주기 ─────────────────────────────────────────────

        private void Update()
        {
            // Tab키로 패널 열기/닫기
            if (Input.GetKeyDown(KeyCode.Tab))
                TogglePanel();
        }

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>
        /// 배팅 패널을 초기화합니다.
        /// GameUIManager가 PlayerTurn 진입 시 호출합니다.
        /// </summary>
        public void Setup(int betMin, int betMax, Action<int> onConfirm)
        {
            _betMin = betMin;
            _betMax = betMax;
            _onConfirm = onConfirm;
            _betAmount = betMin;

            RefreshUI();

            _betDecButton.onClick.RemoveAllListeners();
            _betDecButton.onClick.AddListener(() =>
            {
                _betAmount = Mathf.Clamp(_betAmount - 1, _betMin, _betMax);
                RefreshUI();
            });

            _betIncButton.onClick.RemoveAllListeners();
            _betIncButton.onClick.AddListener(() =>
            {
                _betAmount = Mathf.Clamp(_betAmount + 1, _betMin, _betMax);
                RefreshUI();
            });

            _betConfirmButton.onClick.RemoveAllListeners();
            _betConfirmButton.onClick.AddListener(() =>
            {
                ClosePanel();
                _onConfirm?.Invoke(_betAmount);
            });
        }
        /// <summary>
        /// 베팅 패널을 강제로 엽니다.
        /// 재도전 / 다음 스테이지 진입 시 GameUIManager가 호출합니다.
        /// </summary>
        public void OpenPanel()
        {
            _isOpen = true;
            _bettingPanel?.SetActive(true);
            RefreshUI();
        }

        /// <summary>패널을 강제로 닫습니다.</summary>
        public void ClosePanel()
        {
            _isOpen = false;
            _bettingPanel?.SetActive(false);
        }

        // ─── 내부 ────────────────────────────────────────────────

        /// <summary>Tab키 입력 시 패널을 열고 닫습니다.</summary>
        private void TogglePanel()
        {
            _isOpen = !_isOpen;
            _bettingPanel?.SetActive(_isOpen);

            if (_isOpen) RefreshUI();
        }

        /// <summary>배팅 UI 텍스트를 갱신합니다.</summary>
        private void RefreshUI()
        {
            if (_betAmountText != null)
                _betAmountText.text = _betAmount.ToString();

            if (_betMinText != null)
                _betMinText.text = $"최소: {_betMin}";

            if (_betMaxText != null)
                _betMaxText.text = $"최대: {_betMax}";
        }
    }
}