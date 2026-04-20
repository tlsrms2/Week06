using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System;

namespace HTH
{
    /// <summary>
    /// 타이틀 씬의 UI 기능을 담당합니다.
    /// </summary>
    public class TitleUIManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;

        [Header("Title UI Panels")]
        [SerializeField] private GameObject _titleContent; // 타이틀 텍스트와 버튼들이 담긴 루트 오브젝트

        [Header("Cameras")]
        [SerializeField] private CinemachineCamera _titleCamera;
        [SerializeField] private CinemachineCamera _gameCamera;

        private Action _onStartCallback;

        private void Start()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartButtonClicked);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        /// <summary>
        /// GameManager에서 타이틀 화면을 세팅할 때 호출합니다.
        /// </summary>
        public void Setup(Action onStart)
        {
            _onStartCallback = onStart;
            if (_titleContent != null)
                _titleContent.SetActive(true);
        }

        private void OnStartButtonClicked()
        {
            AudioManager.instance.PlaySfx(AudioManager.Sfx.startButton);
            Debug.Log("[Title] Start Button Clicked. Transitioning to Game...");
            
            // 1. 타이틀 UI 숨기기
            if (_titleContent != null)
                _titleContent.SetActive(false);

            // 2. SceneLoadManager를 통해 카메라 트랜지션 실행
            if (SceneLoadManager.Instance != null && _titleCamera != null && _gameCamera != null)
            {
                SceneLoadManager.Instance.StartGameWithCameraTransition(_titleCamera, _gameCamera, () =>
                {
                    Debug.Log("[Title] Transition Complete. Invoking Game Start Callback...");
                    _onStartCallback?.Invoke();
                });
            }
            else
            {
                // 트랜지션 환경이 아닐 경우 기존처럼 씬 로드
                if (SceneLoadManager.Instance != null)
                    SceneLoadManager.Instance.LoadGame();
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("InGameScene");

                _onStartCallback?.Invoke();
            }
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("[Title] Quit Button Clicked.");
            if (SceneLoadManager.Instance != null)
            {
                SceneLoadManager.Instance.QuitGame();
            }
            else
            {
                Application.Quit();
            }
        }
    }
}
