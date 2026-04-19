using UnityEngine;
using UnityEngine.UI;

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

        private void Start()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartButtonClicked);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        private void OnStartButtonClicked()
        {
            Debug.Log("[Title] Start Button Clicked. Loading Game Scene...");
            if (SceneLoadManager.Instance != null)
            {
                SceneLoadManager.Instance.LoadGame();
            }
            else
            {
                // SceneLoadManager가 씬에 없을 경우를 대비한 직접 로드
                UnityEngine.SceneManagement.SceneManager.LoadScene("InGameScene");
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
