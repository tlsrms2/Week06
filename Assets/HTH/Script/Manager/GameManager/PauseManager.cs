using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    public class PauseManager : MonoBehaviour
    {
        [SerializeField] Image _pausePanel;
        bool _isActive = false;
        bool _isBlocked = false;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Time.timeScale = 1f;
            _pausePanel.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (_isBlocked) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if(_isActive)
                {
                    _pausePanel.gameObject.SetActive(true);
                    Time.timeScale = 0f;
                    _isActive = false;
                }
                else
                {
                    _pausePanel.gameObject.SetActive(false);
                    Time.timeScale = 1f;
                    _isActive = true;
                }

                
            }
        }

        public void ResumeButton()
        {
            _pausePanel.gameObject.SetActive(false);
            Time.timeScale = 1f;
            _isActive = true;
        }
        public void GameExitButton()
        {
            Application.Quit();
        }

        /// <summary>PauseManager를 비활성화합니다. 게임오버 시 호출합니다.</summary>
        public void Block() => _isBlocked = true;

        /// <summary>PauseManager를 재활성화합니다. 재시작 시 호출합니다.</summary>
        public void Unblock() => _isBlocked = false;
    }
}
