using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using Unity.Cinemachine;

namespace HTH
{
    /// <summary>
    /// 씬 전환 및 카메라 트랜지션을 관리하는 싱글톤 매니저입니다.
    /// </summary>
    public class SceneLoadManager : MonoBehaviour
    {
        public static SceneLoadManager Instance { get; private set; }

        [Header("Scene Names")]
        public string titleSceneName = "TitleScene";
        public string inGameSceneName = "InGameScene";
        public string endingSceneName = "EndingScene";

        [Header("Fade Settings")]
        [SerializeField] private float _fadeDuration = 1f;

        private CanvasGroup _fadeCanvasGroup;
        private bool _isTransitioning = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                CreateFadeCanvas();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void CreateFadeCanvas()
        {
            GameObject fadeObj = new GameObject("FadeCanvas");
            fadeObj.transform.SetParent(this.transform);

            Canvas canvas = fadeObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; 

            fadeObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            fadeObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(fadeObj.transform);

            UnityEngine.UI.Image image = imageObj.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.black;

            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
        }

        public void LoadGame() => StartCoroutine(FadeRoutine(inGameSceneName));
        public void LoadTitle() => StartCoroutine(FadeRoutine(titleSceneName));
        public void LoadEnding() => StartCoroutine(FadeRoutine(endingSceneName));

        /// <summary>
        /// 시네머신 카메라 블렌딩을 이용해 타이틀에서 인게임으로 부드럽게 전환합니다.
        /// </summary>
        public void StartGameWithCameraTransition(CinemachineCamera titleCam, CinemachineCamera gameCam, Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(CameraTransitionRoutine(titleCam, gameCam, onComplete));
        }
        
        private IEnumerator CameraTransitionRoutine(CinemachineCamera titleCam, CinemachineCamera gameCam, Action onComplete)
        {
            _isTransitioning = true;

            // 1. 카메라 우선순위 변경하여 블렌딩 시작
            if (titleCam != null) titleCam.Priority = 0;
            if (gameCam != null) gameCam.Priority = 10;

            // 2. 시네머신 브레인이 블렌딩 중인지 확인 (혹은 고정 시간 대기)
            // CinemachineBrain을 찾아 블렌딩 종료를 기다립니다.
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                // 블렌딩이 시작될 때까지 한 프레임 대기
                yield return null;
                while (brain.IsBlending)
                {
                    yield return null;
                }
            }
            else
            {
                // 브레인을 못 찾을 경우 기본 2초 대기
                yield return new WaitForSeconds(2f);
            }

            _isTransitioning = false;
            onComplete?.Invoke();
        }

        private IEnumerator FadeRoutine(string sceneName)
        {
            if (_isTransitioning) yield break;
            _isTransitioning = true;

            _fadeCanvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
                yield return null;
            }
            _fadeCanvasGroup.alpha = 1f;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone) yield return null;

            yield return new WaitForSeconds(0.1f);

            elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / _fadeDuration));
                yield return null;
            }
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;

            _isTransitioning = false;
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
