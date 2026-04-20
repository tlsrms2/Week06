using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System;
using Unity.Cinemachine;

namespace HTH
{
    /// <summary>
    /// 스테이지 시작, 승/패에 따른 눈알 프리팹 스폰 및 이동 연출을 관리합니다.
    /// </summary>
    public class EyeSpawnManager : MonoBehaviour
    {
        [Header("프리팹 및 위치 설정")]
        [Tooltip("생성할 눈알 프리팹")]
        [SerializeField] private GameObject _eyePrefab;
        [Tooltip("스테이지 시작 시 스폰될 목표 위치 (이 위치에서 자라납니다)")]
        [SerializeField] private Transform _stagePoint;
        [Tooltip("스테이지 패배 시 이동할 목표 위치")]
        [SerializeField] private Transform _losePoint;

        [Header("이동 시간 및 효과 설정")]
        [SerializeField] private float _spawnUpDuration = 1.5f;
        [SerializeField] private float _moveBackDuration = 1.5f;
        [SerializeField] private float _moveToLoseDuration = 1.5f;

        [Header("딜러 공통 설정")]
        [SerializeField] private Transform _dealerTransform;
        [SerializeField] private Animator _dealerAnimator;
        [SerializeField] private string _dealerIdleAnimTrigger = "Idle";
        
        [Tooltip("특수 연출 시 활성화될 딜러 몸의 눈알 오브젝트들")]
        [SerializeField] private GameObject[] _dealerEyes;

        [Header("일반 패배 연출 (시야 50 제외)")]
        [Tooltip("일반 패배 시 딜러가 이동할 위치 (좌표값)")]
        [SerializeField] private Vector3 _dealerNormalLosePoint;
        [Tooltip("일반 패배 시 재생할 애니메이션 트리거")]
        [SerializeField] private string _dealerNormalLoseAnimTrigger = "Trigger7";
        [SerializeField] private float _destroyWaitAfterLose = 2f;
        [Tooltip("일반 패배 시 눈알이 줄어들며 사라지는 시간")]
        [SerializeField] private float _shrinkDuration = 0.5f;
        [SerializeField] private GameObject _breakParticlePrefab;

        [Header("특수 패배 연출 (피 50 이하 1회)")]
        [Header("[1. 전조 이동]")]
        [Tooltip("연출 시작 시 딜러가 천천히 이동할 전조 위치")]
        [SerializeField] private Transform _dealerPreSpecialPoint;
        [Tooltip("전조 위치까지 이동하는 시간")]
        [SerializeField] private float _preSpecialMoveDuration = 2.0f;
        [Tooltip("이동 완료 후 카메라 전환 전까지의 대기 시간")]
        [SerializeField] private float _delayBeforeCameraTransition = 1.0f;

        [Header("[2. 본 연출]")]
        [SerializeField] private CinemachineCamera _mainGameCamera;
        [SerializeField] private CinemachineCamera _reachOutCamera;
        [SerializeField] private Transform _dealerReachOutPoint;
        [SerializeField] private string _dealerReachOutAnimTrigger = "ReachOut";

        [Header("블랙아웃(깜깜해짐) 공통 설정")]
        [SerializeField] private float _blackoutFadeTime = 0.5f;
        [SerializeField] private float _blackoutHoldTime = 1f;

        [Header("블랙아웃 타이밍 딜레이")]
        [SerializeField] private float _normalBlackoutDelay = 1.5f;
        [SerializeField] private float _specialBlackoutDelay = 4.0f;

        private GameObject _currentEye;
        private Vector3 _dealerInitialPos;
        private Quaternion _dealerInitialRot;
        private static bool _hasPlayedSpecialRoutine = false;

        private void Awake()
        {
            if (_dealerTransform != null)
            {
                _dealerInitialPos = _dealerTransform.position;
                _dealerInitialRot = _dealerTransform.rotation;
            }
        }

        public void SpawnAndMoveToStage()
        {
            if (_eyePrefab == null || _stagePoint == null) return;
            if (_currentEye != null) { _currentEye.transform.DOKill(); Destroy(_currentEye); }

            _currentEye = Instantiate(_eyePrefab, _stagePoint.position, _stagePoint.rotation);
            Vector3 originalScale = _currentEye.transform.localScale;
            _currentEye.transform.localScale = new Vector3(originalScale.x, 0f, originalScale.z);
            _currentEye.transform.DOScaleY(originalScale.y, _spawnUpDuration).SetEase(Ease.OutCubic).SetLink(_currentEye);
        }

        public void OnWin(Action onComplete = null)
        {
            // [수정] 승리 시에는 눈알을 부수거나 사라지게 하지 않고 그대로 둡니다.
            // 바로 다음 로직(카드 수거 등)으로 넘어가도록 콜백만 즉시 실행합니다.
            onComplete?.Invoke();
        }

        public void OnLose(Action onComplete = null)
        {
            if (_currentEye == null || _losePoint == null) { onComplete?.Invoke(); return; }
            StartCoroutine(LoseRoutine(onComplete));
        }

        private IEnumerator LoseRoutine(Action onComplete)
        {
            _currentEye.transform.DOMove(_losePoint.position, _moveToLoseDuration).SetEase(Ease.InOutQuad).SetLink(_currentEye);
            yield return new WaitForSeconds(_moveToLoseDuration);

            bool shouldPlaySpecial = !_hasPlayedSpecialRoutine && 
                                     VisionManager.Instance != null && 
                                     VisionManager.Instance.CurrentVision <= 50 && 
                                     VisionManager.Instance.CurrentVision > 0;

            if (shouldPlaySpecial)
            {
                _hasPlayedSpecialRoutine = true;
                yield return StartCoroutine(SpecialLoseRoutine50());
            }
            else
            {
                yield return StartCoroutine(NormalLoseRoutine());
            }

            onComplete?.Invoke();
        }

