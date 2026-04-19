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

        [Header("배팅 설정")]
        [Tooltip("증감 버튼이 이동할 고정 배팅 금액 목록입니다. (인스펙터에서 설정)")]
        [SerializeField] private int[] _betOptions = { 5, 10, 20, 50, 100 };

        [Header("추가 정보 UI")]
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private TextMeshProUGUI _goalText;
        [SerializeField] private TextMeshProUGUI _currentVisionText;

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
        public void Setup(int betMin, int betMax, int currentVision, int stageIndex, long bustValue, Action<int> onConfirm)
        {
            _betMin = betMin;
            _betMax = betMax;
            _onConfirm = onConfirm;

            // 배팅금은 이제 betMin (스테이지 설정값)으로 고정됩니다.
            _betAmount = betMin;

            // 스테이지 및 시야 정보 반영
            if (_stageText != null) _stageText.text = $"STAGE {stageIndex}";
            if (_goalText != null) _goalText.text = $"목표: {bustValue:N0}";
            if (_currentVisionText != null) _currentVisionText.text = $"보유 시야: {currentVision:N0}";

            RefreshUI();

            // 증감 버튼 리스너 제거 (기존 버튼이 레이아웃에 있을 수 있으므로 비활성 처리 권장)
            if (_betDecButton != null)
            {
                _betDecButton.onClick.RemoveAllListeners();
                _betDecButton.gameObject.SetActive(false); // 로직 제거 및 버튼 숨김
            }

            if (_betIncButton != null)
            {
                _betIncButton.onClick.RemoveAllListeners();
                _betIncButton.gameObject.SetActive(false); // 로직 제거 및 버튼 숨김
            }

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

        /// <summary>특정 금액을 직접 베팅액으로 설정합니다. (인스펙터 전용/외부 버튼용)</summary>
        public void SelectBet(int amount)
        {
            _betAmount = Mathf.Clamp(amount, _betMin, _betMax);
            RefreshUI();
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