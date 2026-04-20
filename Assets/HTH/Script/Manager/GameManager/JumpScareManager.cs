using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace HTH
{
    /// <summary>
    /// 시야가 0이 되었을 때 랜덤 점프스케어를 실행합니다.
    /// </summary>
    public class JumpScareManager : MonoBehaviour
    {
        [Header("디버그 설정")]
        [SerializeField] private bool _useDebugScare = false;
        [Range(1, 2)]
        [SerializeField] private int _debugScareIndex = 1;

        [Header("점프스케어 1 (시야 0)")]
        [SerializeField] private GameObject _jumpScareObject;
        [SerializeField] private Transform _jumpScareTargetPoint;
        [SerializeField] private float _jumpScareMoveDuration = 3f;

        [Header("점프스케어 2 (시야 0)")]
        [Tooltip("회전/이동할 오브젝트")]
        [SerializeField] private GameObject _js2Object;
        
        [Space(10)]
        [Header("JS2 - 1단계 이동")]
        [SerializeField] private Transform _js2TargetPoint1;
        [SerializeField] private float _js2MoveDuration1 = 1f;

        [Header("JS2 - 2단계 이동")]
        [SerializeField] private Transform _js2TargetPoint2;
        [SerializeField] private float _js2MoveDuration2 = 0.5f;

        [Space(10)]
        [SerializeField] private float _rotationPerTenVision = 3.33f;

        private bool _isScareTriggered = false;

        private Vector3 _js1InitialLocalPos;
        private Vector3 _js1InitialLocalEuler;
        private bool _js1InitialActive;

        private Vector3 _js2InitialLocalPos;
        private Vector3 _js2InitialLocalEuler;
        private bool _js2InitialActive;

        private void Awake()
        {
            if (_jumpScareObject != null)
            {
                _js1InitialLocalPos = _jumpScareObject.transform.localPosition;
                _js1InitialLocalEuler = _jumpScareObject.transform.localEulerAngles;
                _js1InitialActive = _jumpScareObject.activeSelf;
            }

            if (_js2Object != null)
            {
                _js2InitialLocalPos = _js2Object.transform.localPosition;
                _js2InitialLocalEuler = _js2Object.transform.localEulerAngles;
                _js2InitialActive = _js2Object.activeSelf;
            }
        }

        private void Start()
        {
            StartCoroutine(WaitAndSubscribe());
        }

        private IEnumerator WaitAndSubscribe()
        {
            while (VisionManager.Instance == null) yield return null;
            VisionManager.Instance.OnVisionChanged += UpdateJS2Rotation;
            Debug.Log("<color=cyan>[JumpScare] VisionManager 구독 성공!</color>");
        }

        private void OnDestroy()
        {
            if (VisionManager.Instance != null)
                VisionManager.Instance.OnVisionChanged -= UpdateJS2Rotation;
        }

        private void UpdateJS2Rotation(int currentVision, int maxVision)
        {
            if (_js2Object == null || _isScareTriggered) return;

            float lostVision = maxVision - currentVision;
            float targetYRotation = (lostVision / 10f) * _rotationPerTenVision;
            Vector3 targetEuler = _js2InitialLocalEuler + new Vector3(0, targetYRotation, 0);

            _js2Object.transform.DOKill();
            _js2Object.transform.DOLocalRotate(targetEuler, 0.5f).SetEase(Ease.OutQuad).SetLink(_js2Object);
            
            Debug.Log($"[JumpScare] 시야 {currentVision}: JS2 {targetYRotation}도 회전");
        }

        public IEnumerator PlayRandomJumpScareRoutine(System.Action onComplete)
        {
            if (_isScareTriggered) yield break;
            _isScareTriggered = true;

            int targetIndex = _useDebugScare ? _debugScareIndex : Random.Range(1, 3);
            Debug.Log($"[JumpScare] 점프스케어 {targetIndex}번 실행!");

            if (targetIndex == 1) yield return StartCoroutine(JumpScareRoutine1());
            else yield return StartCoroutine(JumpScareRoutine2());

            onComplete?.Invoke();
        }

        private IEnumerator JumpScareRoutine1()
        {
            if (_jumpScareObject == null || _jumpScareTargetPoint == null) yield break;
            if (!_jumpScareObject.activeSelf) _jumpScareObject.SetActive(true);

            _jumpScareObject.transform.DOKill();
            yield return _jumpScareObject.transform.DOMove(_jumpScareTargetPoint.position, _jumpScareMoveDuration)
                .SetEase(Ease.InOutSine).SetLink(_jumpScareObject).WaitForCompletion();
        }

        private IEnumerator JumpScareRoutine2()
        {
            if (_js2Object == null || _js2TargetPoint1 == null || _js2TargetPoint2 == null)
            {
                Debug.LogWarning("[JumpScare] JS2 타겟 포인트가 설정되지 않았습니다.");
                yield break;
            }

            if (!_js2Object.activeSelf) _js2Object.SetActive(true);

            Animator anim = _js2Object.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;

            _js2Object.transform.DOKill();

            // [1단계 이동/회전]
            Debug.Log("[JumpScare] JS2 1단계 이동 시작");
            _js2Object.transform.DOMove(_js2TargetPoint1.position, _js2MoveDuration1).SetEase(Ease.InQuad).SetLink(_js2Object);
            yield return _js2Object.transform.DORotateQuaternion(_js2TargetPoint1.rotation, _js2MoveDuration1)
                .SetEase(Ease.InQuad).SetLink(_js2Object).WaitForCompletion();

            // [2단계 이동/회전]
            Debug.Log("[JumpScare] JS2 2단계 이동 시작");
            _js2Object.transform.DOMove(_js2TargetPoint2.position, _js2MoveDuration2).SetEase(Ease.InQuad).SetLink(_js2Object);
            yield return _js2Object.transform.DORotateQuaternion(_js2TargetPoint2.rotation, _js2MoveDuration2)
                .SetEase(Ease.InQuad).SetLink(_js2Object).WaitForCompletion();

            if (anim != null) anim.enabled = true;
        }

        public void ResetAll()
        {
            _isScareTriggered = false;

            if (_jumpScareObject != null)
            {
                _jumpScareObject.transform.DOKill();
                _jumpScareObject.transform.localPosition = _js1InitialLocalPos;
                _jumpScareObject.transform.localEulerAngles = _js1InitialLocalEuler;
                _jumpScareObject.SetActive(_js1InitialActive);
            }

            if (_js2Object != null)
            {
                _js2Object.transform.DOKill();
                _js2Object.transform.localPosition = _js2InitialLocalPos;
                _js2Object.transform.localEulerAngles = _js2InitialLocalEuler;
                _js2Object.SetActive(_js2InitialActive);
                
                Animator anim = _js2Object.GetComponent<Animator>();
                if (anim != null) anim.enabled = true;
            }
        }
    }
}
