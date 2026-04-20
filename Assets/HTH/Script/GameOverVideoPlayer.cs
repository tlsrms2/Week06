using UnityEngine;
using UnityEngine.Video;

namespace HTH
{
    public class GameOverVideoPlayer : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;

        private System.Action _onComplete;

        private void Awake()
        {
            _videoPlayer.loopPointReached += OnVideoEnd;
        }

        private void OnDestroy()
        {
            _videoPlayer.loopPointReached -= OnVideoEnd;
        }

        /// <summary>영상을 재생하고 완료 시 콜백을 호출합니다.</summary>
        public void Play(System.Action onComplete)
        {
            _onComplete = onComplete;
            gameObject.SetActive(true);
            _videoPlayer.Play();
        }

        private void OnVideoEnd(VideoPlayer vp)
        {
            gameObject.SetActive(false);
            _onComplete?.Invoke();
            _onComplete = null;
        }
    }
}