using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace JSG
{
    /// <summary>
    /// 눈알이 갑자기 휙휙 돌면서 다른 곳을 주시하는 기괴한 애니메이션을 담당합니다.
    /// 자연스러운 안구 운동(Saccade)처럼 빠른 회전과 멈춤을 반복합니다.
    /// </summary>
    public class EyeFlickerAnimation : MonoBehaviour
    {
        [Header("Gaze Settings (주시 설정)")]
        [Tooltip("최대 회전 가능 각도 (기본 정면 기준)")]
        [SerializeField] private float _maxRotationAngle = 40f;
        
        [Tooltip("한 번 휙 돌 때 걸리는 시간")]
        [SerializeField] private float _flickDuration = 0.1f;

        [Header("Wait Settings (대기 설정)")]
        [SerializeField] private float _minWaitTime = 0.5f;
        [SerializeField] private float _maxWaitTime = 3.0f;

        [Header("Optional Shake (미세한 떨림)")]
        [SerializeField] private bool _useMicroShake = true;
        [SerializeField] private float _shakeStrength = 0.02f;

        private Quaternion _initialRotation;

        private void Start()
        {
            _initialRotation = transform.localRotation;
            StartCoroutine(FlickerRoutine());

            if (_useMicroShake)
            {
                // 주시하는 중에도 미세하게 눈알이 떨리도록 설정
                transform.DOShakePosition(1f, _shakeStrength, 10)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }

        private IEnumerator FlickerRoutine()
        {
            while (true)
            {
                // 1. 임의의 대기 시간
                yield return new WaitForSeconds(Random.Range(_minWaitTime, _maxWaitTime));

                // 2. 랜덤한 목표 회전값 계산 (정면 기준 일정 범위 내)
                float randomX = Random.Range(-_maxRotationAngle, _maxRotationAngle);
                float randomY = Random.Range(-_maxRotationAngle, _maxRotationAngle);
                Quaternion targetRotation = _initialRotation * Quaternion.Euler(randomX, randomY, 0);

                // 3. 휙! 하고 빠르게 회전 (Saccade 연출)
                // Ease.OutCubic이나 Ease.OutExpo를 사용해 끝이 날카로운 움직임을 줍니다.
                transform.DOLocalRotateQuaternion(targetRotation, _flickDuration)
                    .SetEase(Ease.OutExpo);
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
