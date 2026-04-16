using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 게임 오버 / 스테이지 클리어 패널의 텍스트와 버튼을 담당합니다.
    /// 두 화면이 같은 패널을 재활용하므로 하나의 Manager가 처리합니다.
    /// </summary>
    public class GameOverPanelManager : MonoBehaviour
    {
        [Header("GameOverPanel 요소")]
        [Tooltip("게임 오버 / 클리어 타이틀 텍스트")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Tooltip("상세 설명 텍스트")]
        [SerializeField] private TextMeshProUGUI _detailText;

        [Tooltip("재시작 버튼")]
        [SerializeField] private Button _restartButton;

        /// <summary>
        /// 게임 오버 화면을 설정합니다.
        /// 시야가 0이 되었을 때 GameUIManager가 호출합니다.
        /// </summary>
        public void SetupGameOver(Action onRestart)
        {
            _titleText.text = "GAME OVER";
            _titleText.color = UIColor.Hex("#F44336");
            _detailText.text =
                $"시야를 모두 잃었습니다.\n" +
                $"남은 시야: {VisionManager.Instance?.CurrentVision}";

            BindRestart(onRestart);
        }

        /// <summary>
        /// 스테이지 클리어 화면을 설정합니다.
        /// 모든 스테이지를 클리어했을 때 GameUIManager가 호출합니다.
        /// </summary>
        public void SetupStageClear(Action onRestart)
        {
            _titleText.text = "C L E A R !";
            _titleText.color = UIColor.Hex("#FFD700");
            _detailText.text =
                $"모든 스테이지를 클리어했습니다!\n" +
                $"남은 시야: {VisionManager.Instance?.CurrentVision}";

            BindRestart(onRestart);
        }

        /// <summary>재시작 버튼에 콜백을 등록합니다.</summary>
        private void BindRestart(Action onRestart)
        {
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(() => onRestart?.Invoke());
        }
    }
}