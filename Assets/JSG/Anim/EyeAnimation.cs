using UnityEngine;
using DG.Tweening;

namespace JSG
{
    /// <summary>
    /// 맵에 배치될 눈알 오브젝트의 애니메이션을 담당합니다.
    /// 위치 흔들림(Shake)과 천천히 회전하는 효과를 줍니다.
    /// </summary>
    public class EyeAnimation : MonoBehaviour
    {
        [Header("Shake Settings (위치 흔들림)")]
        [SerializeField] private float _shakeStrength = 0.05f;
        [SerializeField] private float _shakeDuration = 2f;
        [SerializeField] private int _shakeVibrato = 5;

        [Header("Rotation Settings (회전)")]
        [SerializeField] private Vector3 _rotationAxis = new Vector3(0, 1, 0);
        [SerializeField] private float _rotationSpeed = 10f;

        private void Start()
        {
            // 1. 위치 흔들기 (Loop)
            // 상하좌우로 미세하게 떨리는 연출
            transform.DOShakePosition(_shakeDuration, _shakeStrength, _shakeVibrato)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            // 2. 자전 회전 (Loop)
            // 설정한 축을 기준으로 천천히 계속 회전
            // Linear를 사용하여 끊김 없는 회전을 구현합니다.
            float fullRotationTime = 360f / _rotationSpeed;
            transform.DORotate(_rotationAxis * 360f, fullRotationTime, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear);
        }

        private void OnDestroy()
        {
            // 오브젝트 파괴 시 트윈 살해
            transform.DOKill();
        }
    }
}