        private IEnumerator NormalLoseRoutine()
        {
            if (_dealerTransform != null && _dealerNormalLosePoint != Vector3.zero)
                _dealerTransform.position = _dealerNormalLosePoint;

            if (_dealerAnimator != null)
                _dealerAnimator.SetTrigger(_dealerNormalLoseAnimTrigger);

            bool isBlackoutDone = false;
            StartCoroutine(BlackoutAndRestoreRoutine(_normalBlackoutDelay, () => isBlackoutDone = true));

            yield return new WaitForSeconds(_destroyWaitAfterLose);

            if (_currentEye != null)
            {
                yield return _currentEye.transform.DOScaleY(0f, _shrinkDuration)
                    .SetEase(Ease.InCubic)
                    .SetLink(_currentEye)
                    .WaitForCompletion();

                if (_breakParticlePrefab != null)
                {
                    Instantiate(_breakParticlePrefab, _currentEye.transform.position, Quaternion.identity);
                }

                _currentEye.transform.DOKill();
                Destroy(_currentEye);
                AudioManager.instance.PlaySfx(AudioManager.Sfx.dealerStab);
            }

            yield return new WaitUntil(() => isBlackoutDone);
        }

        private IEnumerator SpecialLoseRoutine50()
        {
            // 1. 딜러 전조 이동
            if (_dealerTransform != null && _dealerPreSpecialPoint != null)
            {
                _dealerTransform.DOMove(_dealerPreSpecialPoint.position, _preSpecialMoveDuration).SetEase(Ease.InOutQuad).SetLink(_dealerTransform.gameObject);
                _dealerTransform.DORotateQuaternion(_dealerPreSpecialPoint.rotation, _preSpecialMoveDuration).SetEase(Ease.InOutQuad).SetLink(_dealerTransform.gameObject);
                yield return new WaitForSeconds(_preSpecialMoveDuration);
            }

            yield return new WaitForSeconds(_delayBeforeCameraTransition);

            // 2. 카메라 전환
            AudioManager.instance.PlaySfx(AudioManager.Sfx.dealer);
            if (SceneLoadManager.Instance != null && _reachOutCamera != null)
            {
                CinemachineCamera fromCam = _mainGameCamera;
                if (fromCam == null)
                {
                    var brain = GameObject.FindAnyObjectByType<CinemachineBrain>();
                    if (brain != null && brain.ActiveVirtualCamera is CinemachineCamera active) fromCam = active;
                }
                if (fromCam != null)
                {
                    bool transitionDone = false;
                    SceneLoadManager.Instance.StartGameWithCameraTransition(fromCam, _reachOutCamera, () => transitionDone = true);
                    yield return new WaitUntil(() => transitionDone);
                }
            }

            // 3. 본 연출 배치 및 애니메이션 실행
            if (_dealerTransform != null && _dealerReachOutPoint != null)
            {
                _dealerTransform.position = _dealerReachOutPoint.position;
                _dealerTransform.rotation = _dealerReachOutPoint.rotation;
            }

            if (_dealerAnimator != null) _dealerAnimator.SetTrigger(_dealerReachOutAnimTrigger);

            if (_dealerEyes != null)
            {
                foreach (var eye in _dealerEyes)
                {
                    if (eye != null) eye.SetActive(true);
                }
            }

            bool isBlackoutDone = false;
            StartCoroutine(BlackoutAndRestoreRoutine(_specialBlackoutDelay, () => isBlackoutDone = true));

            yield return new WaitUntil(() => isBlackoutDone);
        }

        private IEnumerator BlackoutAndRestoreRoutine(float startDelay, Action onDone)
        {
            yield return new WaitForSeconds(startDelay);

            GameObject canvasGo = new GameObject("BlackoutCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            
            GameObject panelGo = new GameObject("BlackoutPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            Image img = panelGo.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            RectTransform rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

            yield return img.DOFade(1f, _blackoutFadeTime).SetEase(Ease.InOutSine).WaitForCompletion();

            if (_currentEye != null) { _currentEye.transform.DOKill(); Destroy(_currentEye); }
            
            if (VisionManager.Instance != null)
            {
                VisionManager.Instance.ApplyVignetteEffect();
            }

            if (_mainGameCamera != null) _mainGameCamera.Priority = 10;
            if (_reachOutCamera != null) _reachOutCamera.Priority = 0;
            if (_dealerTransform != null) { _dealerTransform.position = _dealerInitialPos; _dealerTransform.rotation = _dealerInitialRot; }
            if (_dealerAnimator != null) _dealerAnimator.SetTrigger(_dealerIdleAnimTrigger);

            yield return new WaitForSeconds(_blackoutHoldTime);
            yield return img.DOFade(0f, _blackoutFadeTime).SetEase(Ease.InOutSine).WaitForCompletion();

            Destroy(canvasGo);
            onDone?.Invoke();

            AudioManager.instance.PlaySfx(AudioManager.Sfx.whoosh);
            AudioManager.instance.PlayBgm(AudioManager.Bgm.damaged);
        }

        public static void ResetSpecialRoutineFlag() => _hasPlayedSpecialRoutine = false;
    }
}
