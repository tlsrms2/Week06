using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace HTH
{
    /// <summary>
    /// 게임 클리어 연출을 담당합니다.
    ///
    /// 흐름
    /// 1. 페이드 아웃 (화면 → 검정)
    /// 2. 영상 재생
    /// 3. 페이드 아웃 (화면 → 검정)
    /// 4. onComplete 콜백 호출
    /// </summary>
    public class GameClear : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("페이드")]
        [Tooltip("페이드 전용 Image — 검정 색상, 전체 화면 크기")]
        [SerializeField] private Image _fadeImage;

        [Tooltip("페이드 인/아웃 시간 (초)")]
        [SerializeField] private float _fadeDuration = 1f;

        [Header("영상")]
        [Tooltip("게임 클리어 영상 플레이어")]
        [SerializeField] private VideoPlayer _videoPlayer;

        // ─── 상태 ─────────────────────────────────────────────────
        private Action _onComplete;

        // ─── 생명주기 ─────────────────────────────────────────────

        private void Awake()
        {
            // 시작 시 완전 투명
            SetFadeAlpha(0f);
            _videoPlayer.loopPointReached += OnVideoEnd;
            _videoPlayer.playOnAwake = false;
        }

        private void OnDestroy()
        {
            _videoPlayer.loopPointReached -= OnVideoEnd;
        }

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>
        /// 게임 클리어 연출을 시작합니다.
        /// onComplete : 모든 연출 완료 후 호출될 콜백
        /// </summary>
        public void Play(Action onComplete = null)
        {
            _onComplete = onComplete;
            gameObject.SetActive(true);
            StartCoroutine(PlayRoutine());
        }

        // ─── 연출 코루틴 ─────────────────────────────────────────

        private IEnumerator PlayRoutine()
        {
            // 1. 페이드 아웃 (투명 → 검정)
            yield return StartCoroutine(Fade(0f, 1f, _fadeDuration));

            // 2. 영상 재생
            _videoPlayer.Play();

            // 영상이 준비될 때까지 대기
            yield return new WaitUntil(() => _videoPlayer.isPrepared && _videoPlayer.isPlaying);

            // 3. 영상 길이의 75% 시점까지 대기
            double targetTime = _videoPlayer.length * 0.75;
            yield return new WaitUntil(() => _videoPlayer.time >= targetTime);

            // 4. 페이드 아웃 (투명 → 검정)
            yield return StartCoroutine(Fade(0f, 1f, _fadeDuration));
        }

        private void OnVideoEnd(VideoPlayer vp)
        {
            _videoPlayer.gameObject.SetActive(false);
            StartCoroutine(EndRoutine());
        }

        private IEnumerator EndRoutine()
        {
            // 3. 페이드 아웃 (투명 → 검정)
            SetFadeAlpha(0f);
            yield return StartCoroutine(Fade(0f, 1f, _fadeDuration));

            // 4. 완료 콜백
            gameObject.SetActive(false);
            _onComplete?.Invoke();
            _onComplete = null;
            SceneManager.LoadScene("InGameScene");
        }

        // ─── 페이드 유틸 ─────────────────────────────────────────

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            SetFadeAlpha(from);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetFadeAlpha(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetFadeAlpha(to);
        }

        private void SetFadeAlpha(float alpha)
        {
            if (_fadeImage == null) return;
            var color = _fadeImage.color;
            color.a = alpha;
            _fadeImage.color = color;
        }
    }
}