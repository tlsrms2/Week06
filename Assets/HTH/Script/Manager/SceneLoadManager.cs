using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace HTH
{
    /// <summary>
    /// 씬 전환을 관리하는 싱글톤 매니저입니다.
    /// DontDestroyOnLoad를 통해 씬이 바뀌어도 유지됩니다.
    /// </summary>
    public class SceneLoadManager : MonoBehaviour
    {
        public static SceneLoadManager Instance { get; private set; }

        [Header("Scene Names")]
        public string titleSceneName = "TitleScene";
        public string inGameSceneName = "InGameScene";
        public string endingSceneName = "EndingScene";

        [Header("Fade Settings")]
        [SerializeField] private float _fadeDuration = 1f; // 페이드 아웃 2초, 페이드 인 2초

        private CanvasGroup _fadeCanvasGroup;
        private bool _isFading = false;

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

        /// <summary>
        /// 페이드 효과를 위한 캔버스 및 이미지를 동적으로 생성하여 화면을 꽉 채웁니다.
        /// </summary>
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
            rect.localPosition = Vector3.zero;

            _fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
        }

        public void LoadGame() => StartCoroutine(FadeRoutine(inGameSceneName));
        public void LoadTitle() => StartCoroutine(FadeRoutine(titleSceneName));
        public void LoadEnding() => StartCoroutine(FadeRoutine(endingSceneName));
        
        private IEnumerator FadeRoutine(string sceneName)
        {
            if (_isFading) yield break;
            _isFading = true;

            // Fade Out (검은색으로)
            _fadeCanvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
                yield return null;
            }
            _fadeCanvasGroup.alpha = 1f;

            // Scene Load
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 잠시 대기 (로딩 완료 후 안정화)
            yield return new WaitForSeconds(0.1f);

            // Fade In (투명하게)
            elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / _fadeDuration));
                yield return null;
            }
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;

            _isFading = false;
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
