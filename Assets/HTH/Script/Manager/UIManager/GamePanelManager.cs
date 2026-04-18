using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// Hit/Stay 버튼 / Result 패널 / GameOver 패널을 담당합니다.
    /// Result와 GameOver는 각각 별도 패널로 관리합니다.
    /// </summary>
    public class GamePanelManager : MonoBehaviour
    {
        // ─── Inspector — Hit/Stay ─────────────────────────────────
        [Header("게임 버튼")]
        [SerializeField] private Button _hitButton;
        [SerializeField] private Button _stayButton;

        // ─── Inspector — Result ───────────────────────────────────
        [Header("Result 패널")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [SerializeField] private TextMeshProUGUI _resultExprText;
        [SerializeField] private TextMeshProUGUI _resultCompareText;
        [SerializeField] private Button _resultNextButton;
        [SerializeField] private TextMeshProUGUI _resultNextButtonLabel;

        // ─── Inspector — GameOver ─────────────────────────────────
        [Header("GameOver 패널")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _gameOverTitleText;
        [SerializeField] private TextMeshProUGUI _gameOverDetailText;
        [SerializeField] private Button _gameOverRestartButton;

        [Header("연산자 선택 패널")]
        [SerializeField] private GameObject _operatorChoicePanel;
        [SerializeField] private Button _operatorDiscardButton;

        // ─── Hit/Stay ─────────────────────────────────────────────

        /// <summary>Hit / Stay 버튼 콜백을 등록합니다.</summary>
        public void Setup(Action onHit, Action onStand)
        {
            SetGameButtonLabel(_hitButton, "HIT");
            SetGameButtonLabel(_stayButton, "STAY");

            _hitButton.onClick.RemoveAllListeners();
            _hitButton.onClick.AddListener(() => onHit?.Invoke());
            _stayButton.onClick.RemoveAllListeners();
            _stayButton.onClick.AddListener(() => onStand?.Invoke());
        }

        /// <summary>Hit / Stay 버튼을 활성화합니다.</summary>
        public void EnableButtons()
        {
            _hitButton.interactable = true;
            _stayButton.interactable = true;
        }

        /// <summary>Hit / Stay 버튼을 비활성화합니다.</summary>
        public void DisableButtons()
        {
            _hitButton.interactable = false;
            _stayButton.interactable = false;
        }

        // ─── Result 패널 ──────────────────────────────────────────

        /// <summary>
        /// Result 패널을 표시합니다.
        /// win = true면 다음 스테이지, false면 재도전 버튼을 표시합니다.
        /// </summary>
        public void ShowResult(
            string description,
            bool win,
            Action onNext)
        {
            _resultPanel?.SetActive(true);

            _resultTitleText.text = win ? "S U C C E S S" : "B U S T !";
            _resultTitleText.color = win
                ? UIColor.Hex("#4CAF50")
                : UIColor.Hex("#F44336");

            _resultExprText.text = description;
            _resultNextButtonLabel.text = win ? "다음 스테이지" : "재  도  전";

            _resultNextButton.onClick.RemoveAllListeners();
            _resultNextButton.onClick.AddListener(() =>
            {
                _resultPanel?.SetActive(false);
                onNext?.Invoke();
            });
        }

        /// <summary>결과 비교 텍스트를 갱신합니다.</summary>
        public void SetResultCompare(
            long playerTotal,
            long dealerTotal,
            long bustValue,
            bool win)
        {
            if (_resultCompareText == null) return;

            long playerDiff = Mathf.Abs((int)(bustValue - playerTotal));
            long dealerDiff = Mathf.Abs((int)(bustValue - dealerTotal));

            _resultCompareText.text = win
                ? $"플레이어 {playerTotal:N0} (차이:{playerDiff}) " +
                  $"vs 딜러 {dealerTotal:N0} (차이:{dealerDiff})"
                : $"플레이어 {playerTotal:N0} (차이:{playerDiff}) " +
                  $"vs 딜러 {dealerTotal:N0} (차이:{dealerDiff})";

            _resultCompareText.color = win
                ? UIColor.Hex("#FFD700")
                : UIColor.Hex("#F44336");
        }

        /// <summary>Result 패널을 닫습니다.</summary>
        public void HideResult()
            => _resultPanel?.SetActive(false);

        // ─── GameOver 패널 ────────────────────────────────────────

        /// <summary>게임 오버 패널을 표시합니다.</summary>
        public void ShowGameOver(Action onRestart)
        {
            _gameOverPanel?.SetActive(true);
            _gameOverTitleText.text = "GAME OVER";
            _gameOverTitleText.color = UIColor.Hex("#F44336");
            _gameOverDetailText.text =
                $"시야를 모두 잃었습니다.\n" +
                $"남은 시야: {VisionManager.Instance?.CurrentVision}";

            BindRestart(onRestart);
        }

        /// <summary>스테이지 클리어 패널을 표시합니다.</summary>
        public void ShowStageClear(Action onRestart)
        {
            _gameOverPanel?.SetActive(true);
            _gameOverTitleText.text = "C L E A R !";
            _gameOverTitleText.color = UIColor.Hex("#FFD700");
            _gameOverDetailText.text =
                $"모든 스테이지를 클리어했습니다!\n" +
                $"남은 시야: {VisionManager.Instance?.CurrentVision}";

            BindRestart(onRestart);
        }

        /// <summary>GameOver 패널을 닫습니다.</summary>
        public void HideGameOver()
            => _gameOverPanel?.SetActive(false);

        private void BindRestart(Action onRestart)
        {
            _gameOverRestartButton.onClick.RemoveAllListeners();
            _gameOverRestartButton.onClick.AddListener(() =>
            {
                _gameOverPanel?.SetActive(false);
                onRestart?.Invoke();
            });
        }
        public void ShowOperatorChoice()
        {
            _operatorChoicePanel?.SetActive(true);
            _operatorDiscardButton?.onClick.RemoveAllListeners();
            if (_operatorDiscardButton != null)
                _operatorDiscardButton.gameObject.SetActive(false);
        }

        public void ShowAceChoice(Action onSelectOne, Action onSelectEleven)
        {
            SetGameButtonLabel(_hitButton, "A = 1");
            SetGameButtonLabel(_stayButton, "A = 11");

            _hitButton.onClick.RemoveAllListeners();
            _hitButton.onClick.AddListener(() => onSelectOne?.Invoke());

            _stayButton.onClick.RemoveAllListeners();
            _stayButton.onClick.AddListener(() => onSelectEleven?.Invoke());

            EnableButtons();
        }

        public void HideOperatorChoice()
        {
            _operatorChoicePanel?.SetActive(false);
            if (_operatorDiscardButton != null)
                _operatorDiscardButton.gameObject.SetActive(true);
        }

        private void SetGameButtonLabel(Button button, string label)
        {
            if (button == null) return;

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.text = label;
                return;
            }

            var legacy = button.GetComponentInChildren<Text>(true);
            if (legacy != null)
                legacy.text = label;
        }
    }
}
