using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
        [SerializeField] private PauseManager _pauseManager;
        [SerializeField] private GameObject _gameOverPanel;

        [SerializeField] private GameOverVideoPlayer _videoPlayer;
        [SerializeField] private RawImage _gameOverImage;

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

        // ─── GameOver 패널 ────────────────────────────────────────

        /// <summary>게임 오버 패널을 표시합니다.</summary>
        public void ShowGameOver(Action onRestart)
        {
            _pauseManager?.Block();

            _gameOverPanel?.SetActive(true);
            _gameOverImage?.gameObject.SetActive(true);
            _videoPlayer?.Play(() =>
            {
                _pauseManager?.Unblock();
                BindRestart(onRestart);
            });
        }

        /// <summary>스테이지 클리어 패널을 표시합니다.</summary>
        public void ShowStageClear(Action onRestart)
        {
            _gameOverPanel?.SetActive(true);

            BindRestart(onRestart);
        }

        /// <summary>GameOver 패널을 닫습니다.</summary>
        public void HideGameOver()
            => _gameOverPanel?.SetActive(false);

        private void BindRestart(Action onRestart)
        {
            _gameOverPanel?.SetActive(false);
            onRestart?.Invoke();
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
